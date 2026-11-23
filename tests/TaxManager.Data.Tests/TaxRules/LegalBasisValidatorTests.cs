using TaxManager.Core;
using TaxManager.Data.TaxRules;
using Xunit;

namespace TaxManager.Data.Tests.TaxRules;

public class LegalBasisValidatorTests
{
    [Theory]
    [InlineData("https://vbpl.vn/Pages/vbpq-toanvan.aspx?ItemID=1")]
    [InlineData("https://vbpl.vn/botaichinh/Pages/vbpq-chitiet.aspx?foo=x&ItemID=123")]
    [InlineData("https://sub.vbpl.vn/Pages/vbpq-toanvan.aspx?itemid=42")]
    public void AcceptsVerifiedDocumentDetails(string url)
    {
        LegalBasisValidator.ValidateOfficial(Verified(url));
    }

    [Theory]
    [InlineData("https://vbpl.vn/")]
    [InlineData("https://vbpl.vn/Pages/search.aspx?ItemID=1")]
    [InlineData("https://vbpl.vn/Pages/vbpq-toanvan.aspx")]
    [InlineData("https://vbpl.vn/Pages/vbpq-toanvan.aspx?ItemID=0")]
    [InlineData("https://vbpl.vn/Pages/vbpq-toanvan.aspx?ItemID=-1")]
    [InlineData("https://vbpl.vn/Pages/vbpq-toanvan.aspx?ItemID=1&itemid=2")]
    [InlineData("https://vbpl.vn/Pages/vbpq-toanvan.aspx?ItemID=1.5")]
    [InlineData("https://user@vbpl.vn/Pages/vbpq-toanvan.aspx?ItemID=1")]
    public void RejectsNonDocumentUrls(string url)
    {
        Assert.Throws<ArgumentException>(() => LegalBasisValidator.ValidateOfficial(Verified(url)));
    }

    [Fact]
    public void RejectsMissingAndUnverifiedSources()
    {
        Assert.Throws<ArgumentException>(() => LegalBasisValidator.ValidateOfficial(LegalBasis.Missing));
        Assert.Throws<ArgumentException>(() => LegalBasisValidator.ValidateOfficial(TaxRuleSet.Vietnam2025.TaxBasis));
        Assert.Throws<ArgumentException>(() => LegalBasisValidator.ValidateOfficial(TaxRuleSet.Vietnam2026.TaxBasis));
    }

    private static LegalBasis Verified(string url) => new(
        VerificationStatus.Verified,
        id: "synthetic",
        version: "1",
        sourceUrl: url,
        verifiedOn: new DateOnly(2026, 1, 1));
}
