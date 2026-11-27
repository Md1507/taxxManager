using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaxManager.Data.Forecasting;
using TaxManager.Web.Demo;
using Xunit;

namespace TaxManager.Web.Tests.Forecasting;

public sealed class ForecastMonthHttpTests
{
    [Fact]
    public async Task Saved_profile_links_to_its_month_and_edit_save_reopens_the_right_month_without_changing_provenance()
    {
        await using var host = await DemoHost.StartAsync();
        using var client = Client(host);
        var savedPage = await Save(client);
        var fields = Fields(savedPage);
        var id = Guid.Parse(fields["ProfileId"]);
        var detailUrl = $"/ho-so/{id}/thang/6?revision=1";
        Assert.Contains(detailUrl, WebUtility.HtmlDecode(savedPage));
        using var detail = await client.GetAsync(detailUrl);
        detail.EnsureSuccessStatusCode();
        Assert.True(detail.Headers.CacheControl!.NoStore);
        var html = WebUtility.HtmlDecode(await detail.Content.ReadAsStringAsync());
        Assert.Contains("Tháng 6/2026", html);
        Assert.Contains("415.000,00 đ", html); // (30m + 3m - 3m - 15.5m - 6.2m) * 5%.
        Assert.Contains("đã gồm trong gross, không cộng trùng", html);
        Assert.Contains("Chưa nhập khấu trừ tháng này", html);
        Assert.Contains("kết quả này chưa lưu vào lịch sử", html);
        Assert.Contains("Phiếu lương tổng hợp", html);
        Assert.Contains("Ghi chú\nhai dòng", html);
        Assert.Contains("Unverified", html);
        Assert.Contains($"/Forecast?id={id}&month=6#sua-thang", html);
        var editPage = await client.GetStringAsync($"/Forecast?id={id}&month=6");
        Assert.Matches("<input[^>]*type=\"text\"[^>]*name=\"Exceptions\\[5\\].Insurance\"", editPage);
        await using (var context = host.Services.GetRequiredService<SyntheticDatabase>().Open())
        {
            using var before = JsonDocument.Parse((await new ForecastStore(context).ReadAsync(id))!.Payload!);
            var stamps = before.RootElement.GetProperty("Stamps").GetString()!;
            var edit = Fields(editPage);
            edit["Exceptions[5].Insurance"] = "4000000";
            using var response = await client.PostAsync("/Forecast?handler=Save", new FormUrlEncodedContent(edit));
            response.EnsureSuccessStatusCode();
            Assert.Contains("month=6", response.RequestMessage!.RequestUri!.Query);
            using var after = JsonDocument.Parse((await new ForecastStore(context).ReadAsync(id))!.Payload!);
            using var oldStamps = JsonDocument.Parse(stamps);
            using var newStamps = JsonDocument.Parse(after.RootElement.GetProperty("Stamps").GetString()!);
            foreach (var key in new[] { "base", "dependent-period/0", "bonus/0", "bonus/1" })
                Assert.Equal(oldStamps.RootElement.GetProperty(key).GetRawText(), newStamps.RootElement.GetProperty(key).GetRawText());
            Assert.Equal(before.RootElement.GetProperty("Exceptions")[0].GetRawText(), after.RootElement.GetProperty("Exceptions")[0].GetRawText());
            Assert.Empty(await context.UserProfiles.ToListAsync());
            Assert.Empty(await context.MonthlyIncomes.ToListAsync());
            Assert.Empty(await context.CalculationHistory.ToListAsync());
            Assert.Empty(await new AnnualForecastService(context).HistoryAsync(id));
        }
        using var stale = await client.GetAsync(detailUrl);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        var staleHtml = WebUtility.HtmlDecode(await stale.Content.ReadAsStringAsync());
        Assert.Contains("Hồ sơ đã thay đổi", staleHtml);
        Assert.DoesNotContain("415.000", staleHtml);
        var fresh = WebUtility.HtmlDecode(await client.GetStringAsync($"/ho-so/{id}/thang/6?revision=2"));
        Assert.Contains("365.000,00 đ", fresh);
    }

    [Fact]
    public async Task Reopening_the_same_synthetic_store_in_a_new_host_keeps_the_exception_and_month_sources()
    {
        string root;
        string id;
        await using (var firstHost = await DemoHost.StartAsync())
        {
            using var client = Client(firstHost);
            var fields = Fields(await Save(client));
            id = fields["ProfileId"];
            fields["Exceptions[5].Gross"] = "31000000";
            fields["Exceptions[5].Status"] = "Actual";
            fields["Exceptions[5].Source"] = "Lương ngoại lệ synthetic";
            using var saved = await client.PostAsync("/Forecast?handler=Save", new FormUrlEncodedContent(fields));
            saved.EnsureSuccessStatusCode();
            root = firstHost.Services.GetRequiredService<SyntheticDatabase>().RootPath;
        }
        await using var reopenedHost = await DemoHost.StartAsync(syntheticRoot: root);
        using var reopenedClient = Client(reopenedHost);
        var html = WebUtility.HtmlDecode(await reopenedClient.GetStringAsync($"/ho-so/{id}/thang/6?revision=2"));
        Assert.Contains("465.000,00 đ", html);
        Assert.Contains("Lương ngoại lệ synthetic", html);
        Assert.Contains("Ghi chú\nhai dòng", html);
        Assert.Contains("đã gồm trong gross, không cộng trùng", html);
        Assert.Contains($"/Forecast?id={id}&month=6#sua-thang", html);
    }

