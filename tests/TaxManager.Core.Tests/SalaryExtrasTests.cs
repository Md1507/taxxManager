using TaxManager.Core;
using Xunit;

namespace TaxManager.Core.Tests;
public class SalaryExtrasTests
{
    static TaxRuleProvider Provider() => TaxRuleSet.CreateVietnam2026SalaryExtraProvider(InsuranceRegion.I);
    static SalaryExtra Work(string id, decimal paid, decimal qualified, int month = 12, SalaryExtraKind kind = SalaryExtraKind.Overtime) => new(id, kind, paid, new(2026, month, 1), qualified, true, "payroll");
    static SalaryExtra Meal(string id, decimal paid, int month = 12, int day = 1) => new(id, SalaryExtraKind.CashMeal, paid, new(2026, month, day));
    static SalaryMonthInput Month(int m, SalaryExtra[]? extras = null, CashBonus[]? bonuses = null, decimal gross = 30_000_000) => new(2026, m, gross, 950_000, 0, bonuses: bonuses, extras: extras);
    static BonusMonthlySalaryResult Calc(SalaryMonthInput input) => BonusMonthlySalaryCalculator.Calculate(input, 0, Provider());
    static SalaryExtra[] Extras() => [Work("ot", 3_000_000, 3_000_000), Work("night", 1_300_000, 1_300_000, kind: SalaryExtraKind.NightWork), Meal("meal", 1_500_000)];
    static CashBonus[] Bonus() => [new("bonus", 20_000_000, new(2026, 12, 1), CashBonusTreatment.TaxableCashFromCurrentEmployer)];
    [Fact] public void MonthlyAndAnnualExamples()
    {
        var r = Calc(Month(12, Extras(), Bonus()));
        Assert.Equal(55_800_000, r.TotalGrossIncome); Assert.Equal(5_800_000, r.ExtraPaid); Assert.Equal(5_500_000, r.ExemptIncome); Assert.Equal(50_300_000, r.TaxableGrossIncome);
        Assert.Equal(34_800_000, r.Salary.TaxableIncome.Value); Assert.Equal(3_460_000, r.Salary.Pit.TotalTax.Value); Assert.Equal(950_000, r.ActualWithheldTax);
        var months = Enumerable.Range(1, 12).Select(m => m == 12 ? Month(m, Extras(), Bonus()) : Month(m)).ToArray();
        var annual = AnnualSalaryCalculator.Calculate(2026, months, [], Provider());
        Assert.Equal(385_800_000, annual.GrossIncome); Assert.Equal(5_500_000, annual.ExemptIncome); Assert.Equal(380_300_000, annual.TaxableGrossIncome); Assert.Equal(194_300_000, annual.TaxableIncome); Assert.Equal(13_430_000, annual.RawTax); Assert.Equal(11_400_000, annual.TotalWithheldTax); Assert.Equal(2_030_000, annual.AdditionalPayable);
        var low = Enumerable.Range(1, 12).Select(m => m == 12 ? Month(m, [Work("ot", 3_000_000, 2_000_000)]) : Month(m, gross: 0)).ToArray();
        var clamped = AnnualSalaryCalculator.Calculate(2026, low, [], Provider()); Assert.Equal(-155_000_000, clamped.TaxableIncomeBeforeClamp); Assert.Equal(0, clamped.TaxableIncome);
    }
    [Theory] [InlineData(0,3000000)] [InlineData(2000000,1000000)] [InlineData(3000000,0)] public void QualifiedAmounts(decimal qualified, decimal taxable)
    { var r = Calc(Month(12, [Work("x", 3_000_000, qualified)])); Assert.Equal(qualified, r.ExemptIncome); Assert.Equal(taxable, r.Extras[0].Taxable); }
    [Theory] [InlineData(800000,800000,0)] [InlineData(1200000,1200000,0)] [InlineData(1500000,1200000,300000)] public void MealCap(decimal paid, decimal exempt, decimal taxable)
    { var r = Calc(Month(12, [Meal("x", paid)])); Assert.Equal(exempt, r.ExemptIncome); Assert.Equal(taxable, r.Extras[0].Taxable); }
    [Fact] public void MealAllocationTimingAndFractions()
    {
        var r = Calc(Month(12, [Meal("b", 700_000, day: 2), Meal("a", 800_000)]));
        Assert.Equal(1_200_000, r.ExemptIncome); Assert.Equal("a", r.Extras[0].Input.Id); Assert.Equal(400_000, r.Extras[1].Exempt);
        Assert.Equal(.5m, Calc(Month(12, [Meal("x", 1_200_000.5m)])).Extras[0].Taxable);
        Assert.Throws<NotSupportedException>(() => Calc(Month(6, [Meal("x", 0, 6, 30)])));
        Assert.Equal(1, Calc(Month(7, [Meal("x", 1, 7)])).ExemptIncome);
        Assert.Equal(1, Calc(Month(1, [Work("x", 1, 1, 1)])).ExemptIncome);
        Assert.Equal(1_200_000, Calc(Month(11, [Meal("x", 1_500_000, 11)])).ExemptIncome);
    }
    [Fact] public void ValidationAndImmutability()
    {
        Assert.Throws<ArgumentException>(() => Work(" ", 1, 1)); Assert.Throws<ArgumentOutOfRangeException>(() => Work("x", -1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Work("x", 1, 2)); Assert.Throws<ArgumentException>(() => new SalaryExtra("x", SalaryExtraKind.Overtime, 1, new(2026,12,1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SalaryExtra("x", (SalaryExtraKind)99, 0, new(2026,12,1)));
        Assert.Throws<ArgumentException>(() => Calc(Month(12, [Work("x", 1, 1), Meal(" x ", 1)])));
        Assert.Throws<ArgumentException>(() => Calc(Month(12, [Work("bonus", 1, 1)], Bonus())));
        Assert.Throws<ArgumentException>(() => Calc(Month(12, [Work("x", 1, 1, 11)])));
        Assert.Throws<InvalidOperationException>(() => BonusMonthlySalaryCalculator.Calculate(Month(12, [Work("x", 1, 1)]), 0, TaxRuleSet.CreateVietnam2026CashBonusProvider(InsuranceRegion.I)));
        Assert.Throws<OverflowException>(() => Calc(Month(12, [Work("x", decimal.MaxValue, 0)])));
        var list = new List<SalaryExtra> { Work(" A ", 1, 1), Work("a", 1, 0) }; var input = Month(12, list.ToArray()); list.Clear(); var result = Calc(input);
        Assert.Equal(2, result.Extras.Count); Assert.Throws<NotSupportedException>(() => ((IList<SalaryExtraLineResult>)result.Extras).Clear());
        Assert.Equal(VerificationStatus.Missing, result.Extras[0].QualifiedInputBasis.Status); Assert.Equal(VerificationStatus.Unverified, result.Extras[0].Entry.Basis.Status);
        var months = Enumerable.Range(1, 12).Select(m => Month(m)).ToArray(); months[0] = Month(1, [Work("bonus", 1, 1, 1)]); months[11] = Month(12, bonuses: Bonus());
        Assert.Throws<ArgumentException>(() => AnnualSalaryCalculator.Calculate(2026, months, [], Provider()));
    }
    [Fact] public void CustomCapCoverageAndInsurance()
    {
        var original = Provider().GetForMonth(2026, 12); var existing = original.SalaryExtraRules!;
        var entry = new SalaryExtraRule(SalaryExtraKind.CashMeal, SalaryExtraMode.MonthlyCap, 900_000, existing.Entries.Single(x => x.Kind == SalaryExtraKind.CashMeal).Basis);
        var extraRule = new SalaryExtraRuleSet("fixture", existing.Version, 2026, existing.PeriodFrom, existing.PeriodTo, existing.Timing, [entry], existing.TimingBasis, existing.PeriodBasis, existing.TransitionBasis);
        var snapshot = new TaxRuleSet(original.Id, original.Version, original.Period, original.FromYear, original.PersonalDeduction, original.DependentDeduction, original.Brackets, original.PersonalDeductionBasis, original.DependentDeductionBasis, original.TaxableIncomeBasis, original.TaxBasis, original.InsuranceRules, original.RoundingPolicy, original.AnnualSalaryRules, original.CashBonusRules, extraRule);
        var provider = new TaxRuleProvider([new(snapshot, new(2026,7,1), new(2026,12,31))]);
        var r = BonusMonthlySalaryCalculator.Calculate(Month(12, [Meal("x", 1_500_000)]), 0, provider);
        Assert.Equal(900_000, r.ExemptIncome); Assert.Equal(600_000, r.Extras[0].Taxable); Assert.Same(extraRule, r.Extras[0].Rules);
        Assert.Throws<NotSupportedException>(() => BonusMonthlySalaryCalculator.Calculate(Month(12, [Work("x", 1, 1)]), 0, provider));
        var insurance = new InsuranceInput(InsuranceSubject.VietnameseFullMonthEmployee, InsuranceRegion.I, 30_000_000);
        var a = new SalaryMonthInput(2026, 12, 30_000_000, 123, insuranceInput: insurance);
        var b = new SalaryMonthInput(2026, 12, 30_000_000, 123, insuranceInput: insurance, extras: Extras(), bonuses: Bonus());
        Assert.Equal(Calc(a).InsuranceDetails!.DeductibleTotal, Calc(b).InsuranceDetails!.DeductibleTotal); Assert.Equal(123, Calc(b).ActualWithheldTax);
    }
    [Fact] public void ExtraGuardsAndCopies()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Work("x", 1, -1));
        Assert.Throws<ArgumentException>(() => new SalaryExtra("x", SalaryExtraKind.Overtime, 1, new(2026,12,1), 1, false));
        Assert.Throws<ArgumentException>(() => new SalaryExtra("x", SalaryExtraKind.CashMeal, 1, new(2026,12,1), 1, true));
        Assert.Throws<ArgumentNullException>(() => Calc(Month(12, [null!])));
        Assert.Throws<ArgumentException>(() => Calc(Month(12, [new SalaryExtra("x", SalaryExtraKind.Overtime, 1, new(2027,1,1), 1, true)])));
        var items = new[] { Work("x", 1, 1) }; var input = Month(12, items); items[0] = Work("changed", 2, 2);
        Assert.Equal("x", Calc(input).Extras[0].Input.Id);
        var r = Calc(Month(12, [Meal("x", 1)]));
        Assert.Same(r.RuleSet.SalaryExtraRules, r.Extras[0].Rules);
        Assert.Equal("8", r.Extras[0].Entry.Basis.Article); Assert.Null(r.Extras[0].Entry.Basis.VerifiedOn);
        Assert.Equal("2026", r.Extras[0].Entry.Basis.Period); Assert.Equal(VerificationStatus.Missing, r.Extras[0].InputBasis.Status);
        var empty = BonusMonthlySalaryCalculator.Calculate(Month(12, []), 0, TaxRuleSet.CreateVietnam2026CashBonusProvider(InsuranceRegion.I));
        Assert.Equal(950_000, empty.Salary.Pit.TotalTax.Value);
        var gap = new TaxRuleProvider([new(Provider().GetForMonth(2026, 1), new(2026,1,1), new(2026,6,30))]);
        Assert.Throws<NotSupportedException>(() => AnnualSalaryCalculator.Calculate(2026, Enumerable.Range(1,12).Select(m => Month(m)), [], gap));
    }
}

public class SalaryExtraRuleValidationTests
{
    private static readonly DateOnly Start = new(2026, 7, 1);
    private static readonly DateOnly End = new(2026, 12, 31);

