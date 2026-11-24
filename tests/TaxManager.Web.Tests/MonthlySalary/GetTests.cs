using System.Net;
using System.Text.RegularExpressions;
using Xunit;

namespace TaxManager.Web.Tests.MonthlySalary;

public class GetTests
{
    [Fact]
    public async Task Local_demo_form_shows_all_twelve_months()
    {
        await using var host = await DemoHost.StartAsync();
        using var client = new HttpClient { BaseAddress = host.Address };
        var response = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Tháng 12", html);
        Assert.Contains("Dữ liệu giả", html);
    }

    [Theory]
    [InlineData("2025", "2.150.000")]
    [InlineData("2026", "950.000")]
    public async Task Twelve_months_are_calculated_and_shown_with_reference_basis(string year, string tax)
    {
        await using var host = await DemoHost.StartAsync();
        using var client = new HttpClient(new HttpClientHandler { UseCookies = true }) { BaseAddress = host.Address };
        var html = await client.GetStringAsync("/");
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
        Assert.NotEmpty(token);
        var fields = new List<KeyValuePair<string, string>> { new("__RequestVerificationToken", token), new("year", year) };
        for (var month = 1; month <= 12; month++)
        {
            fields.Add(new("gross", "30000000"));
            fields.Add(new("insurance", "0"));
            fields.Add(new("withheld", "0"));
        }
        var response = await client.PostAsync("/", new FormUrlEncodedContent(fields));
        var result = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(tax, result);
        Assert.Contains("Tháng 12", result);
        Assert.Contains("Unverified", result);
    }

    [Fact]
    public async Task Unsupported_year_does_not_fall_back_to_a_reference_year()
    {
        await using var host = await DemoHost.StartAsync();
        using var client = new HttpClient(new HttpClientHandler { UseCookies = true }) { BaseAddress = host.Address };
        var html = await client.GetStringAsync("/");
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
        var fields = new List<KeyValuePair<string, string>> { new("__RequestVerificationToken", token), new("year", "2027") };
        for (var month = 1; month <= 12; month++)
        {
            fields.Add(new("gross", "30000000")); fields.Add(new("insurance", "0")); fields.Add(new("withheld", "0"));
        }
        var result = await (await client.PostAsync("/", new FormUrlEncodedContent(fields))).Content.ReadAsStringAsync();
        Assert.Contains("không được hỗ trợ", WebUtility.HtmlDecode(result));
        Assert.DoesNotContain("Kết quả 12 tháng</h2>", result);
    }

    [Fact]
    public async Task Sample_button_provides_synthetic_amounts()
    {
        await using var host = await DemoHost.StartAsync();
        using var client = new HttpClient { BaseAddress = host.Address };
        var html = await client.GetStringAsync("/?handler=Sample");
        Assert.Contains("value=\"30000000\"", html);
        Assert.DoesNotContain("Kết quả 12 tháng</h2>", html);
    }

    [Fact]
    public async Task Local_page_serves_its_stylesheet()
    {
        await using var host = await DemoHost.StartAsync();
        using var client = new HttpClient { BaseAddress = host.Address };
        using var response = await client.GetAsync("/site.css");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("--accent", await response.Content.ReadAsStringAsync());
    }
}
