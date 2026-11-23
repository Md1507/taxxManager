using System.Collections.Immutable;
using TaxManager.Core;
using TaxManager.Data.TaxRules;

namespace TaxManager.Data.CalculationHistory;

public sealed record CalculationRuleSnapshot(string RuleId, string Version, string Period, int FromYear,
    DateOnly PeriodFrom, DateOnly PeriodTo, decimal PersonalDeduction, decimal DependentDeduction,
    ImmutableArray<Bracket> Brackets, LegalBasis PersonalDeductionBasis, LegalBasis DependentDeductionBasis,
    LegalBasis TaxableIncomeBasis, LegalBasis TaxBasis)
{
    internal static CalculationRuleSnapshot FromCore(TaxRuleRegistration registration)
    {
        var r = registration.RuleSet;
        TaxRuleValidator.ValidateSupported(r); // Never silently discard optional graphs.
        return new(r.Id, r.Version, r.Period, r.FromYear, registration.PeriodFrom, registration.PeriodTo,
            r.PersonalDeduction, r.DependentDeduction, r.Brackets.ToImmutableArray(),
            r.PersonalDeductionBasis, r.DependentDeductionBasis, r.TaxableIncomeBasis, r.TaxBasis);
    }

    public override string ToString() => nameof(CalculationRuleSnapshot);
}
