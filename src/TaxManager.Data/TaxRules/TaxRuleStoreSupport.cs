using Microsoft.EntityFrameworkCore;
using TaxManager.Core;
using TaxManager.Data.Infrastructure;

namespace TaxManager.Data.TaxRules;

/// <summary>Explicit import and loading for the single monthly manual-insurance timeline.</summary>
internal sealed class TaxRuleStoreSupport(TaxManagerDbContext context)
{
    internal static TaxRuleRegistration[] References() =>
    [
        new(TaxRuleSet.Vietnam2025, new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31)),
        new(TaxRuleSet.Vietnam2026, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31))
    ];

    internal static bool SameRules(TaxRuleSet a, TaxRuleSet b) =>
        a.Id == b.Id && a.Version == b.Version && a.Period == b.Period && a.FromYear == b.FromYear &&
        a.PersonalDeduction == b.PersonalDeduction && a.DependentDeduction == b.DependentDeduction &&
        a.PersonalDeductionBasis == b.PersonalDeductionBasis && a.DependentDeductionBasis == b.DependentDeductionBasis &&
        a.TaxableIncomeBasis == b.TaxableIncomeBasis && a.TaxBasis == b.TaxBasis &&
        a.Brackets.SequenceEqual(b.Brackets);

    internal IQueryable<TaxRuleSnapshot> Snapshots() => context.Set<TaxRuleSnapshot>()
        .Include(x => x.PersonalBasis).Include(x => x.DependentBasis)
        .Include(x => x.IncomeBasis).Include(x => x.TaxBasis)
        .Include(x => x.Brackets).ThenInclude(x => x.Basis);

}
