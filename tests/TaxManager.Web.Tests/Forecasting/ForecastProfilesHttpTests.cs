using System.Net;
using System.Text.RegularExpressions;
using Xunit;

namespace TaxManager.Web.Tests.Forecasting;

public sealed class ForecastProfilesHttpTests
{
    [Fact]
    public async Task Section_navigation_stays_in_the_posted_preview_document()
    {
        await using var host = await DemoHost.StartAsync();
        using var client = new HttpClient(new HttpClientHandler { UseCookies = true }) { BaseAddress = host.Address };
        var editor = await client.GetStringAsync("/Forecast");
        var fields = new Dictionary<string, string> {
            ["__RequestVerificationToken"] = Regex.Match(editor, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value,
            ["FromMonth"] = "1", ["ToMonth"] = "12", ["Gross"] = "12345678"
        };
        using var response = await client.PostAsync("/Forecast?handler=Preview", new FormUrlEncodedContent(fields));
        response.EnsureSuccessStatusCode();
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        var navigation = Regex.Match(html, "<nav\\b[^>]*>[\\s\\S]*?</nav>").Value;
        var document = new Uri(host.Address, "/Forecast?handler=Preview");
        var positions = new List<int>();
        foreach (Match link in Regex.Matches(navigation, "href=\"#([^\"]+)\""))
            positions.Add(Regex.Match(html, $"\\bid=\"{link.Groups[1].Value}\"").Index);
        Assert.Equal(3, positions.Count);
        Assert.Equal(positions.Order(), positions);
        Assert.True(html.IndexOf("id=\"bang-thang\"", StringComparison.Ordinal) <
            html.IndexOf("id=\"nguoi-phu-thuoc\"", StringComparison.Ordinal));
        foreach (var section in new[] { "du-lieu", "nguoi-phu-thuoc", "tong-quan" })
        {
            var href = Regex.Match(navigation, $"href=\"([^\"]*#{section})\"").Groups[1].Value;
            Assert.NotEmpty(href);
            var destination = new Uri(document, href);
            Assert.Equal(document.PathAndQuery, destination.PathAndQuery);
            Assert.Contains($"id=\"{section}\"", html);
        }
    }

    [Fact]
    public async Task List_and_editor_are_separate_and_completeness_distinguishes_zero_from_missing()
    {
        await using var host = await DemoHost.StartAsync();
        using var client = new HttpClient(new HttpClientHandler { UseCookies = true }) { BaseAddress = host.Address };
        var empty = await client.GetStringAsync("/ho-so");
        Assert.Contains("Chưa có hồ sơ đã lưu", empty);
        Assert.DoesNotContain("<form", empty);
        Assert.Contains("href=\"/Forecast\"", empty);

        var ids = new List<string>();
        foreach (var complete in new[] { false, true })
        {
            var editor = await client.GetStringAsync("/Forecast");
            Assert.DoesNotContain("<table class=\"profiles-table\"", editor);
            var fields = new Dictionary<string, string> {
                ["__RequestVerificationToken"] = Regex.Match(editor, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value,
                ["FromMonth"] = "1", ["ToMonth"] = "12", ["Gross"] = "0"
            };
            for (var i = 0; i < 12; i++)
            {
                fields[$"Exceptions[{i}].Month"] = (i + 1).ToString();
                fields[$"Exceptions[{i}].Insurance"] = complete ? "0" : "";
                fields[$"Exceptions[{i}].Dependents"] = "0";
                fields[$"Exceptions[{i}].Withheld"] = "0";
            }
            using var response = await client.PostAsync("/Forecast?handler=Save", new FormUrlEncodedContent(fields));
            response.EnsureSuccessStatusCode();
            var reopened = await response.Content.ReadAsStringAsync();
            ids.Add(Regex.Match(reopened, "id=\"ProfileId\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
            Assert.NotEmpty(ids[^1]);
        }
        var list = WebUtility.HtmlDecode(await client.GetStringAsync("/ho-so"));
        var text = Regex.Replace(list, "<[^>]+>", " ");
        Assert.DoesNotContain("<form", list);
        Assert.Contains("Thiếu dữ liệu", text);
        Assert.Contains("Đủ dữ liệu", text);
        Assert.Contains("1 phiên bản", text);
        Assert.DoesNotContain("Dữ liệu cần bổ sung", text);
        foreach (var id in ids)
        {
            Assert.DoesNotContain(id, text);
            var row = Regex.Match(list, $"<tr>(?:(?!</tr>)[\\s\\S])*{id}(?:(?!</tr>)[\\s\\S])*</tr>").Value;
            Assert.Contains(id == ids[1] ? "Đủ dữ liệu" : "Thiếu dữ liệu", row);
        }
        Assert.Single(Regex.Matches(list, "class=\"button primary\""));
    }
}
