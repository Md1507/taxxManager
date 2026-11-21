using TaxManager.Core;
using Xunit;

namespace TaxManager.Core.Tests;

public class MultipleSalaryRuleTests
{
    private const MultipleSalaryScope Scope = MultipleSalaryScope.VietnameseResidentCalendarYearDomesticVndOnlyOrdinarySalary;
    private const SalaryReceiptSubject Subject = SalaryReceiptSubject.OrdinaryEmploymentFullyTaxableGross;
    private static LegalBasis Basis(string id, string period, string version = "1", string url = "https://example.org/source", string article = "1", DateOnly? from = null, DateOnly? to = null) => new(VerificationStatus.Unverified, id, version, article: article, sourceUrl: url, period: period, effectiveFrom: from, effectiveTo: to);
    private static MultipleSalaryRuleSet Rules(string version = "1", LegalBasis? basis = null, DateOnly? from = null, DateOnly? to = null)
    {
        var b = basis ?? Basis("multiple", "2026", version);
        return new("rules", version, 2026, from ?? new(2026, 1, 1), to ?? new(2026, 12, 31), Scope, Subject, MultipleSalaryTiming.ReceivedOrPaidDate, b, b, b, b, b, b, b, b, b);
    }
    private static MultipleSalaryYearInput Input() => new(2026, Scope, true, true, Enumerable.Range(1, 12).Select(m => new MultipleSalaryMonth(2026, m, 1_000_000m,
        [new SalaryPayerMonth("A", [new($"a{m}", new(2026, m, 1), 20_000_000m, 2_000_000m, Subject)]), new SalaryPayerMonth("B", [new($"b{m}", new(2026, m, 1), 10_000_000m, 1_000_000m, Subject)])])),
        [new DependentPeriod("child", new(2026, 7, 1), new(2026, 12, 1))]);
    private static TaxRuleSet Clone(TaxRuleSet s, MultipleSalaryRuleSet? rules) => new(s.Id, s.Version, s.Period, s.FromYear, s.PersonalDeduction, s.DependentDeduction, s.Brackets, s.PersonalDeductionBasis, s.DependentDeductionBasis, s.TaxableIncomeBasis, s.TaxBasis, s.InsuranceRules, s.RoundingPolicy, s.AnnualSalaryRules, s.CashBonusRules, s.SalaryExtraRules, s.CasualWithholdingRules, rules);

    [Fact]
    public void RulesValidateBasisAndCoverageWithValidControl()
    {
        _ = Rules();
        Assert.Throws<ArgumentException>(() => Rules(basis: LegalBasis.Missing));
        Assert.Throws<ArgumentException>(() => Rules(basis: Basis("x", "2026", url: "http://example.org")));
        Assert.Throws<ArgumentException>(() => Rules(basis: Basis("x", "2026", version: "other")));
        Assert.Throws<ArgumentException>(() => Rules(basis: Basis("x", "2025")));
        Assert.Throws<ArgumentException>(() => Rules(basis: Basis("x", "2026", article: "")));
        Assert.Throws<ArgumentException>(() => Rules(basis: Basis("x", "2026", from: new(2026, 2, 1))));
        Assert.Throws<ArgumentException>(() => Rules(basis: Basis("x", "2026", to: new(2026, 11, 30))));
    }

    [Fact]
    public void MonthSnapshotsAndMissingInputsRemainTraceable()
    {
        var source = TaxRuleSet.CreateVietnam2026AnnualSalaryProvider(InsuranceRegion.I);
        var first = Clone(source.GetForMonth(2026, 1), Rules("a")); var second = Clone(source.GetForMonth(2026, 7), Rules("b"));
        var provider = new TaxRuleProvider([new(first, new(2026, 1, 1), new(2026, 6, 30)), new(second, new(2026, 7, 1), new(2026, 12, 31))]);
        var result = MultipleSalaryCalculator.Calculate(Input(), provider);
        Assert.Same(first, result.Months[5].RuleSet); Assert.Same(second, result.Months[6].Payers[0].RuleSet);
        Assert.Equal("a", result.Months[5].Rules.Version); Assert.Equal("b", result.Months[6].Rules.Version);
        Assert.Equal(VerificationStatus.Missing, result.ActualInputBasis.Status);
        var seedProvider = TaxRuleSet.CreateVietnam2026MultipleSalaryProvider(InsuranceRegion.I);
        var seed = seedProvider.GetForMonth(2026, 1).MultipleSalaryRules!;
        Assert.Equal(VerificationStatus.Unverified, seed.AggregationBasis.Status); Assert.Null(seed.AggregationBasis.VerifiedOn);
        var overpayment = seed.OverpaymentContextBasis;
        Assert.Equal("68", overpayment.Article); Assert.Equal("1,2,4", overpayment.Clause); Assert.Null(overpayment.Point);
        Assert.Equal("253/2026/ND-CP", overpayment.DocumentNumber); Assert.Equal("2026", overpayment.Period);
        Assert.Equal("https://datafiles.chinhphu.vn/cpp/files/vbpq/2026/7/253m-ndcp.signed.pdf", overpayment.SourceUrl);
        Assert.Equal(VerificationStatus.Unverified, overpayment.Status); Assert.Null(overpayment.VerifiedOn);
        Assert.Same(overpayment, MultipleSalaryCalculator.Calculate(Input(), seedProvider).Months[0].Rules.OverpaymentContextBasis);
        var missing = Clone(first, null);
        Assert.Throws<InvalidOperationException>(() => MultipleSalaryCalculator.Calculate(Input(), new([new(missing, new(2026, 1, 1), new(2026, 12, 31))])));
        var partial = Clone(first, Rules(from: new(2026, 1, 2)));
        Assert.Throws<ArgumentException>(() => MultipleSalaryCalculator.Calculate(Input(), new([new(partial, new(2026, 1, 1), new(2026, 12, 31))])));
    }

