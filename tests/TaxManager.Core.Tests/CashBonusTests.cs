using TaxManager.Core;
using Xunit;

namespace TaxManager.Core.Tests;

public class CashBonusTests
{
    private static TaxRuleProvider Provider() => TaxRuleSet.CreateVietnam2026CashBonusProvider(InsuranceRegion.I);
    private static CashBonus Bonus(string id, decimal amount, int month = 12, int year = 2026, int day = 1) => new(id, amount, new(year, month, day), CashBonusTreatment.TaxableCashFromCurrentEmployer, "2025");
    private static SalaryMonthInput Month(int month, params CashBonus[] bonuses) => new(2026, month, 30_000_000m, 950_000m, 0, bonuses: bonuses);
    private static SalaryMonthInput[] Months() => Enumerable.Range(1, 12).Select(m => Month(m)).ToArray();
    private static AnnualSalaryResult Annual(SalaryMonthInput[] months) => AnnualSalaryCalculator.Calculate(2026, months, [], Provider());

    [Fact]
    public void MonthlyAndAnnualUseIndependentExamples()
    {
        var month = BonusMonthlySalaryCalculator.Calculate(Month(12, Bonus("a", 12_000_000), Bonus("b", 8_000_000)), 0, Provider());
        Assert.Equal(50_000_000m, month.TotalGrossIncome); Assert.Equal(34_500_000m, month.Salary.TaxableIncome.Value);
        Assert.Equal(3_400_000m, month.Salary.Pit.TotalTax.Value); Assert.Equal(2, month.Bonuses.Count);
        var inputs = Months(); inputs[11] = Month(12, Bonus("year2025", 20_000_000, day: 31));
        var result = Annual(inputs);
        Assert.Equal(360_000_000m, result.BaseGrossIncome); Assert.Equal(20_000_000m, result.BonusTotal);
        Assert.Equal(380_000_000m, result.GrossIncome); Assert.Equal(194_000_000m, result.TaxableIncome);
        Assert.Equal(13_400_000m, result.RawTax); Assert.Equal(13_850_000m, result.Months.Sum(x => x.Salary.Pit.TotalTax.Value));
        Assert.Equal(11_400_000m, result.TotalWithheldTax); Assert.Equal(2_000_000m, result.Delta);
        Assert.Equal(11_400_000m, Annual(Months()).RawTax);
        var fraction = BonusMonthlySalaryCalculator.Calculate(Month(12, Bonus("fraction", 20_000_000.5m)), 0, Provider());
        Assert.Equal(3_400_000.1m, fraction.Salary.Pit.TotalTax.Value);
    }

