using TaxManager.Core;
using Xunit;

namespace TaxManager.Core.Tests;

public class Vietnam2025Tests
{
    [Fact]
    public void SeedReflectsUserSuppliedDataAndSeparateSources()
    {
        var rule = TaxRuleSet.Vietnam2025;
        Assert.Equal(11_000_000m, rule.PersonalDeduction); Assert.Equal(4_400_000m, rule.DependentDeduction);
        Assert.Equal(new decimal?[] { 5_000_000m, 10_000_000m, 18_000_000m, 32_000_000m, 52_000_000m, 80_000_000m, null }, rule.Brackets.Select(b => b.UpperBound));
        Assert.Equal(new[] { .05m, .10m, .15m, .20m, .25m, .30m, .35m }, rule.Brackets.Select(b => b.Rate));
        Assert.Equal(2025, rule.FromYear); Assert.Contains("2025", rule.Period);
        Assert.Contains("baochinhphu.vn", rule.PersonalDeductionBasis.SourceUrl);
        Assert.Same(rule.PersonalDeductionBasis, rule.DependentDeductionBasis);
        Assert.Null(rule.TaxBasis.SourceUrl); Assert.Equal("111/2013/TT-BTC", rule.TaxBasis.DocumentNumber);
        Assert.Null(rule.TaxBasis.Article); Assert.Null(rule.TaxBasis.EffectiveFrom);
        Assert.Same(LegalBasis.Missing, rule.TaxableIncomeBasis);
        Assert.All(rule.Brackets.Select(b => b.Basis).Append(rule.TaxBasis).Append(rule.PersonalDeductionBasis), b =>
        { Assert.Equal(VerificationStatus.Unverified, b.Status); Assert.Null(b.VerifiedOn); Assert.Equal(rule.Period, b.Period); });
    }

    [Theory]
    [InlineData(5000000, 250000, .05, .10)]
    [InlineData(10000000, 750000, .10, .15)]
    [InlineData(18000000, 1950000, .15, .20)]
    [InlineData(32000000, 4750000, .20, .25)]
    [InlineData(52000000, 9750000, .25, .30)]
    [InlineData(80000000, 18150000, .30, .35)]
    public void BracketBoundaries(decimal bound, decimal tax, decimal belowRate, decimal aboveRate)
    {
        Assert.Equal(tax, PitCalculator.Calculate(bound, TaxRuleSet.Vietnam2025).TotalTax.Value);
        Assert.Equal(tax - belowRate, PitCalculator.Calculate(bound - 1, TaxRuleSet.Vietnam2025).TotalTax.Value);
        Assert.Equal(tax + aboveRate, PitCalculator.Calculate(bound + 1, TaxRuleSet.Vietnam2025).TotalTax.Value);
    }

    [Theory]
    [InlineData(30000000, 3150000, 0, 15850000, 1627500, 11350000, 635000)]
    [InlineData(30000000, 5000000, 1, 9600000, 710000, 3300000, 165000)]
    [InlineData(45000000, 5000000, 2, 20200000, 2390000, 12100000, 710000)]
    public void SameInputsYieldIndependentlyCalculatedResults(decimal gross, decimal insurance, int dependents,
        decimal income25, decimal tax25, decimal income26, decimal tax26)
    {
        var provider = new TaxRuleProvider([
            new(TaxRuleSet.Vietnam2025, new(2025, 1, 1), new(2025, 12, 31)),
            new(TaxRuleSet.Vietnam2026, new(2026, 1, 1), new(2026, 12, 31))]);
        var a = MonthlySalaryCalculator.Calculate(gross, insurance, dependents, provider.GetForDate(new(2025, 12, 31)));
        var b = MonthlySalaryCalculator.Calculate(gross, insurance, dependents, provider.GetForDate(new(2026, 1, 1)));
        Assert.Equal(income25, a.TaxableIncome.Value); Assert.Equal(tax25, a.Pit.TotalTax.Value);
        Assert.Equal(income26, b.TaxableIncome.Value); Assert.Equal(tax26, b.Pit.TotalTax.Value);
        Assert.Same(TaxRuleSet.Vietnam2026, provider.GetForDate(new(2026, 6, 30)));
        Assert.Same(TaxRuleSet.Vietnam2026, provider.GetForDate(new(2026, 7, 1)));
        Assert.Same(TaxRuleSet.Vietnam2025.TaxBasis, a.Pit.TotalTax.Basis);
        Assert.Same(TaxRuleSet.Vietnam2026.TaxBasis, b.Pit.TotalTax.Basis);
        Assert.All(a.Pit.Portions, p => Assert.Same(TaxRuleSet.Vietnam2025.TaxBasis, p.Basis));
        Assert.All(b.Pit.Portions, p => Assert.Same(TaxRuleSet.Vietnam2026.TaxBasis, p.Basis));
    }
}
