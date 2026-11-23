using Microsoft.EntityFrameworkCore;
using TaxManager.Core;
using TaxManager.Data.Infrastructure;

namespace TaxManager.Data.TaxRules;

public sealed class TaxRuleProviderLoader(TaxManagerDbContext context)
{
    public Task<TaxRuleProvider> LoadOfficialAsync(CancellationToken cancellationToken = default) =>
        LoadAsync(true, cancellationToken);

    public Task<TaxRuleProvider> LoadReferencesAsync(CancellationToken cancellationToken = default) =>
        LoadAsync(false, cancellationToken);

    internal async Task<TaxRuleRegistration[]> ReadRegistrationsAsync(CancellationToken cancellationToken)
    {
        await TaxRuleStorageValidator.ValidateAsync(context, cancellationToken);
        var snapshots = await new TaxRuleStoreSupport(context).Snapshots().AsNoTracking().ToListAsync(cancellationToken);
        var periods = await context.Set<TaxRulePeriod>().AsNoTracking().ToListAsync(cancellationToken);
        var snapshotCount = await context.Set<TaxRuleSnapshot>().CountAsync(cancellationToken);
        var bracketCount = await context.Set<TaxRuleBracket>().CountAsync(cancellationToken);
        var basisKeys = snapshots.SelectMany(x => new[]
            { x.PersonalBasisKey, x.DependentBasisKey, x.IncomeBasisKey, x.TaxBasisKey }
            .Concat(x.Brackets.Select(b => b.BasisKey))).Distinct().Count();
        var basisCount = await context.Set<TaxRuleLegalBasis>().CountAsync(cancellationToken);
        if (snapshots.Count != snapshotCount || snapshots.Sum(x => x.Brackets.Count) != bracketCount ||
            basisKeys != basisCount)
            throw new InvalidOperationException("Tax rule graph has missing or unreferenced related data.");
        if (snapshots.Count == 0 || periods.Count == 0)
            throw new InvalidOperationException("Tax rule database is empty.");
        var rules = snapshots.ToDictionary(x => (x.RuleId, x.Version), TaxRuleMapper.ToCore);
        if (rules.Keys.Any(key => !periods.Any(p => (p.RuleId, p.Version) == key)))
            throw new InvalidOperationException("Tax rule snapshot has no registration.");
        var registrations = periods.Select(p => new TaxRuleRegistration(
            rules[(p.RuleId, p.Version)], p.PeriodFrom, p.PeriodTo)).ToArray();
        _ = new TaxRuleProvider(registrations);
        return registrations;
    }

    private async Task<TaxRuleProvider> LoadAsync(bool official, CancellationToken cancellationToken)
    {
        try
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            var registrations = await ReadRegistrationsAsync(cancellationToken);
            if (official)
                foreach (var registration in registrations) TaxRuleValidator.ValidateOfficial(registration.RuleSet);
            var provider = new TaxRuleProvider(registrations);
            if (!official)
            {
                var expected = TaxRuleStoreSupport.References();
                if (registrations.Length != expected.Length || expected.Any(e => !registrations.Any(r =>
                    r.PeriodFrom == e.PeriodFrom && r.PeriodTo == e.PeriodTo &&
                    TaxRuleStoreSupport.SameRules(r.RuleSet, e.RuleSet))))
                    throw new InvalidOperationException("Reference tax rules differ from the supported reference data.");
            }
            await transaction.CommitAsync(cancellationToken);
            return provider;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidOperationException("Cannot load tax rules: the database is invalid, incomplete, or violates the selected source policy.", ex);
        }
    }
}
