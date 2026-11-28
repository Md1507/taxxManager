using System.Net;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaxManager.Core.Forecasting;
using TaxManager.Data.Audit;
using TaxManager.Data.Forecasting;
using TaxManager.Web.Demo;
using Xunit;

namespace TaxManager.Web.Tests.Forecasting;

public sealed class ForecastScenarioHttpTests
{
    [Fact]
    public async Task Compare_uses_saved_annual_inputs_keeps_existing_bonuses_and_never_writes_assumptions()
    {
        await using var host = await DemoHost.StartAsync();
        using var client = new HttpClient { BaseAddress = host.Address };
        var input = Input();
        var id = await Save(host, input);
        int audit;
        await using (var context = host.Services.GetRequiredService<SyntheticDatabase>().Open())
        {
            await new AnnualForecastService(context).CalculateAsync(id, 1);
            audit = await context.Set<AuditEvent>().CountAsync();
        }
        var path = $"/ho-so/{id}/kich-ban";
        using var opened = await client.GetAsync(path);
        opened.EnsureSuccessStatusCode();
        Assert.True(opened.Headers.CacheControl!.NoStore);
        var html = await opened.Content.ReadAsStringAsync();
        var fields = Fields(html);
        fields["BonusMonth"] = "12";
        fields["BonusAmount"] = "10.000.000";
        fields["AmountFormat"] = "vi-VN";
        using var compared = await client.PostAsync(path, new FormUrlEncodedContent(fields));
        compared.EnsureSuccessStatusCode();
        Assert.True(compared.Headers.CacheControl!.NoStore);
        html = WebUtility.HtmlDecode(await compared.Content.ReadAsStringAsync());
        // 360m gross + 10m saved bonus; 5m already inside gross stays counted once.
        // Annual taxable 184m/194m: 120m*5% + 64m/74m*10%.
        Assert.Equal("12.400.000,00 đ", Text(html, "base-obligation"));
        Assert.Equal("13.400.000,00 đ", Text(html, "bonus-obligation"));
        Assert.Equal("1.000.000,00 đ", Text(html, "scenario-tax-change"));
        Assert.Contains("10/12", Text(html, "bonus-coverage"));
        Assert.Contains("11/12", Text(html, "base-coverage"));
        Assert.Contains("Giả định", html);
        Assert.Contains("Unverified", html);
        Assert.Single(Regex.Matches(html, "class=\"primary\""));
        await using var check = host.Services.GetRequiredService<SyntheticDatabase>().Open();
        Assert.Equal(ForecastDraftReader.Encode(input), (await new ForecastStore(check).ReadAsync(id))!.Payload);
        Assert.Single(await new ForecastStore(check).HistoryAsync(id));
        Assert.Single(await new AnnualForecastService(check).HistoryAsync(id));
        Assert.Equal(audit, await check.Set<AuditEvent>().CountAsync());
    }

    [Fact]
    public async Task Initial_form_has_no_invented_bonus_and_explicit_zero_keeps_equal_results()
    {
        await using var host = await DemoHost.StartAsync();
        using var client = new HttpClient { BaseAddress = host.Address };
        var id = await Save(host, Input());
        var path = $"/ho-so/{id}/kich-ban";
        var html = await client.GetStringAsync(path);
        Assert.Contains("Chưa nhập khoản thưởng muốn thử", WebUtility.HtmlDecode(html));
        Assert.DoesNotContain("id=\"bonus-obligation\"", html);
        Assert.DoesNotContain("class=\"error-summary\"", html);
        foreach (var name in new[] { "BonusAmount", "BonusMonth" })
            Assert.Equal("", FieldValue(html, name));
        var fields = Fields(html);
        fields["BonusAmount"] = "0";
        fields["BonusMonth"] = "12";
        using var response = await client.PostAsync(path, new FormUrlEncodedContent(fields));
        response.EnsureSuccessStatusCode();
        html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Equal(Text(html, "base-obligation"), Text(html, "bonus-obligation"));
        Assert.Equal("0,00 đ", Text(html, "scenario-tax-change"));
        Assert.Contains("Nghĩa vụ thuế không đổi", html);
        Assert.Equal("0", FieldValue(html, "BonusAmount"));
    }

