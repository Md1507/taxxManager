using Microsoft.EntityFrameworkCore;
using TaxManager.Data.Infrastructure;

namespace TaxManager.Data.TaxRules;

internal static class TaxRuleLegalBasisLifecycle
{
    internal static void Validate(TaxManagerDbContext context)
    {
        var snapshots = context.ChangeTracker.Entries<TaxRuleSnapshot>().ToArray();
        var brackets = context.ChangeTracker.Entries<TaxRuleBracket>().ToArray();
        var bases = context.ChangeTracker.Entries<TaxRuleLegalBasis>().ToArray();
        var periods = context.ChangeTracker.Entries<TaxRulePeriod>().ToArray();
        if (!snapshots.Any(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted) &&
            !brackets.Any(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted) &&
            !bases.Any(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted) &&
            !periods.Any(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
            return;

        var snapshotRefs = context.Set<TaxRuleSnapshot>().AsNoTracking()
            .Select(x => new { x.RuleId, x.Version, x.PersonalBasisKey, x.DependentBasisKey, x.IncomeBasisKey, x.TaxBasisKey })
            .ToList().ToDictionary(x => (x.RuleId, x.Version),
                x => new[] { x.PersonalBasisKey, x.DependentBasisKey, x.IncomeBasisKey, x.TaxBasisKey });
        var bracketRefs = context.Set<TaxRuleBracket>().AsNoTracking()
            .Select(x => new { x.RuleId, x.Version, x.Ordinal, x.BasisKey })
            .ToList().ToDictionary(x => (x.RuleId, x.Version, x.Ordinal), x => x.BasisKey);
        var basisKeys = context.Set<TaxRuleLegalBasis>().AsNoTracking()
            .Select(x => x.Key).ToHashSet();
        var periodRefs = context.Set<TaxRulePeriod>().AsNoTracking()
            .Select(x => new { x.Key, x.RuleId, x.Version })
            .ToList().ToDictionary(x => x.Key, x => (x.RuleId, x.Version));
        var deletedSnapshots = snapshots.Where(e => e.State == EntityState.Deleted)
            .Select(e => (e.Entity.RuleId, e.Entity.Version)).ToHashSet();

        foreach (var entry in snapshots.Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            var x = entry.Entity;
            var key = (x.RuleId, x.Version);
            if (entry.State == EntityState.Deleted)
            {
                snapshotRefs.Remove(key);
                foreach (var bracketKey in bracketRefs.Keys.Where(k => k.RuleId == x.RuleId && k.Version == x.Version).ToArray())
                    bracketRefs.Remove(bracketKey);
                foreach (var periodKey in periodRefs.Where(p => p.Value == key).Select(p => p.Key).ToArray())
                    periodRefs.Remove(periodKey);
            }
            else
                snapshotRefs[key] = [x.PersonalBasisKey, x.DependentBasisKey, x.IncomeBasisKey, x.TaxBasisKey];
        }

        foreach (var entry in brackets.Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            var x = entry.Entity;
            var key = (x.RuleId, x.Version, x.Ordinal);
            if (entry.State == EntityState.Deleted || !snapshotRefs.ContainsKey((x.RuleId, x.Version)))
                bracketRefs.Remove(key);
            else
                bracketRefs[key] = x.BasisKey;
        }

        foreach (var entry in bases)
        {
            if (entry.State == EntityState.Deleted) basisKeys.Remove(entry.Entity.Key);
            else if (entry.State == EntityState.Added) basisKeys.Add(entry.Entity.Key);
        }

        foreach (var entry in periods.Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            var x = entry.Entity;
            var snapshotKey = (x.RuleId, x.Version);
            if (entry.State == EntityState.Deleted || deletedSnapshots.Contains(snapshotKey))
                periodRefs.Remove(x.Key);
            else
                periodRefs[x.Key] = snapshotKey;
        }

        var registeredSnapshots = periodRefs.Values.ToHashSet();
        if (!registeredSnapshots.SetEquals(snapshotRefs.Keys))
            throw new InvalidOperationException(
                "Tax rule changes would leave a missing snapshot or a snapshot without a registration; update the complete graph together.");

        var references = snapshotRefs.Values.SelectMany(x => x).Concat(bracketRefs.Values).ToHashSet();
        if (!references.SetEquals(basisKeys))
            throw new InvalidOperationException(
                "Tax rule changes would leave missing or unreferenced legal bases; update the complete graph together.");
    }
}
