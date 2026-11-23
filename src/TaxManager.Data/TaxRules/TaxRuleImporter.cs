using Microsoft.EntityFrameworkCore;
using TaxManager.Core;
using TaxManager.Data.Infrastructure;

namespace TaxManager.Data.TaxRules;

public sealed class TaxRuleImporter(TaxManagerDbContext context)
{
    public Task SeedReferencesAsync(CancellationToken cancellationToken = default) =>
        ImportAsync(TaxRuleStoreSupport.References(), false, cancellationToken);

    public Task ImportOfficialAsync(IEnumerable<TaxRuleRegistration> registrations,
        CancellationToken cancellationToken = default) => ImportAsync(registrations, true, cancellationToken);

    private async Task ImportAsync(IEnumerable<TaxRuleRegistration> source, bool official,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (context.ChangeTracker.HasChanges())
            throw new InvalidOperationException("Import requires a context without pending changes.");
        var input = source.ToArray();
        if (input.Length == 0) throw new ArgumentException("No tax registrations supplied.");
        foreach (var registration in input)
        {
            if (official) TaxRuleValidator.ValidateOfficial(registration.RuleSet);
            else TaxRuleValidator.ValidateSupported(registration.RuleSet);
        }
        _ = new TaxRuleProvider(input);
        var previouslyTracked = context.ChangeTracker.Entries().Select(x => x.Entity).ToHashSet();
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            if (await context.Set<TaxRuleSnapshot>().AnyAsync(cancellationToken) ||
                await context.Set<TaxRulePeriod>().AnyAsync(cancellationToken) ||
                await context.Set<TaxRuleLegalBasis>().AnyAsync(cancellationToken) ||
                await context.Set<TaxRuleBracket>().AnyAsync(cancellationToken))
            {
                await new TaxRuleProviderLoader(context).ReadRegistrationsAsync(cancellationToken);
            }
            foreach (var group in input.GroupBy(x => (x.RuleSet.Id, x.RuleSet.Version)))
            {
                var first = group.First().RuleSet;
                if (group.Any(x => !TaxRuleStoreSupport.SameRules(x.RuleSet, first)))
                    throw new InvalidOperationException("Conflicting tax rule snapshots share an identity and version.");
                var existing = await new TaxRuleStoreSupport(context).Snapshots().AsNoTracking().SingleOrDefaultAsync(x => x.RuleId == first.Id && x.Version == first.Version, cancellationToken);
                if (existing is null)
                {
                    context.Add(TaxRuleMapper.ToEntity(first));
                }
                else if (!TaxRuleStoreSupport.SameRules(TaxRuleMapper.ToCore(existing), first))
                {
                    throw new InvalidOperationException("Tax rule snapshot drift detected.");
                }
                var oldPeriods = await context.Set<TaxRulePeriod>().AsNoTracking().Where(x => x.RuleId == first.Id && x.Version == first.Version).ToListAsync(cancellationToken);
                if (existing is not null && (oldPeriods.Count != group.Count() ||
                    group.Any(x => !oldPeriods.Any(p => p.PeriodFrom == x.PeriodFrom && p.PeriodTo == x.PeriodTo))))
                    throw new InvalidOperationException("Tax rule coverage drift detected.");
                if (existing is null)
                    foreach (var registration in group)
                        context.Add(new TaxRulePeriod
                        {
                            Key = Guid.NewGuid(),
                            RuleId = first.Id,
                            Version = first.Version,
                            PeriodFrom = registration.PeriodFrom,
                            PeriodTo = registration.PeriodTo
                        });
            }
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            foreach (var entry in context.ChangeTracker.Entries()
                         .Where(x => !previouslyTracked.Contains(x.Entity))
                         .OrderBy(x => x.Entity switch
                         {
                             TaxRulePeriod => 0,
                             TaxRuleBracket => 1,
                             TaxRuleSnapshot => 2,
                             _ => 3
                         }).ToArray())
                entry.State = EntityState.Detached;
            throw;
        }
    }

}