    [Fact]
    public void CustomAnnualRulesDetermineTaxInsteadOfSeedConstants()
    {
        var personal = Basis("p", "2026 annual"); var dependent = Basis("d", "2026 dependent-month");
        var income = Basis("i", "2026"); var annualTax = Basis("at", "2026 annual"); var monthlyTax = Basis("mt", "2026 monthly");
        var annualSet = new TaxRuleSet("custom-annual", "1", "2026 annual", 2026, 120_000_000m, 2_000_000m, [new(60_000_000m, .1m, annualTax), new(null, .2m, annualTax)], personal, dependent, income, annualTax);
        var annual = new AnnualSalaryRuleSet(2026, annualSet, income, income, income, income);
        var policy = new SalaryRoundingPolicy("no-rounding", "1", income);
        var monthly = new TaxRuleSet("custom-monthly", "1", "2026 monthly", 2026, 10_000_000m, 2_000_000m, [new(5_000_000m, .1m, monthlyTax), new(null, .2m, monthlyTax)], personal, dependent, income, monthlyTax, roundingPolicy: policy, annualSalaryRules: annual, multipleSalaryRules: Rules());
        var provider = new TaxRuleProvider([new(monthly, new(2026, 1, 1), new(2026, 12, 31))]);
        var result = MultipleSalaryCalculator.Calculate(Input(), provider);
        Assert.Equal(216_000_000m, result.Annual.TaxableIncome); Assert.Equal(37_200_000m, result.Annual.RawTax);
        Assert.Equal(36_000_000m, result.Annual.TotalWithheldTax); Assert.Equal(1_200_000m, result.Annual.AdditionalPayable);
    }

    [Fact]
    public void ZeroIncomeMonthsStillRequireSnapshotAndWholeMonthCoverage()
    {
        var months = Enumerable.Range(1, 12).Select(m => new MultipleSalaryMonth(2026, m, 0, [])).ToArray();
        var input = new MultipleSalaryYearInput(2026, Scope, true, true, months, []);
        var source = TaxRuleSet.CreateVietnam2026MultipleSalaryProvider(InsuranceRegion.I);
        _ = MultipleSalaryCalculator.Calculate(input, source);
        var snapshot = source.GetForMonth(2026, 1);
        var gap = new TaxRuleProvider([new(snapshot, new(2026, 1, 1), new(2026, 5, 31)), new(snapshot, new(2026, 7, 1), new(2026, 12, 31))]);
        Assert.Throws<NotSupportedException>(() => MultipleSalaryCalculator.Calculate(input, gap));
        var partial = Clone(snapshot, Rules(to: new(2026, 12, 30)));
        Assert.Throws<ArgumentException>(() => MultipleSalaryCalculator.Calculate(input, new([new(partial, new(2026, 1, 1), new(2026, 12, 31))])));
    }

    [Fact]
    public void RuleContractRejectsUnsupportedFieldsAndMissingBasis()
    {
        var b = Basis("b", "2026");
        MultipleSalaryRuleSet Make(MultipleSalaryScope scope, SalaryReceiptSubject subject, MultipleSalaryTiming timing, LegalBasis basis) => new("r", "1", 2026, new(2026, 1, 1), new(2026, 12, 31), scope, subject, timing, basis, b, b, b, b, b, b, b, b);
        _ = Make(Scope, Subject, MultipleSalaryTiming.ReceivedOrPaidDate, b);
        Assert.Throws<ArgumentException>(() => Make(0, Subject, MultipleSalaryTiming.ReceivedOrPaidDate, b));
        Assert.Throws<ArgumentException>(() => Make(Scope, 0, MultipleSalaryTiming.ReceivedOrPaidDate, b));
        Assert.Throws<ArgumentException>(() => Make(Scope, Subject, 0, b));
        Assert.Throws<ArgumentNullException>(() => Make(Scope, Subject, MultipleSalaryTiming.ReceivedOrPaidDate, null!));
        Assert.Throws<ArgumentException>(() => Make(Scope, Subject, MultipleSalaryTiming.ReceivedOrPaidDate, Basis("", "2026")));
        Assert.Throws<ArgumentException>(() => Make(Scope, Subject, MultipleSalaryTiming.ReceivedOrPaidDate, Basis("b", "2026", version: "")));
    }

