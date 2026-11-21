using TaxManager.Core;
using Xunit;

namespace TaxManager.Core.Tests;

public class InsuranceTests
{
    private static InsuranceInput Input(InsuranceRegion region, decimal amount) => new(InsuranceSubject.VietnameseFullMonthEmployee, region, amount);

    [Theory]
    [InlineData(1, InsuranceRegion.I, "30000000", "30000000", 0, "2400000", "450000", "300000", "11350000", "635000")]
    [InlineData(7, InsuranceRegion.II, "40000000", "20000000", 1, "1600000", "300000", "200000", "16200000", "1120000")]
    [InlineData(6, InsuranceRegion.I, "100000000", "80000000", 0, "3744000", "702000", "800000", "79254000", "14276200")]
    [InlineData(7, InsuranceRegion.I, "100000000", "80000000", 0, "4048000", "759000", "800000", "78893000", "14167900")]
    [InlineData(1, InsuranceRegion.IV, "150000000", "120000000", 0, "3744000", "702000", "740000", "129314000", "30759900")]
    [InlineData(1, InsuranceRegion.IV, "30000000", "10000000.5", 0, "800000.04", "150000.0075", "100000.005", "13449999.9475", "844999.99475")]
    public void IndependentSalaryExamples(int month, InsuranceRegion region, string gross, string basis, int dependents, string social, string health, string unemployment, string taxable, string tax)
    {
        decimal D(string value) => decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        var result = InsuredMonthlySalaryCalculator.Calculate(D(gross), Input(region, D(basis)), dependents, 2026, month, TaxRuleSet.CreateVietnam2026SalaryProvider(region));
        Assert.Equal(D(social), result.Insurance.Social.Raw); Assert.Equal(D(health), result.Insurance.Health.Raw);
        Assert.Equal(D(unemployment), result.Insurance.Unemployment.Raw); Assert.Equal(D(taxable), result.TaxableIncome.Value);
        Assert.Equal(D(tax), result.Tax.Raw); Assert.Equal(result.Tax.Raw, result.Tax.Applied);
        Assert.Equal(result.Insurance.TotalRaw, result.Insurance.TotalApplied); Assert.Equal(result.Insurance.TotalRaw, result.Insurance.DeductibleTotal);
        Assert.Equal(result.Insurance.Social.Raw, result.Insurance.Social.Applied);
        Assert.Equal(result.Insurance.Health.Raw, result.Insurance.Health.Applied);
        Assert.Equal(result.Insurance.Unemployment.Raw, result.Insurance.Unemployment.Applied);
    }

    [Theory]
    [InlineData(InsuranceRegion.I, 5310000, 106200000)]
    [InlineData(InsuranceRegion.II, 4730000, 94600000)]
    [InlineData(InsuranceRegion.III, 4140000, 82800000)]
    [InlineData(InsuranceRegion.IV, 3700000, 74000000)]
    public void GuardsAndCapsAreIndependent(InsuranceRegion region, int minimum, int unemploymentCap)
    {
        var rule = TaxRuleSet.CreateVietnam2026SalaryProvider(region).GetForMonth(2026, 1);
        InsuranceResult Calc(decimal salary) => InsuranceCalculator.Calculate(Input(region, salary), rule, 2026, 1);
        Assert.Throws<NotSupportedException>(() => Calc(minimum - 1m));
        Assert.Equal(minimum * .08m, Calc(minimum).Social.Raw); Assert.Equal((minimum + 1m) * .01m, Calc(minimum + 1m).Unemployment.Raw);
        foreach (decimal amount in new[] { 46_799_999m, 46_800_000m, 46_800_001m, 46_800_000.5m })
        {
            var result = Calc(amount);
            Assert.Equal(Math.Min(amount, 46_800_000m), result.Social.AppliedBase);
            Assert.Equal(result.Social.AppliedBase, result.Health.AppliedBase);
            Assert.Equal(amount, result.Unemployment.AppliedBase);
        }
        Assert.Equal(unemploymentCap - 1m, Calc(unemploymentCap - 1m).Unemployment.AppliedBase);
        Assert.Equal(unemploymentCap, Calc(unemploymentCap).Unemployment.AppliedBase);
        Assert.Equal(unemploymentCap, Calc(unemploymentCap + .5m).Unemployment.AppliedBase);
    }