    [Fact]
    public void InputsAreExplicitAndImmutable()
    {
        Assert.Throws<ArgumentException>(() => Bonus("  ", 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Bonus("x", -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CashBonus("x", 0, new(2026, 1, 1), 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CashBonus("x", 0, new(2026, 1, 1), (CashBonusTreatment)42));
        var list = new List<CashBonus> { Bonus(" A ", 0, 1), Bonus("a", 1, 1) };
        var input = new SalaryMonthInput(2026, 1, 30_000_000, 0, 0, bonuses: list); list.Clear();
        var result = BonusMonthlySalaryCalculator.Calculate(input, 0, Provider());
        Assert.Equal(2, result.Bonuses.Count); Assert.Equal("A", result.Bonuses[0].Bonus.Id);
        Assert.Throws<NotSupportedException>(() => ((IList<CashBonusLineResult>)result.Bonuses).Clear());
        Assert.Equal(VerificationStatus.Missing, result.Bonuses[0].InputBasis.Status);
        Assert.Equal(VerificationStatus.Unverified, result.Bonuses[0].TaxableBasis.Status);
        Assert.Equal(VerificationStatus.Unverified, result.Bonuses[0].TimingBasis.Status);
        Assert.Same(result.RuleSet, result.Bonuses[0].RuleSet);
        Assert.Equal(new DateOnly(2026, 1, 1), result.Bonuses[0].Bonus.ReceivedOn);
    }

    [Fact]
    public void RejectsDuplicatePeriodsMissingRulesAndOverflow()
    {
        Assert.Throws<ArgumentException>(() => BonusMonthlySalaryCalculator.Calculate(Month(12, Bonus("x", 1), Bonus(" x ", 2)), 0, Provider()));
        var inputs = Months(); inputs[0] = Month(1, Bonus("same", 1, 1)); inputs[11] = Month(12, Bonus(" same ", 1));
        Assert.Throws<ArgumentException>(() => Annual(inputs));
        Assert.Throws<ArgumentException>(() => BonusMonthlySalaryCalculator.Calculate(Month(12, Bonus("x", 1, 1, 2027)), 0, Provider()));
        Assert.Throws<ArgumentException>(() => BonusMonthlySalaryCalculator.Calculate(Month(12, Bonus("x", 1, 11)), 0, Provider()));
        Assert.Throws<ArgumentNullException>(() => BonusMonthlySalaryCalculator.Calculate(Month(12, new CashBonus[] { null! }), 0, Provider()));
        Assert.Throws<InvalidOperationException>(() => BonusMonthlySalaryCalculator.Calculate(Month(12, Bonus("x", 1)), 0, TaxRuleSet.CreateVietnam2026AnnualSalaryProvider(InsuranceRegion.I)));
        Assert.Throws<OverflowException>(() => BonusMonthlySalaryCalculator.Calculate(Month(12, Bonus("x", decimal.MaxValue)), 0, Provider()));
        Assert.Throws<ArgumentException>(() => Annual(Months().Skip(1).ToArray()));
        var dup = Months(); dup[0] = dup[1]; Assert.Throws<ArgumentException>(() => Annual(dup));
        Assert.Throws<NotSupportedException>(() => Provider().GetForMonth(2027, 1));
        var gap = new TaxRuleProvider([new(Provider().GetForMonth(2026, 1), new(2026, 1, 1), new(2026, 6, 30))]);
        Assert.Throws<NotSupportedException>(() => AnnualSalaryCalculator.Calculate(2026, Months(), [], gap));
    }

    [Fact]
    public void InsuranceAndWithheldAreIndependentOfBonus()
    {
        var insurance = new InsuranceInput(InsuranceSubject.VietnameseFullMonthEmployee, InsuranceRegion.I, 30_000_000m);
        var baseInput = new SalaryMonthInput(2026, 7, 30_000_000m, 123, insuranceInput: insurance);
        var bonusInput = new SalaryMonthInput(2026, 7, 30_000_000m, 123, insuranceInput: insurance, bonuses: [Bonus("x", 20_000_000, 7)]);
        var a = BonusMonthlySalaryCalculator.Calculate(baseInput, 1, Provider()); var b = BonusMonthlySalaryCalculator.Calculate(bonusInput, 1, Provider());
        Assert.Equal(3_150_000m, b.InsuranceDetails!.DeductibleTotal); Assert.Equal(a.InsuranceDetails!.DeductibleTotal, b.InsuranceDetails.DeductibleTotal);
        Assert.Equal(25_150_000m, b.Salary.TaxableIncome.Value); Assert.Equal(123, b.ActualWithheldTax);
        Assert.Equal(950_000m, BonusMonthlySalaryCalculator.Calculate(Month(1), 0, Provider()).Salary.Pit.TotalTax.Value);
    }
    [Fact]
    public void CustomSnapshotsAndCoverageAreRespected()
    {
        var source = Provider(); var original = source.GetForMonth(2026, 1);
        LegalBasis Basis(string id) => new(VerificationStatus.Unverified, id, "fixture-v2", article: "test", sourceUrl: "https://example.org/rule", period: "2026");
        CashBonusRuleSet Rule(string id, DateOnly from, DateOnly to) => new(id, "fixture-v2", 2026, from, to, CashBonusTreatment.TaxableCashFromCurrentEmployer, CashBonusTiming.ReceivedOrPaidDate, Basis(id + "-tax"), Basis(id + "-time"));
        TaxRuleSet Snapshot(CashBonusRuleSet bonus) => new(original.Id, original.Version, original.Period, original.FromYear, original.PersonalDeduction, original.DependentDeduction, original.Brackets, original.PersonalDeductionBasis, original.DependentDeductionBasis, original.TaxableIncomeBasis, original.TaxBasis, original.InsuranceRules, original.RoundingPolicy, original.AnnualSalaryRules, bonus);
        var first = Snapshot(Rule("first", new(2026, 1, 1), new(2026, 6, 30)));
        var second = Snapshot(Rule("second", new(2026, 7, 1), new(2026, 12, 31)));
        var provider = new TaxRuleProvider([new(first, new(2026, 1, 1), new(2026, 6, 30)), new(second, new(2026, 7, 1), new(2026, 12, 31))]);
        var inputs = Months(); inputs[0] = Month(1, Bonus("first", 1, 1)); inputs[11] = Month(12, Bonus("second", 1));
        var result = AnnualSalaryCalculator.Calculate(2026, inputs, [], provider);
        Assert.Equal("first", result.Months[0].Bonuses[0].BonusRules.Id); Assert.Equal("second", result.Months[11].Bonuses[0].BonusRules.Id);
        Assert.Equal("fixture-v2", result.Months[11].Bonuses[0].TimingBasis.Version);
        var partial = Snapshot(Rule("partial", new(2026, 1, 2), new(2026, 6, 30)));
        var bad = new TaxRuleProvider([new(partial, new(2026, 1, 1), new(2026, 6, 30))]);
        Assert.Throws<ArgumentException>(() => BonusMonthlySalaryCalculator.Calculate(Month(1, Bonus("x", 1, 1, day: 2)), 0, bad));
        Assert.Throws<ArgumentException>(() => new CashBonusRuleSet("x", "1", 2026, new(2026, 1, 1), new(2026, 12, 31), CashBonusTreatment.TaxableCashFromCurrentEmployer, (CashBonusTiming)99, Basis("x"), Basis("y")));
        Assert.Throws<ArgumentException>(() => new CashBonusRuleSet("x", "1", 2026, new(2026, 1, 1), new(2026, 12, 31), CashBonusTreatment.TaxableCashFromCurrentEmployer, CashBonusTiming.ReceivedOrPaidDate, LegalBasis.Missing, Basis("y")));
    }

    [Fact]
    public void EmptyAndOmittedBonusesKeepLegacyArithmetic()
    {
        var provider = TaxRuleSet.CreateVietnam2026AnnualSalaryProvider(InsuranceRegion.I);
        var omitted = new SalaryMonthInput(2026, 1, 30_000_000m, 0, 0);
        var empty = new SalaryMonthInput(2026, 1, 30_000_000m, 0, 0, bonuses: []);
        var a = BonusMonthlySalaryCalculator.Calculate(omitted, 0, provider);
        var b = BonusMonthlySalaryCalculator.Calculate(empty, 0, provider);
        Assert.Equal(950_000m, a.Salary.Pit.TotalTax.Value); Assert.Equal(a.Salary.Pit.TotalTax.Value, b.Salary.Pit.TotalTax.Value);
        Assert.Empty(a.Bonuses); Assert.Empty(b.Bonuses);
        var zero = BonusMonthlySalaryCalculator.Calculate(Month(1, Bonus("zero", 0, 1)), 0, Provider());
        Assert.Equal(950_000m, zero.Salary.Pit.TotalTax.Value); Assert.Single(zero.Bonuses);
    }

    [Fact]
    public void MonthlyCalculationUsesInjectedTaxRules()
    {
        var original = Provider().GetForMonth(2026, 1);
        var fake = new TaxRuleSet("fake", "1", "2026 monthly", 2026, 1_000_000m, 0, [new(null, .1m)], cashBonusRules: original.CashBonusRules);
        var provider = new TaxRuleProvider([new(fake, new(2026, 1, 1), new(2026, 1, 31))]);
        var result = BonusMonthlySalaryCalculator.Calculate(Month(1, Bonus("x", 20_000_000m, 1)), 0, provider);
        Assert.Equal(49_000_000m, result.Salary.TaxableIncome.Value); Assert.Equal(4_900_000m, result.Salary.Pit.TotalTax.Value);
        Assert.Same(fake, result.Bonuses[0].RuleSet);
    }

}
