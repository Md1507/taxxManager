using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using TaxManager.Data.Forecasting;
using TaxManager.Web.Demo;
using Xunit;

namespace TaxManager.Web.Tests.Forecasting;

public sealed class GroupedMoneyHttpTests
{
    [Theory]
    [InlineData("123.456.789,50", "123456789.50", true)]
    [InlineData("1.234", "1234", true)]
    [InlineData("1,234", "1.234", true)]
    [InlineData("1.234", "1.234", false)]
    public async Task Display_format_is_explicit_and_saved_decimal_keeps_its_exact_value(string input, string expected, bool grouped)
    {
        await using var host = await DemoHost.StartAsync();
        using var client = new HttpClient(new HttpClientHandler { UseCookies = true }) { BaseAddress = host.Address };
        var page = await client.GetStringAsync("/Forecast");
        var fields = Fields(page, input);
        if (grouped) fields["AmountFormat"] = "vi-VN";
        using var response = await client.PostAsync("/Forecast?handler=Save", new FormUrlEncodedContent(fields));
        response.EnsureSuccessStatusCode();
        await using var context = host.Services.GetRequiredService<SyntheticDatabase>().Open();
        var saved = Assert.Single(await new ForecastStore(context).ListAsync());
        using var payload = JsonDocument.Parse(saved.Payload!);
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), payload.RootElement.GetProperty("Gross").GetDecimal());
        Assert.Equal(JsonValueKind.Null, payload.RootElement.GetProperty("Exceptions")[0].GetProperty("Insurance").ValueKind);
    }

    [Theory]
    [InlineData("1.23.456")]
    [InlineData("1,234,567")]
    [InlineData("1.234.567.89")]
    public async Task Invalid_grouping_preserves_attempted_text_and_does_not_save(string amount)
    {
        await using var host = await DemoHost.StartAsync();
        using var client = new HttpClient(new HttpClientHandler { UseCookies = true }) { BaseAddress = host.Address };
        var fields = Fields(await client.GetStringAsync("/Forecast"), amount);
        fields["AmountFormat"] = "vi-VN";
        using var response = await client.PostAsync("/Forecast?handler=Save", new FormUrlEncodedContent(fields));
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains($"value=\"{amount}\"", html);
        Assert.Contains("Số tiền chưa đúng định dạng", html);
        // A screen reader receives both the existing salary hint and the server error,
        // even before the browser runs any JavaScript.
        var gross = Regex.Match(html, "<input\\b(?=[^>]*\\bid=\"Gross\")[^>]*>").Value;
        Assert.Contains("aria-invalid=\"true\"", gross);
        var descriptions = Regex.Match(gross, "aria-describedby=\"([^\"]+)\"").Groups[1].Value.Split(' ');
        Assert.Contains("salary-terms", descriptions);
        Assert.Contains("error-Gross", descriptions);
        Assert.Contains("money-Gross-hint", descriptions);
        Assert.Contains("id=\"error-Gross\"", html);
        Assert.Contains("href=\"#Gross\"", html);
        await using var context = host.Services.GetRequiredService<SyntheticDatabase>().Open();
        Assert.Empty(await new ForecastStore(context).ListAsync());
    }

    [Fact]
    public async Task Monthly_tool_accepts_the_same_grouped_format()
    {
        await using var host = await DemoHost.StartAsync();
        using var client = new HttpClient(new HttpClientHandler { UseCookies = true }) { BaseAddress = host.Address };
        var page = await client.GetStringAsync("/");
        var fields = new List<KeyValuePair<string, string>> {
            new("__RequestVerificationToken", Regex.Match(page, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value),
            new("year", "2025"), new("AmountFormat", "vi-VN") };
        for (var i = 0; i < 12; i++) { fields.Add(new("gross", "30.000.000,25")); fields.Add(new("insurance", "0")); fields.Add(new("withheld", "0")); }
        using var response = await client.PostAsync("/", new FormUrlEncodedContent(fields));
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains("<h2>Kết quả 12 tháng</h2>", html);
        Assert.Contains("2.150.000,05", html);
    }

    private static Dictionary<string, string> Fields(string page, string gross)
    {
        var fields = new Dictionary<string, string> {
            ["__RequestVerificationToken"] = Regex.Match(page, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value,
            ["Gross"] = gross };
        for (var i = 0; i < 12; i++) fields[$"Exceptions[{i}].Month"] = (i + 1).ToString();
        return fields;
    }
}