    [Fact]
    public void SnapshotAndValidationAreExplicit()
    {
        var provider = TaxRuleSet.CreateVietnam2026SalaryProvider(InsuranceRegion.I);
        var june = provider.GetForDate(new(2026, 6, 30)); var july = provider.GetForDate(new(2026, 7, 1));
        Assert.Equal(46_800_000m, june.InsuranceRules!.Social.Cap); Assert.Equal(50_600_000m, july.InsuranceRules!.Social.Cap);
        Assert.Equal("73/2024/ND-CP", june.InsuranceRules.Social.CapValueBasis.DocumentNumber);
        Assert.Equal("161/2026/ND-CP", july.InsuranceRules.Social.CapValueBasis.DocumentNumber);
        var result = InsuranceCalculator.Calculate(Input(InsuranceRegion.I, 6_000_000), june, 2026, 6);
        Assert.Same(june, result.TaxRuleSet); Assert.Same(june.InsuranceRules, result.Rules);
        Assert.Equal(VerificationStatus.Missing, result.InputBasis.Status);
        Assert.Equal(VerificationStatus.Unverified, result.Policy.Basis.Status);
        Assert.Equal(VerificationStatus.Unverified, result.Social.Rule.RateBasis.Status);
        Assert.Equal(2_340_000m, result.Social.Rule.Floor); Assert.Equal(5_310_000m, result.Rules.ScopeMinimumSalary);
        Assert.Throws<NotSupportedException>(() => provider.GetForMonth(2025, 12)); Assert.Throws<NotSupportedException>(() => provider.GetForMonth(2027, 1));
        Assert.Throws<ArgumentException>(() => InsuranceCalculator.Calculate(Input(InsuranceRegion.II, 6_000_000), june, 2026, 6));
        Assert.Throws<ArgumentException>(() => InsuranceCalculator.Calculate(Input(InsuranceRegion.I, 6_000_000), june, 2026, 7));
        Assert.Throws<ArgumentOutOfRangeException>(() => InsuranceCalculator.Calculate(Input(InsuranceRegion.I, -1), june, 2026, 6));
        Assert.Throws<ArgumentOutOfRangeException>(() => InsuranceCalculator.Calculate(Input((InsuranceRegion)0, 6_000_000), june, 2026, 6));
        Assert.Throws<NotSupportedException>(() => InsuranceCalculator.Calculate(new((InsuranceSubject)0, InsuranceRegion.I, 6_000_000), june, 2026, 6));
        Assert.Throws<InvalidOperationException>(() => InsuranceCalculator.Calculate(Input(InsuranceRegion.I, 6_000_000), TaxRuleSet.Vietnam2026, 2026, 6));
        Assert.Throws<ArgumentNullException>(() => InsuranceCalculator.Calculate(null!, june, 2026, 6));
        Assert.Throws<InvalidOperationException>(() => provider.GetForYear(2026));
    }

    [Fact]
    public void CustomRulesDriveCalculationAndOverflowIsNotHidden()
    {
        var b = LegalBasis.Missing;
        var social = new InsuranceComponentRule(.2m, 1m, 100m, b, b, b, b);
        var health = new InsuranceComponentRule(.03m, 1m, 100m, b, b, b, b);
        var unemployment = new InsuranceComponentRule(.04m, null, 200m, b, null, b, b);
        var rules = new InsuranceRuleSet("fixture", "1", InsuranceSubject.VietnameseFullMonthEmployee, InsuranceRegion.I, new(2026, 1, 1), new(2026, 12, 31), 2m, b, social, health, unemployment);
        var policy = new SalaryRoundingPolicy("no-rounding", "1", b);
        var tax = new TaxRuleSet("fixture", "1", "test", 2026, 0, 0, [new(null, .1m)], insuranceRules: rules, roundingPolicy: policy);
        var result = InsuranceCalculator.Calculate(Input(InsuranceRegion.I, 150m), tax, 2026, 1);
        Assert.Equal(20m, result.Social.Raw); Assert.Equal(3m, result.Health.Raw); Assert.Equal(6m, result.Unemployment.Raw);
        var maximum = new InsuranceComponentRule(1m, null, decimal.MaxValue, b, null, b, b);
        var huge = new InsuranceRuleSet("huge", "1", InsuranceSubject.VietnameseFullMonthEmployee, InsuranceRegion.I, new(2026, 1, 1), new(2026, 12, 31), 0m, b, maximum, maximum, maximum);
        var hugeTax = new TaxRuleSet("huge", "1", "test", 2026, 0, 0, [new(null, 0m)], insuranceRules: huge, roundingPolicy: policy);
        Assert.Throws<OverflowException>(() => InsuranceCalculator.Calculate(Input(InsuranceRegion.I, decimal.MaxValue), hugeTax, 2026, 1));
        var missingPolicy = new TaxRuleSet("missing", "1", "test", 2026, 0, 0, [new(null, 0m)], insuranceRules: rules);
        Assert.Throws<InvalidOperationException>(() => InsuranceCalculator.Calculate(Input(InsuranceRegion.I, 2m), missingPolicy, 2026, 1));
    }

