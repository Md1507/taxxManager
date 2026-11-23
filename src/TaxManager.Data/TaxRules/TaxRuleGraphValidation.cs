using Microsoft.EntityFrameworkCore;
using TaxManager.Data.Infrastructure;

namespace TaxManager.Data.TaxRules;

internal static class TaxRuleGraphValidation
{
    internal static void Validate(TaxManagerDbContext context)
    {
        // EF writes only modified properties. A stale tracked entity must not replace
        // the current database row in the projected graph used by the guards below.
        // Reject it under the write transaction so callers can reload and retry.
        foreach (var entry in context.ChangeTracker.Entries().Where(entry =>
                     entry.State == EntityState.Modified &&
                     entry.Entity is TaxRuleSnapshot or TaxRuleBracket or TaxRulePeriod or TaxRuleLegalBasis))
        {
            var databaseValues = entry.GetDatabaseValues();
            if (databaseValues is null || entry.Properties.Any(property =>
                    !Equals(property.OriginalValue, databaseValues[property.Metadata.Name])))
                throw new InvalidOperationException(
                    "Tax rule data changed since it was loaded; reload the graph before saving.");
        }

        var brackets = context.ChangeTracker.Entries<TaxRuleBracket>().ToArray();
        var snapshots = context.ChangeTracker.Entries<TaxRuleSnapshot>().ToArray();
        var affected = brackets
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(entry => (entry.Entity.RuleId, entry.Entity.Version))
            .Concat(snapshots.Where(entry => entry.State is EntityState.Added or EntityState.Modified)
                .Select(entry => (entry.Entity.RuleId, entry.Entity.Version)))
            .Distinct();

        foreach (var key in affected)
        {
            var snapshot = snapshots.FirstOrDefault(entry =>
                entry.Entity.RuleId == key.RuleId && entry.Entity.Version == key.Version);
            if (snapshot?.State == EntityState.Deleted) continue;

            var graph = snapshot?.State == EntityState.Added
                ? new List<TaxRuleBracket>()
                : context.Set<TaxRuleBracket>().AsNoTracking()
                    .Where(item => item.RuleId == key.RuleId && item.Version == key.Version).ToList();
            foreach (var entry in brackets.Where(entry =>
                         entry.Entity.RuleId == key.RuleId && entry.Entity.Version == key.Version &&
                         entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                graph.RemoveAll(item => item.Ordinal == entry.Entity.Ordinal);
                if (entry.State != EntityState.Deleted) graph.Add(entry.Entity);
            }

            var ordered = graph.OrderBy(item => item.Ordinal).ToArray();
            decimal previous = 0;
            if (ordered.Length == 0) throw new ArgumentException("Tax rule brackets are missing.");
            for (var index = 0; index < ordered.Length; index++)
            {
                var bracket = ordered[index];
                if (bracket.Ordinal != index || bracket.Rate is < 0 or > 1 ||
                    (index == ordered.Length - 1 ? bracket.UpperBound is not null :
                        bracket.UpperBound is null || bracket.UpperBound <= previous))
                    throw new ArgumentException("Tax rule bracket graph is invalid.");
                if (bracket.UpperBound is decimal bound) previous = bound;
            }
        }

        TaxRuleLegalBasisLifecycle.Validate(context);
    }
}