    [Fact]
    public async Task Missing_month_displays_specific_fields_and_a_saved_zero_can_be_calculated()
    {
        await using var host = await DemoHost.StartAsync();
        using var client = Client(host);
        var fields = Fields(await Save(client));
        var id = fields["ProfileId"];
        var html = WebUtility.HtmlDecode(await client.GetStringAsync($"/ho-so/{id}/thang/1?revision=1"));
        Assert.Contains("Chưa đủ dữ liệu tính thuế tháng", html);
        Assert.Contains("<li>Bảo hiểm bắt buộc</li>", html);
        Assert.DoesNotContain("class=\"month-tax\"", html);
        Assert.DoesNotContain("<li>Số người phụ thuộc</li>", html); // Inherited from the saved dependent period.
        fields["Exceptions[0].Gross"] = "0";
        fields["Exceptions[0].Insurance"] = "0";
        fields["Exceptions[0].Dependents"] = "0";
        fields["Exceptions[0].Withheld"] = "0";
        using var saved = await client.PostAsync("/Forecast?handler=Save", new FormUrlEncodedContent(fields));
        saved.EnsureSuccessStatusCode();
        html = WebUtility.HtmlDecode(await client.GetStringAsync($"/ho-so/{id}/thang/1?revision=2"));
        Assert.Contains("class=\"month-tax\">0,00 đ", html);
        Assert.Contains("Chênh lệch thuế tạm tính − khấu trừ đã nhập: 0,00 đ", html);
        Assert.DoesNotContain("Chưa đủ dữ liệu tính thuế tháng", html);
    }

    [Fact]
    public async Task Deleted_profile_or_missing_catalog_never_shows_a_calculated_value()
    {
        await using var host = await DemoHost.StartAsync();
        using var client = Client(host);
        var id = Guid.Parse(Fields(await Save(client))["ProfileId"]);
        using var missing = await client.GetAsync($"/ho-so/{Guid.NewGuid()}/thang/6?revision=1");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        await using (var context = host.Services.GetRequiredService<SyntheticDatabase>().Open())
            await context.Database.ExecuteSqlRawAsync("DELETE FROM AnnualForecastRules WHERE PeriodFrom = '2026-01-01'");
        using var unavailable = await client.GetAsync($"/ho-so/{id}/thang/6?revision=1");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, unavailable.StatusCode);
        Assert.DoesNotContain("class=\"month-tax\"", await unavailable.Content.ReadAsStringAsync());
        await using (var context = host.Services.GetRequiredService<SyntheticDatabase>().Open()) await new ForecastStore(context).DeleteAsync(id, 1);
        using var deleted = await client.GetAsync($"/ho-so/{id}/thang/6?revision=1");
        Assert.Equal(HttpStatusCode.NotFound, deleted.StatusCode);
    }

    [Theory]
    [InlineData(0, "1")]
    [InlineData(13, "1")]
    [InlineData(6, "0")]
    [InlineData(6, "abc")]
    public async Task Invalid_month_or_revision_has_a_recovery_link(int month, string revision)
    {
        await using var host = await DemoHost.StartAsync();
        using var client = Client(host);
        using var response = await client.GetAsync($"/ho-so/{Guid.NewGuid()}/thang/{month}?revision={revision}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Trở lại danh sách hồ sơ", WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()));
    }

    private static HttpClient Client(DemoHost host) => new(new HttpClientHandler { UseCookies = true }) { BaseAddress = host.Address };
    private static async Task<string> Save(HttpClient client)
    {
        var fields = Fields(await client.GetStringAsync("/Forecast"));
        fields["Gross"] = "30000000";
        fields["Status"] = "Actual";
        fields["Source"] = "Phiếu lương tổng hợp";
        fields["DependentPeriods[0].Count"] = "1";
        fields["DependentPeriods[0].Status"] = "Actual";
        fields["Exceptions[5].Insurance"] = "3000000";
        fields["Exceptions[5].InsuranceStatus"] = "Actual";
        fields["Exceptions[5].InsuranceNote"] = "Ghi chú\nhai dòng";
        for (var i = 0; i < 2; i++)
        {
            fields[$"Bonuses[{i}].Month"] = "6";
            fields[$"Bonuses[{i}].Amount"] = i == 0 ? "2000000" : "3000000";
            fields[$"Bonuses[{i}].IncludedInGross"] = i == 0 ? "true" : "false";
            fields[$"Bonuses[{i}].Status"] = i == 0 ? "Actual" : "Planned";
        }
        using var response = await client.PostAsync("/Forecast?handler=Save", new FormUrlEncodedContent(fields));
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(Fields(html)["ProfileId"]);
        return html;
    }

    private static Dictionary<string, string> Fields(string html)
    {
        var fields = new Dictionary<string, string>();
        foreach (Match tag in Regex.Matches(html, @"<input\b[^>]*>|<textarea\b[^>]*>[\s\S]*?</textarea>|<select\b[^>]*>[\s\S]*?</select>"))
        {
            var name = Attr(tag.Value, "name");
            if (name.Length == 0 || Attr(tag.Value, "type") is "submit" or "button") continue;
            string value;
            if (tag.Value.StartsWith("<textarea")) value = WebUtility.HtmlDecode(tag.Value[(tag.Value.IndexOf('>') + 1)..tag.Value.LastIndexOf('<')]);
            else if (tag.Value.StartsWith("<select"))
            {
                var options = Regex.Matches(tag.Value, @"<option\b[^>]*>[\s\S]*?</option>").Select(m => m.Value).ToArray();
                value = Attr(options.FirstOrDefault(o => o.Split('>')[0].Contains("selected")) ?? options[0], "value");
            }
            else value = Attr(tag.Value, "value");
            fields.TryAdd(name, value);
        }
        return fields;
    }
    private static string Attr(string tag, string name) => WebUtility.HtmlDecode(Regex.Match(tag.Split('>')[0], "\\b" + name + "=\"([^\"]*)\"").Groups[1].Value);
}
