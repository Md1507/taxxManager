using Microsoft.EntityFrameworkCore;
using TaxManager.Data.Dependents;
using TaxManager.Data.Income;
using TaxManager.Data.Profiles;
using TaxManager.Data.TaxRules;
using TaxManager.Core;
using TaxManager.Data.Protection;
using TaxManager.Data.Audit;
using TaxManager.Data.CalculationHistory;

namespace TaxManager.Data.Infrastructure;

public sealed class TaxManagerDbContext : DbContext
{
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<MonthlyIncome> MonthlyIncomes => Set<MonthlyIncome>();
    public DbSet<Dependent> Dependents => Set<Dependent>();
    public DbSet<DependentEligibilityPeriod> DependentEligibilityPeriods => Set<DependentEligibilityPeriod>();
    public DbSet<CalculationRecord> CalculationHistory => Set<CalculationRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TaxManagerDbContext).Assembly);

    private readonly ProtectionSession protection;
    private readonly AuditSaveCoordinator coordinator;
    public AuditWriteContext? AuditContext { get; set; }
    public TimeProvider Clock { get; set; } = TimeProvider.System;
    internal CalculationRecord? AuthorizedHistoryAppend { get; set; }

    public TaxManagerDbContext(DbContextOptions<TaxManagerDbContext> options, FileKeyRing? keys = null) : base(options)
    {
        protection = new ProtectionSession(this, keys);
        coordinator = new AuditSaveCoordinator(this, protection);
        ChangeTracker.StateChanged += (_, e) =>
        {
            if (e.NewState == EntityState.Unchanged) ProtectionSession.Invalidate(e.Entry.Entity);
        };
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder.AddInterceptors(ProtectedMaterializationInterceptor.Instance);
    internal void BindProtectedEntity(object entity) => protection.Bind(entity);
    internal void MarkSensitiveDirty(object entity, string property)
    {
        try
        {
            var entry = Entry(entity);
            if (entry.State is EntityState.Unchanged or EntityState.Modified) entry.Property(property).IsModified = true;
        }
        catch (ObjectDisposedException) { /* Detached plaintext objects may outlive their reader context. */ }
    }
    public void ValidateProtection() => protection.Ensure();
    internal void InitializeProtectionForImport() => protection.Ensure(initialize: true);
    public AuditPayload ReadAudit(AuditEvent auditEvent) => protection.ReadAudit(auditEvent);
    public override int SaveChanges(bool acceptAllChangesOnSuccess) =>
        coordinator.Save(false, acceptAllChangesOnSuccess, default).GetAwaiter().GetResult();
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default) =>
        coordinator.Save(true, acceptAllChangesOnSuccess, cancellationToken);
    internal int SaveCore() => base.SaveChanges(false);
    internal Task<int> SaveCoreAsync(CancellationToken token) => base.SaveChangesAsync(false, token);
    internal void ValidateEntitiesForSave()
    {
        foreach (var entry in ChangeTracker.Entries().Where(
                     entry => entry.State is EntityState.Added or EntityState.Modified))
            ValidateEntityForSave(entry.Entity);
        TaxRuleGraphValidation.Validate(this);
    }

    internal static void ValidateEntityForSave(object entity)
    {
        switch (entity)
        {
            case TaxRuleSnapshot rule when string.IsNullOrWhiteSpace(rule.RuleId) ||
                                          string.IsNullOrWhiteSpace(rule.Version) ||
                                          string.IsNullOrWhiteSpace(rule.Period) ||
                                          rule.FromYear is < 1 or > 9999 ||
                                          rule.PersonalDeduction < 0 || rule.DependentDeduction < 0:
                throw new ArgumentException("Tax rule identity, year or deductions are invalid.");
            case TaxRuleBracket bracket when bracket.Ordinal < 0 || bracket.Rate is < 0 or > 1 ||
                                            bracket.UpperBound is <= 0:
                throw new ArgumentException("Tax rule bracket is invalid.");
            case TaxRulePeriod registration when registration.PeriodFrom > registration.PeriodTo ||
                                                registration.PeriodFrom.Day != 1 ||
                                                registration.PeriodTo.Day != DateTime.DaysInMonth(registration.PeriodTo.Year, registration.PeriodTo.Month):
                throw new ArgumentException("Tax rule period must cover complete months.");
            case TaxRuleLegalBasis basis:
                _ = new LegalBasis((VerificationStatus)basis.Status, basis.Id, basis.Version,
                    basis.Point, basis.Clause, basis.Article, basis.DocumentNumber, basis.Summary,
                    basis.SourceUrl, basis.Period, basis.EffectiveFrom, basis.EffectiveTo, basis.VerifiedOn);
                break;
            case UserProfile profile when profile.Id == Guid.Empty:
                throw new ArgumentException("Profile ID must be nonempty.");
            case Dependent dependent when dependent.ProfileId == Guid.Empty || dependent.Id == Guid.Empty:
                throw new ArgumentException("Dependent IDs must be nonempty.");
            case MonthlyIncome income when !income.AmountsInitialized || income.ProfileId == Guid.Empty ||
                                           income.GrossIncome < 0 || income.InsuranceAmount < 0 ||
                                           income.ActualWithheldTax < 0:
                throw new ArgumentException("Income IDs and amounts are invalid.");
            case DependentEligibilityPeriod period when period.ProfileId == Guid.Empty || period.DependentId == Guid.Empty:
                throw new ArgumentException("Period IDs must be nonempty.");
        }
    }
}
