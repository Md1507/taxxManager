using TaxManager.Core.Forecasting;
using Xunit;

namespace TaxManager.Core.Tests.Forecasting;

public sealed class AnnualForecastTests
{
    [Fact]
    public void Bonus_in_gross_requires_bonus_rule_for_its_month()
    {
        var when = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var amount = new ForecastValue<decimal>(30_000_000m, ForecastStatus.Actual, "Phiếu", null, when);
        var zero = new ForecastValue<decimal>(0m, ForecastStatus.Actual, "Phiếu", null, when);
        var dependents = new ForecastValue<int>(0, ForecastStatus.Actual, "Tự khai", null, when);
        var months = Enumerable.Range(1, 12).Select(m => new ForecastMonth(m, amount, zero, dependents, null));
        var bonus = new ForecastBonus("bonus", 12, amount, true);

        Assert.Throws<InvalidOperationException>(() => AnnualForecastCalculator.Calculate(2026, months,
            [bonus], TaxRuleSet.CreateVietnam2026AnnualSalaryProvider(InsuranceRegion.I)));
    }

    [Fact]
    public void Missing_obligation_inputs_are_reported_by_month_separately_from_withholding()
    {
        var when = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var actual = new ForecastValue<decimal>(0, ForecastStatus.Actual, "Tự khai", null, when);
        var dependents = new ForecastValue<int>(0, ForecastStatus.Actual, "Tự khai", null, when);
        var months = Enumerable.Range(1, 12).Select(m => new ForecastMonth(m,
            m == 2 ? null : actual, m == 3 ? null : actual, m == 4 ? null : dependents,
            m == 5 ? actual : null)).ToArray();

        var completeness = AnnualForecastCalculator.Assess(months);

        Assert.Equal(new[] { new ForecastMissingInput(2, ForecastInputKind.Gross),
            new ForecastMissingInput(3, ForecastInputKind.Insurance),
            new ForecastMissingInput(4, ForecastInputKind.Dependents) }, completeness.MissingObligation);
        Assert.Equal(9, completeness.ActualObligationMonths);
        Assert.Equal(1, completeness.WithholdingMonths);
        Assert.DoesNotContain(5, completeness.MissingWithholdingMonths);
        Assert.Equal(11, completeness.MissingWithholdingMonths.Count);
    }

    [Fact]
    public void Dependent_period_applies_to_its_months_without_overwriting_monthly_input()
    {
        var when = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var gross = new ForecastValue<decimal>(30_000_000m, ForecastStatus.Actual, "Phiếu", null, when);
        var zero = new ForecastValue<decimal>(0m, ForecastStatus.Actual, "Phiếu", null, when);
        var periodValue = new ForecastValue<int>(1, ForecastStatus.Planned, "Tự khai", null, when);
        var actualZero = new ForecastValue<int>(0, ForecastStatus.Actual, "Tự khai", null, when);
        var months = Enumerable.Range(1, 12).Select(m => new ForecastMonth(m, gross, zero,
            m < 7 || m == 9 ? actualZero : null, null)).ToArray();

        var resolved = ForecastDependentPeriod.Apply(months, [new(7, 12, periodValue)]);
        var result = AnnualForecastCalculator.Calculate(2026, resolved,
            TaxRuleSet.CreateVietnam2026AnnualSalaryProvider(InsuranceRegion.I));

        Assert.Equal(8_300_000m, result.Obligation);
        Assert.Equal(7, result.ActualObligationMonths);
        Assert.Equal(ForecastStatus.Actual, resolved.Single(x => x.Month == 9).Dependents!.Status);
        Assert.Equal(ForecastStatus.Planned, resolved.Single(x => x.Month == 7).Dependents!.Status);
        Assert.Null(months[6].Dependents);
    }

    [Fact]
    public void Saved_bonuses_are_included_once_in_the_annual_obligation_and_explained()
    {
        var when = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        ForecastValue<decimal> Actual(decimal n) => new(n, ForecastStatus.Actual, "Phiếu lương", null, when);
        var planned = new ForecastValue<decimal>(10_000_000m, ForecastStatus.Planned, "Kế hoạch", null, when);
        var dependents = new ForecastValue<int>(0, ForecastStatus.Actual, "Tự khai", null, when);
        var months = Enumerable.Range(1, 12).Select(m => new ForecastMonth(m,
            Actual(30_000_000m), Actual(0m), dependents, null)).ToArray();
        var bonuses = new[] { new ForecastBonus("included", 11, Actual(5_000_000m), true),
            new ForecastBonus("additional", 12, planned, false) };

        var result = AnnualForecastCalculator.Calculate(2026, months, bonuses,
            TaxRuleSet.CreateVietnam2026CashBonusProvider(InsuranceRegion.I));

        Assert.Equal(12_400_000m, result.Obligation);
        Assert.Equal(11, result.ActualObligationMonths);
        Assert.Null(result.Difference);
        Assert.Equal(370_000_000m, result.Details.GrossIncome);
        Assert.Equal(10_000_000m, result.Details.BonusTotal);
        Assert.Equal(5_000_000m, result.Details.BonusAlreadyInGross);
        Assert.Equal(184_000_000m, result.Details.TaxableIncome);
    }

