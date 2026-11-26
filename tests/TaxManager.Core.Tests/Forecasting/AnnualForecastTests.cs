using TaxManager.Core.Forecasting;
using Xunit;

namespace TaxManager.Core.Tests.Forecasting;

public sealed class AnnualForecastTests
{
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
