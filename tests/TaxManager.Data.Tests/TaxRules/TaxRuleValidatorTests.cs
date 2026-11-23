using TaxManager.Core;
using TaxManager.Data.TaxRules;
using Xunit;

namespace TaxManager.Data.Tests.TaxRules;

public class TaxRuleValidatorTests
{
    [Fact]
    public void ReferenceRulesAreSupportedButNotOfficial()
    {
        TaxRuleValidator.ValidateSupported(TaxRuleSet.Vietnam2025);
        TaxRuleValidator.ValidateSupported(TaxRuleSet.Vietnam2026);
        Assert.Throws<ArgumentException>(() => TaxRuleValidator.ValidateOfficial(TaxRuleSet.Vietnam2025));
        Assert.Throws<ArgumentException>(() => TaxRuleValidator.ValidateOfficial(TaxRuleSet.Vietnam2026));
    }

    [Fact]
    public void AttachedInsuranceAndRoundingAreRejected()
    {
        var snapshot = TaxRuleSet.CreateVietnam2026SalaryProvider(InsuranceRegion.I).GetForMonth(2026, 1);
        Assert.Throws<NotSupportedException>(() => TaxRuleValidator.ValidateSupported(snapshot));
    }

    [Fact]
    public void OutOfRangeYearIsRejected()
    {
        var snapshot = new TaxRuleSet("synthetic", "1", "monthly", 10000, 0m, 0m, [new Bracket(null, 0m)]);
        Assert.Throws<ArgumentException>(() => TaxRuleValidator.ValidateSupported(snapshot));
    }
}
