using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaxManager.Core;
using TaxManager.Data.Infrastructure;

namespace TaxManager.Data.TaxRules;

public sealed class AnnualForecastRule
{
    public int Year { get; set; }
    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    public string Graph { get; set; } = null!;
}

public sealed class AnnualForecastRuleConfiguration : IEntityTypeConfiguration<AnnualForecastRule>
{
    public void Configure(EntityTypeBuilder<AnnualForecastRule> builder)
    {
        builder.ToTable("AnnualForecastRules");
        builder.HasKey(x => new { x.Year, x.PeriodFrom });
        builder.Property(x => x.Graph).IsRequired();
    }
}

/// <summary>Explicit reference catalog, separate from monthly-only references and official rules.</summary>
public sealed class AnnualForecastRuleCatalog(TaxManagerDbContext context)
{
    private static AnnualForecastRule[] References()
    {
        var provider = TaxRuleSet.CreateVietnam2026CashBonusProvider(InsuranceRegion.I);
        return new[] { (1, 6), (7, 12) }.Select(period =>
        {
            var source = provider.GetForMonth(2026, period.Item1);
            // Region insurance graphs are irrelevant: this contract accepts manual insurance only.
            var manual = new TaxRuleSet(source.Id, source.Version, source.Period, source.FromYear,
                source.PersonalDeduction, source.DependentDeduction, source.Brackets, source.PersonalDeductionBasis,
                source.DependentDeductionBasis, source.TaxableIncomeBasis, source.TaxBasis,
                roundingPolicy: source.RoundingPolicy, annualSalaryRules: source.AnnualSalaryRules, cashBonusRules: source.CashBonusRules);
            TaxRuleValidator.ValidateSupported(manual);
            return new AnnualForecastRule { Year = 2026, PeriodFrom = new(2026, period.Item1, 1),
                PeriodTo = new(2026, period.Item2, DateTime.DaysInMonth(2026, period.Item2)), Graph = AnnualRuleGraphCodec.Encode(manual) };
        }).ToArray();
    }

    public async Task SeedReferencesAsync(CancellationToken token = default)
    {
        if (context.ChangeTracker.HasChanges()) throw new InvalidOperationException("Catalog seeding requires no pending writes.");
        await using var transaction = await context.Database.BeginTransactionAsync(token);
        var existing = await context.Set<AnnualForecastRule>().AsNoTracking().ToListAsync(token);
        var expected = References();
        if (existing.Count != 0) Validate(existing, expected);
        else
        {
            context.AddRange(expected);
            await context.SaveChangesAsync(token);
        }
        await transaction.CommitAsync(token);
    }

    internal async Task<(TaxRuleProvider Provider, AnnualForecastRule[] Rules)> ReadAsync(int year, CancellationToken token)
    {
        // Validate the stored monthly catalog and its coverage too; no Core fallback after drift.
        var monthly = await new TaxRuleProviderLoader(context).ReadRegistrationsAsync(token);
        TaxRuleProviderLoader.ValidateSourcePolicy(monthly, false);
        var monthlyProvider = new TaxRuleProvider(monthly);
        for (var month = 1; month <= 12; month++) _ = monthlyProvider.GetForMonth(year, month);
        var rows = await context.Set<AnnualForecastRule>().AsNoTracking().OrderBy(x => x.PeriodFrom).ToArrayAsync(token);
        Validate(rows, References());
        var provider = new TaxRuleProvider(rows.Where(r => r.Year == year).Select(r =>
            new TaxRuleRegistration(AnnualRuleGraphCodec.Decode(r.Graph), r.PeriodFrom, r.PeriodTo)));
        return (provider, rows.Where(r => r.Year == year).ToArray());
    }

    private static void Validate(IEnumerable<AnnualForecastRule> rows, AnnualForecastRule[] expected)
    {
        var actual = rows.ToArray();
        if (actual.Length != expected.Length || expected.Any(e => !actual.Any(a =>
            a.Year == e.Year && a.PeriodFrom == e.PeriodFrom && a.PeriodTo == e.PeriodTo && a.Graph == e.Graph)))
            throw new InvalidOperationException("Annual reference catalog is missing or differs from the supported source snapshots.");
    }
}