    [Fact]
    public async Task Invalid_or_missing_fields_keep_attempted_text_and_do_not_display_a_second_result()
    {
        await using var host = await DemoHost.StartAsync();
        using var client = new HttpClient { BaseAddress = host.Address };
        var id = await Save(host, Input());
        var path = $"/ho-so/{id}/kich-ban";
        var fields = Fields(await client.GetStringAsync(path));
        foreach (var (name, value) in new[] { ("BonusAmount", ""), ("BonusAmount", "1.23.456"),
            ("BonusAmount", "-10"), ("BonusAmount", "1,2,3"), ("BonusAmount", "9999999999999999999999999999999999"),
            ("BonusMonth", ""), ("BonusMonth", "0"), ("BonusMonth", "13"), ("BonusMonth", "abc") })
        {
            fields["BonusAmount"] = "10.000.000";
            fields["BonusMonth"] = "12";
            fields["AmountFormat"] = "vi-VN";
            fields[name] = value;
            using var response = await client.PostAsync(path, new FormUrlEncodedContent(fields));
            response.EnsureSuccessStatusCode();
            var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
            Assert.Contains("class=\"error-summary\"", html);
            Assert.Contains($"href=\"#{name}\"", html);
            Assert.Contains($"id=\"error-{name}\"", html);
            Assert.Contains("aria-invalid=\"true\"", html);
            Assert.Equal(value, FieldValue(html, name));
            Assert.DoesNotContain("id=\"bonus-obligation\"", html);
        }
        await using var check = host.Services.GetRequiredService<SyntheticDatabase>().Open();
        Assert.Single(await new ForecastStore(check).HistoryAsync(id));
        Assert.Empty(await new AnnualForecastService(check).HistoryAsync(id));
    }

    [Fact]
    public async Task Missing_obligation_is_specific_but_missing_withholding_still_allows_tax_comparison()
    {
        await using var host = await DemoHost.StartAsync();
        using var client = new HttpClient { BaseAddress = host.Address };
        var input = Input();
        input = input with { Months = input.Months.Select(m => m with { Withheld = null }).ToArray() };
        var id = await Save(host, input);
        var path = $"/ho-so/{id}/kich-ban";
        var fields = Fields(await client.GetStringAsync(path));
        fields["BonusAmount"] = "10000000";
        fields["BonusMonth"] = "12";
        using (var compared = await client.PostAsync(path, new FormUrlEncodedContent(fields)))
        {
            compared.EnsureSuccessStatusCode();
            var html = WebUtility.HtmlDecode(await compared.Content.ReadAsStringAsync());
            Assert.Equal("13.400.000,00 đ", Text(html, "bonus-obligation"));
            Assert.Contains("0/12 tháng đã nhập", html);
            Assert.DoesNotContain("Nộp thêm ước tính:", html);
            Assert.DoesNotContain("Nộp thừa ước tính:", html);
        }
        input.Months[3] = input.Months[3] with { Insurance = null };
        await using (var context = host.Services.GetRequiredService<SyntheticDatabase>().Open())
            await new ForecastStore(context).UpdateAsync(id, 1, ForecastDraftReader.Encode(input));
        var incomplete = WebUtility.HtmlDecode(await client.GetStringAsync(path));
        Assert.Contains("Bảo hiểm: tháng 4", incomplete);
        Assert.DoesNotContain("id=\"base-obligation\"", incomplete);
        Assert.Contains($"/Forecast?id={id}&month=4#sua-thang", incomplete);
    }