    [Fact]
    public void MissingAnnualAndPolicyDoNotProduceResult()
    {
        var s = TaxRuleSet.CreateVietnam2026MultipleSalaryProvider(InsuranceRegion.I).GetForMonth(2026, 1);
        TaxRuleSet Make(AnnualSalaryRuleSet? annual, SalaryRoundingPolicy? policy) => new(s.Id, s.Version, s.Period, s.FromYear, s.PersonalDeduction, s.DependentDeduction, s.Brackets, s.PersonalDeductionBasis, s.DependentDeductionBasis, s.TaxableIncomeBasis, s.TaxBasis, s.InsuranceRules, policy, annual, multipleSalaryRules: s.MultipleSalaryRules);
        TaxRuleProvider Provider(TaxRuleSet rule) => new([new(rule, new(2026, 1, 1), new(2026, 12, 31))]);
        _ = MultipleSalaryCalculator.Calculate(Input(), Provider(s));
        Assert.Throws<InvalidOperationException>(() => MultipleSalaryCalculator.Calculate(Input(), Provider(Make(null, s.RoundingPolicy))));
        Assert.Throws<ArgumentException>(() => MultipleSalaryCalculator.Calculate(Input(), Provider(Make(s.AnnualSalaryRules, null))));
    }

    [Fact]
    public void RootMetadataIsCheckedBeforeAnnualCalculation()
    {
        var s = TaxRuleSet.CreateVietnam2026MultipleSalaryProvider(InsuranceRegion.I).GetForMonth(2026, 1);
        TaxRuleSet Make(string period, int year) => new(s.Id, s.Version, period, year, s.PersonalDeduction, s.DependentDeduction, s.Brackets, s.PersonalDeductionBasis, s.DependentDeductionBasis, s.TaxableIncomeBasis, s.TaxBasis, s.InsuranceRules, s.RoundingPolicy, s.AnnualSalaryRules, multipleSalaryRules: s.MultipleSalaryRules);
        TaxRuleProvider P(TaxRuleSet rule) => new([new(rule, new(2026, 1, 1), new(2026, 12, 31))]);
        _ = MultipleSalaryCalculator.Calculate(Input(), P(s));
        Assert.Throws<ArgumentException>(() => MultipleSalaryCalculator.Calculate(Input(), P(Make("2026 annual", 2026))));
        Assert.Throws<ArgumentException>(() => MultipleSalaryCalculator.Calculate(Input(), P(Make("2026 monthly", 2027))));
    }

    [Fact]
    public void DependentDeductionOverflowIsNotSuppressed()
    {
        var p = Basis("p", "2026 annual"); var d = Basis("d", "2026 dependent-month"); var i = Basis("i", "2026");
        var at = Basis("at", "2026 annual"); var mt = Basis("mt", "2026 monthly");
        var annualTax = new TaxRuleSet("a", "1", "2026 annual", 2026, 0, decimal.MaxValue, [new(null, .1m, at)], p, d, i, at);
        var annual = new AnnualSalaryRuleSet(2026, annualTax, i, i, i, i);
        var monthly = new TaxRuleSet("m", "1", "2026 monthly", 2026, 0, decimal.MaxValue, [new(null, .1m, mt)], p, d, i, mt, roundingPolicy: new("no-rounding", "1", i), annualSalaryRules: annual, multipleSalaryRules: Rules());
        var provider = new TaxRuleProvider([new(monthly, new(2026, 1, 1), new(2026, 12, 31))]);
        var months = Enumerable.Range(1, 12).Select(m => new MultipleSalaryMonth(2026, m, 0, []));
        var one = new MultipleSalaryYearInput(2026, Scope, true, true, months, [new("a", new(2026, 1, 1), new(2026, 1, 1))]);
        _ = MultipleSalaryCalculator.Calculate(one, provider);
        var two = new MultipleSalaryYearInput(2026, Scope, true, true, months, [new("a", new(2026, 1, 1), new(2026, 1, 1)), new("b", new(2026, 1, 1), new(2026, 1, 1))]);
        Assert.Throws<OverflowException>(() => MultipleSalaryCalculator.Calculate(two, provider));
    }
}
