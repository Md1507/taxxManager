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
        SameMonthlyRules(a, b) &&
        (a.AnnualSalaryRules is null && b.AnnualSalaryRules is null ||
         a.AnnualSalaryRules is not null && b.AnnualSalaryRules is not null &&
         AnnualRuleGraphCodec.Encode(a) == AnnualRuleGraphCodec.Encode(b));

    internal static bool SameMonthlyRules(TaxRuleSet a, TaxRuleSet b) =>
        a.Id == b.Id && a.Version == b.Version && a.Period == b.Period && a.FromYear == b.FromYear &&
        a.PersonalDeduction == b.PersonalDeduction && a.DependentDeduction == b.DependentDeduction &&
        a.PersonalDeductionBasis == b.PersonalDeductionBasis && a.DependentDeductionBasis == b.DependentDeductionBasis &&
        a.TaxableIncomeBasis == b.TaxableIncomeBasis && a.TaxBasis == b.TaxBasis &&
        a.Brackets.SequenceEqual(b.Brackets);

    internal IQueryable<TaxRuleSnapshot> SnapshotRows() =>
        context.Database.GetAppliedMigrations().Contains("20261127043040_AddAnnualRuleGraph")
            ? context.Set<TaxRuleSnapshot>()
            : context.Set<TaxRuleSnapshot>().FromSqlRaw("SELECT *, NULL AS AnnualGraph FROM TaxRuleSnapshot");

    internal IQueryable<TaxRuleSnapshot> Snapshots() => SnapshotRows()
        .Include(x => x.PersonalBasis).Include(x => x.DependentBasis)
        .Include(x => x.IncomeBasis).Include(x => x.TaxBasis)
        .Include(x => x.Brackets).ThenInclude(x => x.Basis);

}
