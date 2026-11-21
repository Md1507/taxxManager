using TaxManager.Core;
using Xunit;

namespace TaxManager.Core.Tests;

public class AnnualSalaryTests
{
    private static TaxRuleProvider Provider() => TaxRuleSet.CreateVietnam2026AnnualSalaryProvider(InsuranceRegion.I);
    private static SalaryMonthInput[] Months(decimal gross, decimal withheld = 0) => Enumerable.Range(1, 12).Select(m => new SalaryMonthInput(2026, m, gross, withheld, 0)).ToArray();
    private static AnnualSalaryResult Calc(IEnumerable<SalaryMonthInput> months, params DependentPeriod[] periods) => AnnualSalaryCalculator.Calculate(2026, months, periods, Provider());

    [Fact]
    public void AnnualTaxClampsOnceAndUsesActualWithheld()
    {
        var months = Enumerable.Range(1, 12).Select(m => new SalaryMonthInput(2026, m, m <= 6 ? 0 : 40_000_000m, m <= 6 ? 0 : 1_950_000m, 0)).Reverse();
        var r = Calc(months);
        Assert.Equal(240_000_000m, r.GrossIncome); Assert.Equal(54_000_000m, r.TaxableIncome);
        Assert.Equal(2_700_000m, r.Pit.TotalTax.Value); Assert.Equal(11_700_000m, r.Months.Sum(m => m.Salary.Pit.TotalTax.Value));
        Assert.Equal(-15_500_000m, r.Months[0].Salary.TaxableIncomeBeforeClamp.Value);
        Assert.Equal(-9_000_000m, r.Delta); Assert.Equal(9_000_000m, r.EstimatedOverpaid); Assert.Equal(0m, r.AdditionalPayable);
        Assert.Equal(Enumerable.Range(1, 12), r.Months.Select(x => x.Month));
    }

    [Theory]
    [InlineData(0, 7680000, 0)]
    [InlineData(640000, 0, 0)]
    [InlineData(1000000, 0, 4320000)]
    public void DependentMonthsAndThreeDirections(int withheld, int additional, int overpaid)
    {
        var r = Calc(Months(30_000_000m, withheld), new DependentPeriod("child", new(2026, 7, 1), new(2026, 12, 1)));
        Assert.Equal(37_200_000m, r.DependentDeduction); Assert.Equal(136_800_000m, r.TaxableIncome);
        Assert.Equal(7_680_000m, r.Pit.TotalTax.Value); Assert.Equal(additional, r.AdditionalPayable); Assert.Equal(overpaid, r.EstimatedOverpaid);
        Assert.Empty(r.Months[5].Dependents); Assert.Equal("child", r.Months[6].Dependents[0].DependentId);
    }

    [Fact]
    public void PeriodsClipAreInclusiveAndRejectOverlap()
    {
        var r = Calc(Months(0), new(" a ", new(2025, 12, 1), new(2026, 2, 1)), new("a", new(2026, 4, 1), new(2027, 1, 1)));
        Assert.Equal(11, r.DependentMonths); Assert.Empty(r.Months[2].Dependents);
        Assert.Equal(new DateOnly(2026, 1, 1), r.Months[0].Dependents[0].YearFromMonth);
        Assert.Equal(new DateOnly(2026, 12, 1), r.Months[11].Dependents[0].YearToMonth);
        Assert.Throws<ArgumentException>(() => Calc(Months(0), new("a", new(2025, 1, 1), new(2025, 3, 1)), new("a", new(2025, 3, 1), new(2025, 4, 1))));
        Assert.Throws<ArgumentException>(() => new DependentPeriod(" ", new(2026, 1, 1), new(2026, 1, 1)));
        Assert.Throws<ArgumentException>(() => new DependentPeriod("a", new(2026, 1, 2), new(2026, 2, 1)));
    }