    private static LegalBasis Basis(string version = "fixture", string period = "2026",
        string url = "https://example.org/rule", DateOnly? from = null, DateOnly? to = null) =>
        new(VerificationStatus.Unverified, id: "fixture", version: version, article: "1",
            sourceUrl: url, period: period, effectiveFrom: from, effectiveTo: to);

    private static SalaryExtraRuleSet Rules(int year = 2026, CashBonusTiming timing = CashBonusTiming.ReceivedOrPaidDate,
        DateOnly? from = null, DateOnly? to = null, LegalBasis? basis = null)
    {
        var b = basis ?? Basis(period: year.ToString());
        return new("fixture", "fixture", year, from ?? Start, to ?? End, timing,
            [new(SalaryExtraKind.Overtime, SalaryExtraMode.ConfirmedQualifiedAmount, null, b)],
            Basis(period: year.ToString()), Basis(period: year.ToString()), Basis(period: year.ToString()));
    }

    [Fact]
    public void RuleSetRejectsWrongYearAndTimingWithValidControl()
    {
        Assert.Equal(2026, Rules().Year);
        Assert.Throws<ArgumentException>(() => Rules(year: 2027, from: new(2027, 7, 1), to: new(2027, 12, 31)));
        Assert.Throws<ArgumentException>(() => Rules(timing: CashBonusTiming.Unspecified));
    }