    [Fact]
    public void Bonus_scenario_adds_only_new_bonus_without_changing_base_or_double_counting_gross()
    {
        var when = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        ForecastValue<decimal> V(decimal value) => new(value, ForecastStatus.Actual, "Phiếu lương", null, when);
        var dependents = new ForecastValue<int>(0, ForecastStatus.Actual, "Phiếu lương", null, when);
        var months = Enumerable.Range(1, 12).Select(month => new ForecastMonth(month, V(30_000_000m), V(0m), dependents, null)).ToArray();
        var bonus = new ForecastBonus("existing", 12, V(5_000_000m), IncludedInGross: true);
        var added = new ForecastValue<decimal>(10_000_000m, ForecastStatus.Assumed, "Kịch bản", "Thưởng thêm", when);
        var provider = TaxRuleSet.CreateVietnam2026CashBonusProvider(InsuranceRegion.I);
        var result = AnnualForecastCalculator.CalculateScenarios(2026, months, [bonus], added, 12, provider);
        Assert.Equal(11_400_000m, result.Base.Obligation);
        Assert.Equal(12_400_000m, result.WithBonus.Obligation);
        Assert.Equal(12, result.Base.ActualObligationMonths);
        Assert.Equal(11, result.WithBonus.ActualObligationMonths);
        Assert.Equal(30_000_000m, months[11].Gross!.Value);
    }

    [Fact]
    public void Explicit_zero_withholding_produces_difference_and_incomplete_income_is_rejected()
    {
        var when = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        ForecastValue<decimal> V(decimal value) => new(value, ForecastStatus.Planned, "Kế hoạch", null, when);
        var dependents = new ForecastValue<int>(0, ForecastStatus.Planned, "Kế hoạch", null, when);
        var months = Enumerable.Range(1, 12).Select(month => new ForecastMonth(month, V(30_000_000m), V(0m), dependents, V(0m))).ToArray();
        var provider = TaxRuleSet.CreateVietnam2026AnnualSalaryProvider(InsuranceRegion.I);
        var result = AnnualForecastCalculator.Calculate(2026, months, provider);
        // 120m × 5% + 54m × 10% for 174m annual taxable income.
        Assert.Equal(11_400_000m, result.Obligation);
        Assert.Equal(11_400_000m, result.Difference);
        Assert.Equal(0, result.ActualObligationMonths);
        Assert.Equal(12, result.WithholdingMonths);
        months[0] = months[0] with { Insurance = null };
        Assert.Throws<InvalidOperationException>(() => AnnualForecastCalculator.Calculate(2026, months, provider));
    }

    [Fact]
    public void Withholding_coverage_and_amounts_distinguish_actual_from_planned()
    {
        var when = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        ForecastValue<decimal> V(decimal n, ForecastStatus status) => new(n, status, "Hồ sơ", status == ForecastStatus.Assumed ? "Ngoại suy" : null, when);
        var dependents = new ForecastValue<int>(0, ForecastStatus.Actual, "Hồ sơ", null, when);
        var months = Enumerable.Range(1, 12).Select(m => new ForecastMonth(m,
            V(30_000_000m, ForecastStatus.Actual), V(0, ForecastStatus.Actual), dependents,
            V(500_000m, m <= 4 ? ForecastStatus.Actual : ForecastStatus.Planned))).ToArray();
        var result = AnnualForecastCalculator.Calculate(2026, months, TaxRuleSet.CreateVietnam2026AnnualSalaryProvider(InsuranceRegion.I));
        Assert.Equal(4, result.ActualWithholdingMonths);
        Assert.Equal(2_000_000m, result.ActualWithheld);
        Assert.Equal(4_000_000m, result.ForecastWithheld);
        Assert.Equal(5_400_000m, result.Difference);
    }

    [Fact]
    public void Missing_withholding_keeps_obligation_but_not_difference()
    {
        var when = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        ForecastValue<decimal> V(decimal value) => new(value, ForecastStatus.Actual, "Phiếu lương", null, when);
        ForecastValue<int> D(int value) => new(value, ForecastStatus.Actual, "Đăng ký", null, when);
        var months = Enumerable.Range(1, 12).Select(month => new ForecastMonth(month,
            V(month <= 6 ? 0m : 40_000_000m), V(0m), D(0), null)).ToArray();
        var result = AnnualForecastCalculator.Calculate(2026, months,
            TaxRuleSet.CreateVietnam2026AnnualSalaryProvider(InsuranceRegion.I));

        Assert.Equal(2_700_000m, result.Obligation);
        Assert.Null(result.Difference);
        Assert.Equal(12, result.ActualObligationMonths);
        Assert.Equal(0, result.WithholdingMonths);
        Assert.All(months, month => Assert.Null(month.Withheld));
    }
}