    [Fact]
    public void InsuranceUsesBothSnapshotsAndNoDoubleDeduction()
    {
        var inputs = Enumerable.Range(1, 12).Select(m => new SalaryMonthInput(2026, m, 100_000_000m, 0, insuranceInput: new(InsuranceSubject.VietnameseFullMonthEmployee, InsuranceRegion.I, 80_000_000m))).ToArray();
        var r = Calc(inputs);
        Assert.Equal(5_246_000m, r.Months[5].InsuranceDetails!.DeductibleTotal); Assert.Equal(5_607_000m, r.Months[6].InsuranceDetails!.DeductibleTotal);
        Assert.Equal("73/2024/ND-CP", r.Months[5].InsuranceDetails!.Social.Rule.CapValueBasis.DocumentNumber);
        Assert.Equal("161/2026/ND-CP", r.Months[6].InsuranceDetails!.Social.Rule.CapValueBasis.DocumentNumber);
        Assert.Equal(VerificationStatus.Missing, r.Months[5].InsuranceDetails!.InputBasis.Status);
        Assert.Same(r.Months[6].RuleSet, r.Months[6].InsuranceDetails!.TaxRuleSet);
        Assert.Equal(65_118_000m, r.Insurance); Assert.Equal(948_882_000m, r.TaxableIncome); Assert.Equal(170_664_600m, r.Pit.TotalTax.Value);
        Assert.Throws<InvalidOperationException>(() => Provider().GetForYear(2026));
        inputs[0] = new(2026, 1, 0, 0, 0); Assert.Equal(100_000_000m, r.Months[0].Salary.GrossIncome.Value);
    }

    [Theory]
    [InlineData(0, 0)] [InlineData(1, .05)]
    [InlineData(119999999, 5999999.95)] [InlineData(120000000, 6000000)] [InlineData(120000001, 6000000.10)]
    [InlineData(359999999, 29999999.90)] [InlineData(360000000, 30000000)] [InlineData(360000001, 30000000.20)]
    [InlineData(719999999, 101999999.80)] [InlineData(720000000, 102000000)] [InlineData(720000001, 102000000.30)]
    [InlineData(1199999999, 245999999.70)] [InlineData(1200000000, 246000000)] [InlineData(1200000001, 246000000.35)]
    public void AnnualBracketBoundaries(long taxable, double expected)
    {
        var months = Months(0); months[0] = new(2026, 1, 186_000_000m + taxable, 0, 0);
        Assert.Equal((decimal)expected, Calc(months).Pit.TotalTax.Value);
    }

