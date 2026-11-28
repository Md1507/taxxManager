using System.Text.RegularExpressions;
using TaxManager.Core;
using TaxManager.Web.Demo;
using TaxManager.Web.MonthlySalary;
using TaxManager.Web.Tests.MonthlySalary;
using Xunit;

namespace TaxManager.Web.Tests.LegalCitations;

public sealed class CitationTests
{
    [Fact]
    public async Task Different_bracket_sources_keep_their_applicable_ranges()
    {
        using var database = new SyntheticDatabase(Directory.GetCurrentDirectory());
        var snapshots = (await new SalaryCalculator(database).CalculateAsync(2025,
            Enumerable.Repeat(new SalaryInput(30000000m, 0m, 0m), 12).ToArray())).ToArray();
        var first = snapshots[0];
        var separateBasis = new LegalBasis(VerificationStatus.Unverified, id: "separate-first-bracket", version: "1");
        snapshots[0] = first with { Result = first.Result with {
            Portions = first.Result.Portions.SetItem(0, first.Result.Portions[0] with { Basis = separateBasis })
        } };
        await using var host = await DemoHost.StartAsync(new CitationScenarioCalculator(snapshots));
        using var form = new FormClient(host.Address);
        var html = System.Net.WebUtility.HtmlDecode(await form.PostRawAsync());
        var firstMonth = Regex.Match(html, "<section class=\"monthly-bases\".*?</section>", RegexOptions.Singleline).Value;
        var records = firstMonth.Split("<details class=\"basis-provenance\">")[1];
        Assert.DoesNotContain("Bậc 0", firstMonth.Split("<details class=\"basis-provenance\">")[0]);
        Assert.Matches("(?s)Bậc 0 – 5.000.000 \\(5%\\)</strong>.*?separate-first-bracket", records);
        Assert.Contains("Bậc 5.000.000 – 10.000.000 (10%)", records);
    }

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
        var sameDocumentBasis = new LegalBasis(VerificationStatus.Unverified, id: "same-document-other-clause", version: "2",
            clause: "2", sourceUrl: "https://vbpl.vn/Pages/vbpq-toanvan.aspx?ItemID=123", period: "2025");
        var first = real[0];
        real[0] = first with { Result = first.Result with {
            Insurance = first.Result.Insurance with { Basis = unsafeBasis },
            PersonalDeduction = first.Result.PersonalDeduction with { Basis = verifiedBasis },
            DependentDeduction = first.Result.DependentDeduction with { Basis = sameDocumentBasis }
        } };
        await using var host = await DemoHost.StartAsync(new CitationScenarioCalculator(real));
        using var form = new FormClient(host.Address);
        var html = await form.PostRawAsync();
        Assert.Contains("Missing", html);
        Assert.Contains("Unverified", html);
        Assert.Contains("Verified", html);
        Assert.Contains("verified-source", html);
        Assert.Contains("same-document-other-clause", html);
        Assert.DoesNotContain("<details class=\"basis\">", html);
        Assert.Equal(12, Regex.Matches(html, "class=\"monthly-bases\"").Count);
        var firstMonthBases = System.Net.WebUtility.HtmlDecode(Regex.Match(html, "<section class=\"monthly-bases\".*?</section>", RegexOptions.Singleline).Value);
        Assert.Matches("(?s)Bảo hiểm</strong>.*?bad-source", firstMonthBases);
        Assert.Matches("(?s)Giảm trừ bản thân</strong>.*?verified-source", firstMonthBases);
        Assert.Matches("(?s)Giảm trừ người phụ thuộc</strong>.*?same-document-other-clause", firstMonthBases);
        Assert.Contains("ItemID=123", html);
        Assert.Single(Regex.Matches(html, "href=\"https://vbpl.vn/Pages/vbpq-toanvan.aspx\\?ItemID=123\""));
        var sourceLists = Regex.Matches(html, "<div class=\"legal-sources\">(.*?)</div>", RegexOptions.Singleline);
        Assert.Equal(12, sourceLists.Count);
        foreach (Match sourceList in sourceLists)
        {
            var urls = Regex.Matches(sourceList.Value, "href=\"([^\"]+)\"")
                .Select(match => match.Groups[1].Value).ToArray();
            Assert.Equal(urls.Length, urls.Distinct(StringComparer.Ordinal).Count());
        }
        Assert.DoesNotContain("href=\"javascript:", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<script>alert(1)</script>", html);
        Assert.Contains("&lt;script&gt;", html);
        Assert.Contains("2025", html);
        var firstMonth = Regex.Match(html, "<section class=\"monthly-bases\".*?</section>", RegexOptions.Singleline).Value;
        var visibleReferences = firstMonth.Split("<details class=\"basis-provenance\">")[0];
        Assert.Contains("Thiếu nguồn", System.Net.WebUtility.HtmlDecode(visibleReferences));
        Assert.Contains("Chưa xác minh", System.Net.WebUtility.HtmlDecode(visibleReferences));
        Assert.Contains("Đã xác minh", System.Net.WebUtility.HtmlDecode(visibleReferences));
        Assert.DoesNotContain("Trạng thái nguồn:", visibleReferences);
        Assert.DoesNotContain("chưa có mã", visibleReferences);
        Assert.DoesNotContain("Bậc 0", System.Net.WebUtility.HtmlDecode(visibleReferences));
        Assert.Contains("Biểu thuế tháng", System.Net.WebUtility.HtmlDecode(visibleReferences));
        Assert.Contains("data-verification=\"Unverified\"", visibleReferences);
        Assert.Equal(12, Regex.Matches(html, "<details class=\"basis-provenance\">").Count);
        Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;", firstMonth.Split("<details class=\"basis-provenance\">")[1]);
    }
}
