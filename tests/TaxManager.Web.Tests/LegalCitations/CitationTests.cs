using TaxManager.Core;
using TaxManager.Web.Demo;
using TaxManager.Web.MonthlySalary;
using TaxManager.Web.Tests.MonthlySalary;
using Xunit;

namespace TaxManager.Web.Tests.LegalCitations;

public sealed class CitationTests
{
    [Fact]
    public async Task Rendered_basis_preserves_status_escapes_metadata_and_rejects_unsafe_links()
    {
        using var database = new SyntheticDatabase(Directory.GetCurrentDirectory());
        var real = (await new SalaryCalculator(database).CalculateAsync(2025,
            Enumerable.Repeat(new SalaryInput(30000000m, 0m, 0m), 12).ToArray())).ToArray();
        var unsafeBasis = new LegalBasis(VerificationStatus.Unverified, id: "bad-source", version: "1",
            summary: "<script>alert(1)</script>", sourceUrl: "javascript:alert(1)", period: "2025");
        var verifiedBasis = new LegalBasis(VerificationStatus.Verified, id: "verified-source", version: "2",
            sourceUrl: "https://vbpl.vn/Pages/vbpq-toanvan.aspx?ItemID=123", period: "2025", verifiedOn: new DateOnly(2025, 1, 1));
        var first = real[0];
        real[0] = first with { Result = first.Result with {
            Insurance = first.Result.Insurance with { Basis = unsafeBasis },
            PersonalDeduction = first.Result.PersonalDeduction with { Basis = verifiedBasis }
        } };
        await using var host = await DemoHost.StartAsync(new CitationScenarioCalculator(real));
        using var form = new FormClient(host.Address);
        var html = await form.PostRawAsync();
        Assert.Contains("Missing", html);
        Assert.Contains("Unverified", html);
        Assert.Contains("Verified", html);
        Assert.Contains("verified-source", html);
        Assert.Contains("ItemID=123", html);
        Assert.DoesNotContain("href=\"javascript:", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<script>alert(1)</script>", html);
        Assert.Contains("&lt;script&gt;", html);
        Assert.Contains("2025", html);
    }
}