    [Fact]
    public void RuleSetRejectsInvalidBasisMetadataWithValidControl()
    {
        Assert.Equal("2026", Rules(basis: Basis()).Entries[0].Basis.Period);
        Assert.Throws<ArgumentException>(() => Rules(basis: LegalBasis.Missing));
        Assert.Throws<ArgumentException>(() => Rules(basis: Basis(period: "2025")));
        Assert.Throws<ArgumentException>(() => Rules(basis: Basis(version: "other")));
        Assert.Throws<ArgumentException>(() => Rules(basis: Basis(url: "http://example.org/rule")));
        Assert.Throws<ArgumentException>(() => Rules(basis: Basis(from: new(2026, 7, 2))));
        Assert.Throws<ArgumentException>(() => Rules(basis: Basis(to: new(2026, 12, 30))));
    }

    private static BonusMonthlySalaryResult Calculate(SalaryExtraRuleSet rules)
    {
        var original = TaxRuleSet.CreateVietnam2026SalaryExtraProvider(InsuranceRegion.I).GetForMonth(2026, 12);
        var snapshot = new TaxRuleSet(original.Id, original.Version, original.Period, original.FromYear,
            original.PersonalDeduction, original.DependentDeduction, original.Brackets,
            original.PersonalDeductionBasis, original.DependentDeductionBasis, original.TaxableIncomeBasis,
            original.TaxBasis, original.InsuranceRules, original.RoundingPolicy, original.AnnualSalaryRules,
            original.CashBonusRules, rules);
        var provider = new TaxRuleProvider([new(snapshot, Start, End)]);
        var extra = new SalaryExtra("fixture", SalaryExtraKind.Overtime, 3_000_000, new(2026, 12, 15), 2_000_000, true);
        return BonusMonthlySalaryCalculator.Calculate(new SalaryMonthInput(2026, 12, 30_000_000, 0, 0, extras: [extra]), 0, provider);
    }

    [Fact]
    public void CalculatorRequiresRuleCoverageOfEntireMonth()
    {
        var result = Calculate(Rules());
        Assert.Equal(2_000_000, result.ExemptIncome);
        Assert.Equal(1_000_000, result.Extras[0].Taxable);
        Assert.Throws<ArgumentException>(() => Calculate(Rules(from: new(2026, 12, 2))));
        Assert.Throws<ArgumentException>(() => Calculate(Rules(to: new(2026, 12, 30))));
    }
}