    [Fact]
    public void UnemploymentFloorIsRejectedInsteadOfSilentlyIgnored()
    {
        var b = LegalBasis.Missing;
        var social = new InsuranceComponentRule(.08m, 1m, 100m, b, b, b, b);
        var unemployment = new InsuranceComponentRule(.01m, 5m, 100m, b, b, b, b);
        var error = Assert.Throws<ArgumentException>(() => new InsuranceRuleSet("id", "1", InsuranceSubject.VietnameseFullMonthEmployee,
            InsuranceRegion.I, new(2026, 1, 1), new(2026, 12, 31), 2m, b, social, social, unemployment));
        Assert.Equal("unemployment", error.ParamName);
    }

    [Theory]
    [InlineData(6)]
    [InlineData(7)]
    public void UnemploymentSourcesIdentifyRateAndCapSeparately(int month)
    {
        var snapshot = TaxRuleSet.CreateVietnam2026SalaryProvider(InsuranceRegion.I).GetForMonth(2026, month);
        var rule = InsuranceCalculator.Calculate(Input(InsuranceRegion.I, 6_000_000m), snapshot, 2026, month).Unemployment.Rule;
        Assert.Equal(("374/2025/ND-CP", "4", "1", "https://datafiles.chinhphu.vn/cpp/files/vbpq/2026/01/374-1.signed.pdf"),
            (rule.RateBasis.DocumentNumber, rule.RateBasis.Article, rule.RateBasis.Clause, rule.RateBasis.SourceUrl));
        Assert.Equal(("74/2025/QH15", "34", "2", "https://xaydungchinhsach.chinhphu.vn/toan-van-luat-viec-lam-119250711173403835.htm"),
            (rule.CapFormulaBasis.DocumentNumber, rule.CapFormulaBasis.Article, rule.CapFormulaBasis.Clause, rule.CapFormulaBasis.SourceUrl));
        Assert.Equal(("293/2025/ND-CP", "3", "1", "https://xaydungchinhsach.chinhphu.vn/nghi-dinh-so-293-2025-nd-cp-quy-dinh-muc-luong-toi-thieu-doi-voi-nguoi-lao-dong-lam-viec-theo-hop-dong-lao-dong-119251110172808433.htm"),
            (rule.CapValueBasis.DocumentNumber, rule.CapValueBasis.Article, rule.CapValueBasis.Clause, rule.CapValueBasis.SourceUrl));
        Assert.Equal(VerificationStatus.Unverified, rule.RateBasis.Status);
        Assert.Equal(VerificationStatus.Unverified, rule.CapFormulaBasis.Status);
        Assert.Equal(VerificationStatus.Unverified, rule.CapValueBasis.Status);
    }

    [Fact]
    public void RejectsInvalidConfigurationAndProviderDoesNotFillGaps()
    {
        var b = LegalBasis.Missing;
        Assert.Throws<ArgumentOutOfRangeException>(() => new InsuranceComponentRule(1.01m, null, 100m, b, null, b, b));
        Assert.Throws<ArgumentException>(() => new InsuranceComponentRule(.1m, 101m, 100m, b, b, b, b));
        var rule = new InsuranceComponentRule(.1m, 1m, 100m, b, b, b, b);
        Assert.Throws<ArgumentException>(() => new InsuranceRuleSet("id", "1", InsuranceSubject.VietnameseFullMonthEmployee, InsuranceRegion.I, new(2026, 1, 2), new(2026, 1, 31), 2m, b, rule, rule, rule));
        Assert.Throws<ArgumentException>(() => new InsuranceRuleSet("id", "1", InsuranceSubject.VietnameseFullMonthEmployee, InsuranceRegion.I, new(2026, 1, 1), new(2026, 1, 31), 101m, b, rule, rule, rule));
        var source = new List<TaxRuleRegistration> { new(TaxRuleSet.Vietnam2026, new(2026, 1, 1), new(2026, 1, 31)), new(TaxRuleSet.Vietnam2026, new(2026, 3, 1), new(2026, 3, 31)) };
        var provider = new TaxRuleProvider(source); source.Clear();
        Assert.Same(TaxRuleSet.Vietnam2026, provider.GetForMonth(2026, 1));
        Assert.Throws<NotSupportedException>(() => provider.GetForMonth(2026, 2));
        Assert.Throws<ArgumentException>(() => new TaxRuleProvider([new(TaxRuleSet.Vietnam2026, new(2026, 1, 1), new(2026, 2, 28)), new(TaxRuleSet.Vietnam2026, new(2026, 2, 1), new(2026, 3, 31))]));
    }
}
