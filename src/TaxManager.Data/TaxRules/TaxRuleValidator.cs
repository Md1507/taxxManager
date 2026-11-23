using TaxManager.Core;

namespace TaxManager.Data.TaxRules;

/// <summary>Validates the subset supported by monthly manual-insurance persistence.</summary>
public static class TaxRuleValidator
{
    public static void ValidateSupported(TaxRuleSet ruleSet)
    {
        ArgumentNullException.ThrowIfNull(ruleSet);
        if (ruleSet.FromYear > 9999)
            throw new ArgumentException("FromYear must be between 1 and 9999.", nameof(ruleSet));

        if (ruleSet.InsuranceRules is not null || ruleSet.RoundingPolicy is not null ||
            ruleSet.AnnualSalaryRules is not null || ruleSet.CashBonusRules is not null ||
            ruleSet.SalaryExtraRules is not null || ruleSet.CasualWithholdingRules is not null ||
            ruleSet.MultipleSalaryRules is not null || ruleSet.CharitableContributionRules is not null ||
            ruleSet.RetirementRules is not null)
        {
            throw new NotSupportedException("Only monthly tax rules with manual insurance are supported; attached optional rule graphs cannot be persisted.");
        }
    }

    public static void ValidateOfficial(TaxRuleSet ruleSet)
    {
        ValidateSupported(ruleSet);
        LegalBasisValidator.ValidateOfficial(ruleSet.PersonalDeductionBasis);
        LegalBasisValidator.ValidateOfficial(ruleSet.DependentDeductionBasis);
        LegalBasisValidator.ValidateOfficial(ruleSet.TaxableIncomeBasis);
        LegalBasisValidator.ValidateOfficial(ruleSet.TaxBasis);
        foreach (var bracket in ruleSet.Brackets)
            LegalBasisValidator.ValidateOfficial(bracket.Basis);
    }
}