    [Fact]
    public void RejectsInvalidInputsAndPreservesOverflow()
    {
        var months = Months(0);
        Assert.Throws<ArgumentException>(() => Calc(months.Skip(1)));
        months[0] = months[1]; Assert.Throws<ArgumentException>(() => Calc(months));
        Assert.Throws<ArgumentException>(() => new SalaryMonthInput(2026, 1, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SalaryMonthInput(2026, 13, 0, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SalaryMonthInput(2026, 1, -1, 0, 0));
        Assert.Throws<OverflowException>(() => Calc(Months(decimal.MaxValue)));
        Assert.Throws<OverflowException>(() => Calc(Months(0, decimal.MaxValue)));
        var excessiveInsurance = Enumerable.Range(1, 12).Select(m => new SalaryMonthInput(2026, m, 0, 0, decimal.MaxValue));
        Assert.Throws<OverflowException>(() => Calc(excessiveInsurance));
        Assert.Throws<NotSupportedException>(() => AnnualSalaryCalculator.Calculate(2025, Enumerable.Range(1, 12).Select(m => new SalaryMonthInput(2025, m, 0, 0, 0)), [], Provider()));
    }

    [Fact]
    public void ConfigurationAndProvenanceAreChecked()
    {
        var provider = Provider(); var first = provider.GetForMonth(2026, 1); var second = provider.GetForMonth(2026, 7);
        var gap = new TaxRuleProvider([new(first, new(2026, 1, 1), new(2026, 6, 30))]);
        Assert.Throws<NotSupportedException>(() => AnnualSalaryCalculator.Calculate(2026, Months(0), [], gap));
        var missing = TaxRuleSet.CreateVietnam2026SalaryProvider(InsuranceRegion.I);
        Assert.Throws<InvalidOperationException>(() => AnnualSalaryCalculator.Calculate(2026, Months(0), [], missing));
        var changed = new TaxRuleSet(first.Id, first.Version, first.Period, 2026, 15_500_001m, first.DependentDeduction,
            first.Brackets, first.PersonalDeductionBasis, first.DependentDeductionBasis, first.TaxableIncomeBasis, first.TaxBasis,
            second.InsuranceRules, second.RoundingPolicy, second.AnnualSalaryRules);
        var changedProvider = new TaxRuleProvider([new(first, new(2026, 1, 1), new(2026, 6, 30)), new(changed, new(2026, 7, 1), new(2026, 12, 31))]);
        Assert.Throws<NotSupportedException>(() => AnnualSalaryCalculator.Calculate(2026, Months(0), [], changedProvider));
        var r = Calc(Months(0));
        Assert.Equal(VerificationStatus.Unverified, r.Rules.TaxRules.TaxBasis.Status);
        Assert.Null(r.Rules.TaxRules.TaxBasis.VerifiedOn);
        Assert.Equal(VerificationStatus.Missing, r.Months[0].Salary.GrossIncome.Basis.Status);
        Assert.Equal(VerificationStatus.Missing, r.Months[0].WithheldTaxBasis.Status);
        Assert.All(r.Months, m => { Assert.Equal(VerificationStatus.Missing, m.Salary.Insurance.Basis.Status); Assert.Equal(VerificationStatus.Unverified, m.Salary.Pit.TotalTax.Basis.Status); });
        Assert.All(r.Rules.TaxRules.Brackets, b => Assert.Equal(VerificationStatus.Unverified, b.Basis.Status));
        Assert.Equal(-186_000_000m, r.TaxableIncomeBeforeClamp); Assert.Equal(0, r.TaxableIncome);
    }

    [Fact]
    public void InputsAreValidatedAndFractionalInsuranceIsRetained()
    {
        var auto = new InsuranceInput(InsuranceSubject.VietnameseFullMonthEmployee, InsuranceRegion.I, 10_000_000.5m);
        Assert.Throws<ArgumentException>(() => new SalaryMonthInput(2026, 1, 0, 0, 0, auto));
        Assert.Throws<ArgumentNullException>(() => AnnualSalaryCalculator.Calculate(2026, null!, [], Provider()));
        var wrongYear = Months(0); wrongYear[0] = new(2025, 1, 0, 0, 0);
        Assert.Throws<ArgumentException>(() => Calc(wrongYear));
        var list = Enumerable.Range(1, 12).Select(m => new SalaryMonthInput(2026, m, 30_000_000m, 0, insuranceInput: auto)).ToArray();
        var periods = new List<DependentPeriod> { new("x", new(2026, 1, 1), new(2026, 1, 1)) };
        var r = AnnualSalaryCalculator.Calculate(2026, list, periods, Provider());
        periods.Clear();
        Assert.Single(r.Months[0].Dependents);
        Assert.Equal(1_050_000.0525m, r.Months[0].InsuranceDetails!.DeductibleTotal);
        Assert.Equal(12_600_000.6300m, r.Insurance);
    }

    [Fact]
    public void AnnualMetadataIsValidatedEvenWhenAllSnapshotsShareIt()
    {
        var rule = Provider().GetForMonth(2026, 1).AnnualSalaryRules!;
        var t = rule.TaxRules;
        TaxRuleSet Copy(string period, LegalBasis taxBasis, IEnumerable<Bracket>? brackets = null) =>
            new(t.Id, t.Version, period, t.FromYear, t.PersonalDeduction, t.DependentDeduction,
                brackets ?? t.Brackets, t.PersonalDeductionBasis, t.DependentDeductionBasis, t.TaxableIncomeBasis, taxBasis);
        AnnualSalaryRuleSet Build(TaxRuleSet tax, LegalBasis period) =>
            new(2026, tax, rule.DependentPeriodBasis, rule.SettlementBasis, period, rule.TransitionBasis);
        Assert.Throws<ArgumentException>(() => Build(Copy("2025 annual", t.TaxBasis), rule.PeriodBasis));
        Assert.Throws<ArgumentException>(() => Build(Copy(t.Period, LegalBasis.Missing), rule.PeriodBasis));
        var wrongPeriod = new LegalBasis(VerificationStatus.Unverified, "wrong", "1", article: "9", sourceUrl: "source", period: "2025 annual");
        Assert.Throws<ArgumentException>(() => Build(Copy(t.Period, wrongPeriod), rule.PeriodBasis));
        Assert.Throws<ArgumentException>(() => Build(t, wrongPeriod));
        Assert.Throws<ArgumentException>(() => Build(Copy(t.Period, t.TaxBasis, [new(null, .05m, wrongPeriod)]), rule.PeriodBasis));
        var lateBasis = new LegalBasis(VerificationStatus.Unverified, "late", "1", article: "9", sourceUrl: "source", period: "2026 annual", effectiveFrom: new(2026, 7, 1));
        Assert.Throws<ArgumentException>(() => Build(Copy(t.Period, lateBasis), rule.PeriodBasis));
        Assert.Throws<ArgumentException>(() => new AnnualSalaryRuleSet(2025, t, rule.DependentPeriodBasis, rule.SettlementBasis, rule.PeriodBasis, rule.TransitionBasis));
    }

    [Fact]
    public void MissingPolicyAndMismatchedMonthlyBasisAreRejected()
    {
        var s = Provider().GetForMonth(2026, 1);
        TaxRuleSet Copy(SalaryRoundingPolicy? policy, LegalBasis personal) => new(s.Id, s.Version, s.Period, s.FromYear,
            s.PersonalDeduction, s.DependentDeduction, s.Brackets, personal, s.DependentDeductionBasis,
            s.TaxableIncomeBasis, s.TaxBasis, s.InsuranceRules, policy, s.AnnualSalaryRules);
        TaxRuleProvider Wrap(TaxRuleSet rule) => new([new(rule, new(2026, 1, 1), new(2026, 12, 31))]);
        Assert.Throws<InvalidOperationException>(() => AnnualSalaryCalculator.Calculate(2026, Months(0), [], Wrap(Copy(null, s.PersonalDeductionBasis))));
        Assert.Throws<ArgumentException>(() => AnnualSalaryCalculator.Calculate(2026, Months(0), [], Wrap(Copy(s.RoundingPolicy, LegalBasis.Missing))));
        var a = s.AnnualSalaryRules!;
        var otherBasis = new LegalBasis(VerificationStatus.Unverified, "other", "1", article: "48", sourceUrl: "source", period: "2026");
        var otherAnnual = new AnnualSalaryRuleSet(2026, a.TaxRules, otherBasis, a.SettlementBasis, a.PeriodBasis, a.TransitionBasis);
        var changed = new TaxRuleSet(s.Id, s.Version, s.Period, s.FromYear, s.PersonalDeduction, s.DependentDeduction,
            s.Brackets, s.PersonalDeductionBasis, s.DependentDeductionBasis, s.TaxableIncomeBasis, s.TaxBasis, s.InsuranceRules, s.RoundingPolicy, otherAnnual);
        var split = new TaxRuleProvider([new(s, new(2026, 1, 1), new(2026, 6, 30)), new(changed, new(2026, 7, 1), new(2026, 12, 31))]);
        Assert.Throws<ArgumentException>(() => AnnualSalaryCalculator.Calculate(2026, Months(0), [], split));
    }

    [Fact]
    public void RejectsNullNegativeAndInvalidPeriods()
    {
        Assert.Throws<ArgumentNullException>(() => AnnualSalaryCalculator.Calculate(2026, Months(0), null!, Provider()));
        Assert.Throws<ArgumentNullException>(() => AnnualSalaryCalculator.Calculate(2026, Months(0), [], null!));
        var months = Months(0); months[0] = null!;
        Assert.Throws<ArgumentNullException>(() => Calc(months));
        Assert.Throws<ArgumentNullException>(() => Calc(Months(0), null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SalaryMonthInput(2026, 1, 0, -1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SalaryMonthInput(2026, 1, 0, 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SalaryMonthInput(2026, 0, 0, 0, 0));
        Assert.Throws<ArgumentException>(() => new DependentPeriod("x", new(2026, 2, 1), new(2026, 1, 1)));
    }

    [Fact]
    public void RuleAndProviderCopyInputCollections()
    {
        var s = Provider().GetForMonth(2026, 1);
        var brackets = s.Brackets.ToList();
        var copy = new TaxRuleSet(s.Id, s.Version, s.Period, s.FromYear, s.PersonalDeduction, s.DependentDeduction,
            brackets, s.PersonalDeductionBasis, s.DependentDeductionBasis, s.TaxableIncomeBasis, s.TaxBasis, s.InsuranceRules, s.RoundingPolicy, s.AnnualSalaryRules);
        var registrations = new List<TaxRuleRegistration> { new(copy, new(2026, 1, 1), new(2026, 12, 31)) };
        var provider = new TaxRuleProvider(registrations);
        brackets.Clear(); registrations.Clear();
        var result = AnnualSalaryCalculator.Calculate(2026, Months(0), [], provider);
        Assert.Equal(5, result.Months[0].RuleSet.Brackets.Count);
        Assert.Throws<NotSupportedException>(() => ((IList<SalaryMonthResult>)result.Months).Clear());
    }
}