    [Fact]
    public async Task Antiforgery_revision_and_deletion_guards_refuse_stale_comparisons()
    {
        await using var host = await DemoHost.StartAsync();
        using var client = new HttpClient { BaseAddress = host.Address };
        var id = await Save(host, Input());
        var path = $"/ho-so/{id}/kich-ban";
        var fields = Fields(await client.GetStringAsync(path));
        fields["BonusAmount"] = "10000000";
        fields["BonusMonth"] = "12";
        using (var csrf = await client.PostAsync(path, new FormUrlEncodedContent(fields.Where(f => f.Key != "__RequestVerificationToken"))))
            Assert.Equal(HttpStatusCode.BadRequest, csrf.StatusCode);
        foreach (var query in new[] { "revision=0", "revision=bad" })
        {
            using var invalid = await client.GetAsync(path + "?" + query);
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        }
        await using (var context = host.Services.GetRequiredService<SyntheticDatabase>().Open())
            await new ForecastStore(context).UpdateAsync(id, 1, ForecastDraftReader.Encode(Input()));
        using (var stale = await client.PostAsync(path, new FormUrlEncodedContent(fields)))
        {
            Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
            Assert.DoesNotContain("id=\"bonus-obligation\"", await stale.Content.ReadAsStringAsync());
        }
        await using (var context = host.Services.GetRequiredService<SyntheticDatabase>().Open())
            await new ForecastStore(context).DeleteAsync(id, 2);
        using var deleted = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.NotFound, deleted.StatusCode);
        using var absent = await client.GetAsync($"/ho-so/{Guid.NewGuid()}/kich-ban");
        Assert.Equal(HttpStatusCode.NotFound, absent.StatusCode);
    }

    [Theory]
    [InlineData("DELETE FROM AnnualForecastRules")]
    [InlineData("DELETE FROM DataProtectionMetadata")]
    [InlineData("UPDATE ForecastDocuments SET PayloadEncrypted = randomblob(64)")]
    public async Task Unreadable_input_or_catalog_fails_closed_without_fallback(string corruptSql)
    {
        await using var host = await DemoHost.StartAsync();
        using var client = new HttpClient { BaseAddress = host.Address };
        var id = await Save(host, Input());
        await using (var context = host.Services.GetRequiredService<SyntheticDatabase>().Open())
            await context.Database.ExecuteSqlRawAsync(corruptSql);
        using var response = await client.GetAsync($"/ho-so/{id}/kich-ban");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.True(response.Headers.CacheControl!.NoStore);
        Assert.DoesNotContain("id=\"base-obligation\"", await response.Content.ReadAsStringAsync());
        if (corruptSql.Contains("DataProtectionMetadata"))
        {
            using var emptyLookup = await client.GetAsync($"/ho-so/{Guid.NewGuid()}/kich-ban");
            Assert.Equal(HttpStatusCode.ServiceUnavailable, emptyLookup.StatusCode);
        }
    }

    private static string FieldValue(string html, string name) => WebUtility.HtmlDecode(
        Regex.Match(Regex.Match(html, $"<input\\b[^>]*name=\"{name}\"[^>]*>").Value, "value=\"([^\"]*)\"").Groups[1].Value);

    private static readonly DateTimeOffset Entered = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static ForecastAnnualInput Input() => new(2026,
        Enumerable.Range(1, 12).Select(m => new ForecastMonth(m,
            new(30_000_000m, ForecastStatus.Actual, "Phiếu lương", null, Entered),
            new(0m, ForecastStatus.Actual, "Bảo hiểm", null, Entered),
            new(0, ForecastStatus.Actual, "Tự khai", null, Entered),
            new(0m, ForecastStatus.Actual, "Khấu trừ", null, Entered))).ToArray(),
        [new("included", 6, new(5_000_000m, ForecastStatus.Actual, "Phiếu thưởng", null, Entered), true),
         new("planned", 11, new(10_000_000m, ForecastStatus.Planned, "Kế hoạch", null, Entered), false)]);

    private static async Task<Guid> Save(DemoHost host, ForecastAnnualInput input)
    {
        await using var context = host.Services.GetRequiredService<SyntheticDatabase>().Open();
        return await new ForecastStore(context).CreateAsync(ForecastDraftReader.Encode(input));
    }

    private static Dictionary<string, string> Fields(string html) => Regex.Matches(html, "<input\\b[^>]*>")
        .Select(m => m.Value).Where(s => Regex.IsMatch(s, "type=\"hidden\""))
        .ToDictionary(s => WebUtility.HtmlDecode(Regex.Match(s, "name=\"([^\"]*)\"").Groups[1].Value),
            s => WebUtility.HtmlDecode(Regex.Match(s, "value=\"([^\"]*)\"").Groups[1].Value));

    private static string Text(string html, string id) =>
        Regex.Replace(Regex.Match(html, $"<[^>]*id=\"{id}\"[^>]*>(.*?)</[^>]+>", RegexOptions.Singleline).Groups[1].Value, "<[^>]+>", "").Trim();
}
