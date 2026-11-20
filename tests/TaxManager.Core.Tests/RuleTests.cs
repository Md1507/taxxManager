using TaxManager.Core;
using Xunit;

namespace TaxManager.Core.Tests;

public class RuleTests
{
    private static TaxRuleSet Create(IEnumerable<Bracket> brackets, LegalBasis? basis = null, string id = "a") =>
        new(id, "v1", "period-a", 2026, 10m, 2m, brackets, basis, basis, basis, basis);

    [Fact]
    public void VietnamSeedIsExplicitlyUnverifiedAndMatchesPlan()
    {
        var rule = TaxRuleSet.Vietnam2026;
        Assert.Equal(2026, rule.FromYear); Assert.Equal(15_500_000m, rule.PersonalDeduction);
        Assert.Equal(6_200_000m, rule.DependentDeduction);
        Assert.Equal(new decimal?[] { 10_000_000m, 30_000_000m, 60_000_000m, 100_000_000m, null }, rule.Brackets.Select(b => b.UpperBound));
        Assert.Equal(new[] { .05m, .10m, .20m, .30m, .35m }, rule.Brackets.Select(b => b.Rate));
        Assert.All(rule.Brackets, b => { Assert.Equal(VerificationStatus.Unverified, b.Basis.Status); Assert.Null(b.Basis.SourceUrl); Assert.Null(b.Basis.EffectiveFrom); });
    }

    [Fact]
    public void SnapshotAndMappingUseActualRuleAndBasis()
    {
        var basisA = new LegalBasis(VerificationStatus.Unverified, "basis-a", "1", period: "a");
        var basisB = new LegalBasis(VerificationStatus.Unverified, "basis-b", "2", period: "b");
        var brackets = new List<Bracket> { new(100m, .1m, basisA), new(null, .2m, basisA) };
        var a = Create(brackets, basisA);
        var result = MonthlySalaryCalculator.Calculate(200m, 0m, 1, a);
        brackets[0] = new Bracket(100m, .9m, basisB); brackets.Clear();
        var b = new TaxRuleSet("b", "v2", "period-b", 2027, 0, 0, [new Bracket(null, .5m, basisB)], basisB, basisB, basisB, basisB);
        var other = PitCalculator.Calculate(200m, b);
        Assert.Equal(27.6m, result.Pit.TotalTax.Value);
        Assert.Equal(100m, other.TotalTax.Value);
        Assert.Same(a, result.Pit.Portions[0].RuleSet); Assert.Equal("basis-a", result.Pit.Portions[0].Basis.Id);
        Assert.Equal("v1", result.PersonalDeduction.RuleSet.Version); Assert.Equal("period-a", result.TaxableIncome.RuleSet.Period);
        Assert.Equal("basis-b", other.Portions[0].Basis.Id); Assert.Equal("v2", other.TotalTax.RuleSet.Version);
        Assert.Throws<NotSupportedException>(() => ((IList<Bracket>)a.Brackets).Clear());
    }

    [Fact]
    public void RejectsInvalidRules()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Bracket(null, -0.01m));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Bracket(null, 1.01m));
        Assert.Throws<ArgumentException>(() => Create([]));
        Assert.Throws<ArgumentException>(() => Create([null!]));
        Assert.Throws<ArgumentException>(() => Create([new Bracket(0, .1m), new Bracket(null, .2m)]));
        Assert.Throws<ArgumentException>(() => Create([new Bracket(10, .1m), new Bracket(10, .2m), new Bracket(null, .3m)]));
        Assert.Throws<ArgumentException>(() => Create([new Bracket(null, .1m), new Bracket(null, .2m)]));
        Assert.Throws<ArgumentException>(() => Create([new Bracket(10, .1m)]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TaxRuleSet("a", "v", "p", 2026, -1, 0, [new Bracket(null, 0)]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TaxRuleSet("a", "v", "p", 2026, 0, -1, [new Bracket(null, 0)]));
        Assert.Throws<ArgumentException>(() => new TaxRuleSet(" ", "v", "p", 2026, 0, 0, [new Bracket(null, 0)]));
    }

    [Fact]
    public void SourceStatusIsExplicitAndVerifiedRequiresConsistentEvidence()
    {
        Assert.Equal(VerificationStatus.Missing, new Bracket(null, 0).Basis.Status);
        Assert.Equal(VerificationStatus.Unverified, new LegalBasis(VerificationStatus.Unverified, sourceUrl: "https://vbpl.vn/test").Status);
        foreach (var url in new string?[] { null, "not a url", "https://example.com", "https://vbpl.vn.evil.example", "http://vbpl.vn/test" })
            Assert.Throws<ArgumentException>(() => new LegalBasis(VerificationStatus.Verified, "id", "v", sourceUrl: url, verifiedOn: new DateOnly(2026, 1, 1)));
        Assert.Throws<ArgumentException>(() => new LegalBasis(VerificationStatus.Verified, "id", "v", sourceUrl: "https://vbpl.vn/test"));
        var fixture = new LegalBasis(VerificationStatus.Verified, "id", "v", sourceUrl: "https://example.vbpl.vn/test", verifiedOn: new DateOnly(2026, 1, 1));
        Assert.Equal(VerificationStatus.Verified, fixture.Status); // Model fixture, not a legal verification.
    }
}
