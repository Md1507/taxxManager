using TaxManager.Core;
using Xunit;

namespace TaxManager.Core.Tests;

public class CalculatorTests
{
    public static TheoryData<decimal, decimal> PitCases => new()
    {
        { 0m, 0m }, { 10_000_000m, 500_000m }, { 30_000_000m, 2_500_000m },
        { 60_000_000m, 8_500_000m }, { 100_000_000m, 20_500_000m },
        { 120_000_000m, 27_500_000m }, { 10_000_001m, 500_000.1m },
        { 9_999_999m, 499_999.95m }, { 29_999_999m, 2_499_999.9m }, { 30_000_001m, 2_500_000.2m },
        { 59_999_999m, 8_499_999.8m }, { 60_000_001m, 8_500_000.3m },
        { 99_999_999m, 20_499_999.7m }, { 100_000_001m, 20_500_000.35m },
        { 10_000_000.01m, 500_000.001m }, { 25_000_000m, 2_000_000m }, { 75_000_000m, 13_000_000m }
    };

    [Theory, MemberData(nameof(PitCases))]
    public void PitMatchesIndependentExpected(decimal income, decimal expected)
    {
        var result = PitCalculator.Calculate(income, TaxRuleSet.Vietnam2026);
        Assert.Equal(expected, result.TotalTax.Value);
        Assert.Equal(expected, result.Portions.Sum(p => p.Tax));
        Assert.Equal(income, result.Portions.Sum(p => p.TaxableAmount));
    }

    public static TheoryData<decimal, decimal, int, decimal, decimal, decimal> SalaryCases => new()
    {
        { 30_000_000m, 3_150_000m, 0, 11_350_000m, 11_350_000m, 635_000m },
        { 20_000_000m, 2_100_000m, 1, -3_800_000m, 0m, 0m },
        { 50_000_000m, 5_000_000m, 2, 17_100_000m, 17_100_000m, 1_210_000m },
        { 40_000_000m, 4_000_000m, 1, 14_300_000m, 14_300_000m, 930_000m },
        { 1m, 2m, 0, -15_500_001m, 0m, 0m }
    };

    [Theory, MemberData(nameof(SalaryCases))]
    public void SalaryReturnsIntermediateValues(decimal gross, decimal insurance, int dependents, decimal before, decimal taxable, decimal tax)
    {
        var r = MonthlySalaryCalculator.Calculate(gross, insurance, dependents, TaxRuleSet.Vietnam2026);
        Assert.Equal(gross, r.GrossIncome.Value); Assert.Equal(insurance, r.Insurance.Value);
        Assert.Equal(15_500_000m, r.PersonalDeduction.Value);
        Assert.Equal(dependents * 6_200_000m, r.DependentDeduction.Value);
        Assert.Equal(before, r.TaxableIncomeBeforeClamp.Value); Assert.Equal(taxable, r.TaxableIncome.Value);
        Assert.Equal(tax, r.Pit.TotalTax.Value);
        Assert.Equal(VerificationStatus.Missing, r.Insurance.Basis.Status);
    }

    [Fact]
    public void ValidatesInputsAndOverflow()
    {
        var rule = TaxRuleSet.Vietnam2026;
        Assert.Throws<ArgumentNullException>(() => PitCalculator.Calculate(0, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => PitCalculator.Calculate(-1, rule));
        Assert.Throws<ArgumentOutOfRangeException>(() => MonthlySalaryCalculator.Calculate(-1, 0, 0, rule));
        Assert.Throws<ArgumentOutOfRangeException>(() => MonthlySalaryCalculator.Calculate(0, -1, 0, rule));
        Assert.Throws<ArgumentOutOfRangeException>(() => MonthlySalaryCalculator.Calculate(0, 0, -1, rule));
        Assert.Throws<OverflowException>(() => MonthlySalaryCalculator.Calculate(0, decimal.MaxValue, 0, rule));
    }
}
