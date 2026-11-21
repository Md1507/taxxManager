namespace TaxManager.Core;

// TaxRules uses annual personal deduction and a deduction per dependent-month.
public sealed record AnnualSalaryRuleSet
{
    public int Year { get; }
    public TaxRuleSet TaxRules { get; }
    public decimal AnnualPersonalDeduction => TaxRules.PersonalDeduction;
    public decimal DependentMonthDeduction => TaxRules.DependentDeduction;
    public LegalBasis DependentPeriodBasis { get; }
    public LegalBasis SettlementBasis { get; }
    public LegalBasis PeriodBasis { get; }
    public LegalBasis TransitionBasis { get; }
    public AnnualSalaryRuleSet(int year, TaxRuleSet taxRules, LegalBasis dependentPeriodBasis, LegalBasis settlementBasis, LegalBasis periodBasis, LegalBasis transitionBasis)
    {
        if (year is < 1 or > 9999) throw new ArgumentOutOfRangeException(nameof(year));
        ArgumentNullException.ThrowIfNull(taxRules); ArgumentNullException.ThrowIfNull(dependentPeriodBasis);
        ArgumentNullException.ThrowIfNull(settlementBasis); ArgumentNullException.ThrowIfNull(periodBasis); ArgumentNullException.ThrowIfNull(transitionBasis);
        if (taxRules.FromYear != year || taxRules.AnnualSalaryRules is not null) throw new ArgumentException("Annual tax rule year or nesting is invalid.");
        RequirePeriod(taxRules.Period, $"{year} annual", "annual tax rule");
        CheckBasis(taxRules.PersonalDeductionBasis, $"{year} annual");
        CheckBasis(taxRules.DependentDeductionBasis, $"{year} dependent-month");
        CheckBasis(taxRules.TaxableIncomeBasis, $"{year}");
        CheckBasis(taxRules.TaxBasis, $"{year} annual");
        foreach (var bracket in taxRules.Brackets) CheckBasis(bracket.Basis, $"{year} annual");
        foreach (var basis in new[] { dependentPeriodBasis, settlementBasis, periodBasis, transitionBasis }) CheckBasis(basis, $"{year}");
        var yearStart = new DateOnly(year, 1, 1); var yearEnd = new DateOnly(year, 12, 31);
        foreach (var basis in new[] { taxRules.PersonalDeductionBasis, taxRules.DependentDeductionBasis, taxRules.TaxableIncomeBasis, taxRules.TaxBasis, dependentPeriodBasis, settlementBasis, periodBasis, transitionBasis }.Concat(taxRules.Brackets.Select(b => b.Basis)))
            if (basis.EffectiveFrom > yearStart || basis.EffectiveTo < yearEnd)
                throw new ArgumentException("Annual basis effective dates do not cover the full tax year.");
        Year = year; TaxRules = taxRules; DependentPeriodBasis = dependentPeriodBasis; SettlementBasis = settlementBasis; PeriodBasis = periodBasis; TransitionBasis = transitionBasis;
    }

    internal static void RequirePeriod(string? actual, string expected, string name)
    {
        if (actual != expected) throw new ArgumentException($"{name} period mismatch: expected {expected}.");
    }

    internal static void CheckBasis(LegalBasis basis, string period)
    {
        if (basis.Status == VerificationStatus.Missing || string.IsNullOrWhiteSpace(basis.Id) ||
            string.IsNullOrWhiteSpace(basis.Version) || string.IsNullOrWhiteSpace(basis.SourceUrl) ||
            string.IsNullOrWhiteSpace(basis.Article))
            throw new ArgumentException("Annual rule basis requires an ID, version, source and article.");
        RequirePeriod(basis.Period, period, "basis");
    }
}
