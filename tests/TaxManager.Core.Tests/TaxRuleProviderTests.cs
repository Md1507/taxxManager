using TaxManager.Core;
using Xunit;

namespace TaxManager.Core.Tests;

public class TaxRuleProviderTests
{
    static DateOnly D(int y, int m, int d) => new(y, m, d);
    static TaxRuleRegistration R(TaxRuleSet rule, int y, int m1 = 1, int m2 = 12) =>
        new(rule, D(y, m1, 1), D(y, m2, DateTime.DaysInMonth(y, m2)));
    static TaxRuleSet Fixture(string id, decimal personal, decimal dependent, decimal bound, decimal rate1, decimal rate2,
        List<Bracket>? list = null)
    {
        var basis = new LegalBasis(VerificationStatus.Unverified, id, "basis-v", sourceUrl: "https://example.com/fixture", period: id, effectiveFrom: D(2026, 7, 1));
        list ??= [new(bound, rate1, basis), new(null, rate2, basis)];
        return new(id, "v-" + id, "period-" + id, 1999, personal, dependent, list, basis, basis, basis, basis);
    }

    [Fact]
    public void SelectsPeriodNotLegalEffectiveDateAndPreservesSnapshots()
    {
        var basis = new LegalBasis(VerificationStatus.Unverified, "bracket-a", "1", period: "a");
        var brackets = new List<Bracket> { new(100_000_000m, .1m, basis), new(null, .2m, basis) };
        var a = Fixture("a", 10_000_000m, 2_000_000m, 0, 0, 0, brackets);
        var b = Fixture("b", 20_000_000m, 5_000_000m, 50_000_000m, .2m, .3m);
        var entries = new List<TaxRuleRegistration> { R(a, 2025), R(b, 2026) };
        var provider = new TaxRuleProvider(entries);
        var resultA = MonthlySalaryCalculator.Calculate(200_000_000m, 20_000_000m, 1, provider.GetForYear(2025));
        brackets.Clear(); entries.Clear();
        var resultB = MonthlySalaryCalculator.Calculate(200_000_000m, 20_000_000m, 1, provider.GetForDate(D(2026, 6, 30)));
        Assert.Same(a, provider.GetForDate(D(2025, 12, 31)));
        Assert.Same(b, provider.GetForDate(D(2026, 1, 1)));
        Assert.Same(b, provider.GetForDate(D(2026, 7, 1)));
        Assert.Same(b, provider.GetForYear(2026));
        Assert.Equal(23_600_000m, resultA.Pit.TotalTax.Value);
        Assert.Equal(41_500_000m, resultB.Pit.TotalTax.Value);
        foreach (var (result, rule) in new[] { (resultA, a), (resultB, b) })
        {
            var amounts = new[] { result.GrossIncome, result.Insurance, result.PersonalDeduction, result.DependentDeduction,
                result.TaxableIncomeBeforeClamp, result.TaxableIncome, result.Pit.TaxableIncome, result.Pit.TotalTax };
            Assert.All(amounts, amount => { Assert.Same(rule, amount.RuleSet); Assert.Equal(rule.Version, amount.RuleSet.Version); Assert.Equal(rule.Period, amount.RuleSet.Period); });
            Assert.Same(LegalBasis.Missing, result.GrossIncome.Basis); Assert.Same(LegalBasis.Missing, result.Insurance.Basis);
            Assert.Same(rule.PersonalDeductionBasis, result.PersonalDeduction.Basis);
            Assert.Same(rule.DependentDeductionBasis, result.DependentDeduction.Basis);
            Assert.Same(rule.TaxableIncomeBasis, result.TaxableIncomeBeforeClamp.Basis);
            Assert.Same(rule.TaxableIncomeBasis, result.TaxableIncome.Basis);
            Assert.Same(rule.TaxableIncomeBasis, result.Pit.TaxableIncome.Basis);
            Assert.Same(rule.TaxBasis, result.Pit.TotalTax.Basis);
            for (int i = 0; i < rule.Brackets.Count; i++) { Assert.Same(rule, result.Pit.Portions[i].RuleSet); Assert.Same(rule.Brackets[i].Basis, result.Pit.Portions[i].Basis); }
        }
        Assert.Equal(D(2026, 7, 1), b.TaxBasis.EffectiveFrom);
        Assert.Equal("https://example.com/fixture", b.TaxBasis.SourceUrl);
        Assert.Throws<NotSupportedException>(() => ((IList<Bracket>)a.Brackets).Clear());
    }

    [Fact]
    public void RejectsInvalidRangesOverlapAndUnsupportedPeriods()
    {
        var a = TaxRuleSet.Vietnam2025; var b = TaxRuleSet.Vietnam2026;
        Assert.Throws<ArgumentNullException>(() => new TaxRuleProvider(null!));
        Assert.Throws<ArgumentNullException>(() => new TaxRuleRegistration(null!, D(2025, 1, 1), D(2025, 1, 31)));
        Assert.Throws<ArgumentException>(() => new TaxRuleProvider([null!]));
        Assert.Throws<ArgumentException>(() => new TaxRuleRegistration(a, D(2025, 2, 1), D(2025, 1, 31)));
        Assert.Throws<ArgumentException>(() => new TaxRuleRegistration(a, D(2025, 1, 2), D(2025, 1, 31)));
        Assert.Throws<ArgumentException>(() => new TaxRuleRegistration(a, D(2025, 1, 1), D(2025, 1, 30)));
        Assert.Throws<ArgumentException>(() => new TaxRuleProvider([R(a, 2025), R(a, 2025)]));
        Assert.Throws<ArgumentException>(() => new TaxRuleProvider([R(a, 2025, 1, 6), R(b, 2025, 6, 12)]));
        Assert.Throws<ArgumentException>(() => new TaxRuleProvider([R(a, 2025), R(b, 2025, 3, 4)]));
        var empty = new TaxRuleProvider([]);
        Assert.Throws<NotSupportedException>(() => empty.GetForMonth(2025, 1));
        var gap = new TaxRuleProvider([R(a, 2025, 1, 5), R(b, 2025, 7, 12)]);
        Assert.Throws<NotSupportedException>(() => gap.GetForMonth(2025, 6));
        Assert.Throws<NotSupportedException>(() => gap.GetForYear(2025));
        Assert.Throws<NotSupportedException>(() => gap.GetForYear(2027));
        var split = new TaxRuleProvider([R(a, 2025, 1, 6), R(b, 2025, 7, 12)]);
        Assert.Throws<InvalidOperationException>(() => split.GetForYear(2025));
        Assert.Throws<ArgumentOutOfRangeException>(() => split.GetForMonth(0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => split.GetForYear(10000));
        Assert.Throws<ArgumentOutOfRangeException>(() => split.GetForMonth(2025, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => split.GetForMonth(2025, 13));
        var extremes = new TaxRuleProvider([R(a, 1), R(b, 9999)]);
        Assert.Same(a, extremes.GetForDate(DateOnly.MinValue)); Assert.Same(b, extremes.GetForDate(DateOnly.MaxValue));
        Assert.Same(a, extremes.GetForYear(1)); Assert.Same(b, extremes.GetForYear(9999));
    }
}
