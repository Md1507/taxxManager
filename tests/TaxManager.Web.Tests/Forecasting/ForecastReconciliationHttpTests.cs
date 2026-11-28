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

public sealed class ForecastReconciliationHttpTests
{
    [Fact]
    public async Task Totals_use_only_the_selected_metric_and_period_with_signed_source_minus_profile_difference()
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
        var path = Path(id);
        using var opened = await client.GetAsync(path);
        Assert.True(opened.Headers.CacheControl!.NoStore);
        var initial = await opened.Content.ReadAsStringAsync();
        Assert.DoesNotContain("id=\"reconciliation-difference\"", initial);
        Assert.Equal("", Value(initial, "Amount"));
        var fields = Fields(initial);
        foreach (var (metric, from, to, amount, baseline, delta, direction) in new[] {
            ("Withheld", "1", "12", "18.500.000", "18.000.000 đ", "+500.000 đ", "Nguồn cao hơn hồ sơ"),
            ("Withheld", "6", "6", "1.000.000", "1.500.000 đ", "−500.000 đ", "Nguồn thấp hơn hồ sơ"),
            ("Insurance", "2", "3", "6.000.000", "6.000.000 đ", "0 đ", "Hai số bằng nhau"),
            // Stored gross includes a 5m bonus; a separate 10m bonus is outside this metric.
            ("Gross", "1", "12", "360.000.000", "360.000.000 đ", "0 đ", "Hai số bằng nhau") })
        {
            fields["Metric"] = metric; fields["FromMonth"] = from; fields["ToMonth"] = to; fields["Amount"] = amount;
            using var response = await client.PostAsync(path, new FormUrlEncodedContent(fields));
            response.EnsureSuccessStatusCode();
            Assert.True(response.Headers.CacheControl!.NoStore);
            var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
            Assert.Equal(baseline, Text(html, "profile-amount"));
            Assert.Equal(delta, Text(html, "reconciliation-difference"));
            Assert.Contains(direction, html);
            Assert.Contains("không kết luận công ty hoặc eTax sai", html);
            Assert.Single(Regex.Matches(html, "class=\"primary\""));
        }
        await using var check = host.Services.GetRequiredService<SyntheticDatabase>().Open();
        Assert.Equal(ForecastDraftReader.Encode(input), (await new ForecastStore(check).ReadAsync(id))!.Payload);
        Assert.Single(await new ForecastStore(check).HistoryAsync(id));
        Assert.Single(await new AnnualForecastService(check).HistoryAsync(id));
        Assert.Equal(audit, await check.Set<AuditEvent>().CountAsync());
    }

    [Fact]
    public async Task Missing_selected_month_blocks_total_but_explicit_zero_and_missing_unrelated_fields_do_not()
    {
        await using var host = await DemoHost.StartAsync();
        using var client = new HttpClient { BaseAddress = host.Address };
        var input = Input();
        input.Months[7] = input.Months[7] with { Withheld = null, Gross = null, Insurance = null };
        input.Months[8] = input.Months[8] with { Gross = null, Insurance = null, Withheld = new(0m, ForecastStatus.Actual, "Xác nhận 0", null, Entered) };
        var id = await Save(host, input);
        var fields = Fields(await client.GetStringAsync(Path(id)));
        fields["Amount"] = "0";
        var missing = await Post(client, id, fields);
        Assert.Equal("Chưa so được", Text(missing, "profile-amount"));
        Assert.DoesNotContain("id=\"reconciliation-difference\"", missing);
        Assert.Contains("trong hồ sơ: tháng 8", missing);
        Assert.Contains($"/Forecast?id={id}&month=8#sua-thang", missing);
        fields["FromMonth"] = fields["ToMonth"] = "9";
        var zero = await Post(client, id, fields);
        Assert.Equal("0 đ", Text(zero, "profile-amount"));
        Assert.Equal("0 đ", Text(zero, "reconciliation-difference"));
        // Month 8 can still reconcile a populated field without gross/insurance.
        fields["FromMonth"] = fields["ToMonth"] = "7";
        var valid = await Post(client, id, fields);
        Assert.Equal("1.500.000 đ", Text(valid, "profile-amount"));
    }

    [Fact]
    public async Task Mismatched_year_is_explicit_and_never_produces_a_comparable_total()
    {
        await using var host = await DemoHost.StartAsync();
        using var client = new HttpClient { BaseAddress = host.Address };
        var id = await Save(host, Input());
        var fields = Fields(await client.GetStringAsync(Path(id)));
        fields["SourceYear"] = "2025";
        var html = await Post(client, id, fields);
        Assert.Contains("Hai số đang khác kỳ: hồ sơ năm 2026, nguồn năm 2025", html);
        Assert.DoesNotContain("id=\"reconciliation-difference\"", html);
        Assert.Equal("Chưa so được", Text(html, "profile-amount"));
        Assert.Equal("2025", Value(html, "SourceYear"));
    }

    [Fact]
    public async Task Source_metadata_and_status_are_independent_encoded_and_do_not_replace_monthly_provenance()
    {
        await using var host = await DemoHost.StartAsync(clock: new FixedClock());
        using var client = new HttpClient { BaseAddress = host.Address };
        var input = Input();
        input.Months[1] = input.Months[1] with { Withheld = new(1_500_000m, ForecastStatus.Planned, "Dự toán gốc", "Ghi chú gốc", Entered) };
        var id = await Save(host, input);
        var fields = Fields(await client.GetStringAsync(Path(id)));
        fields["Source"] = "ETax";
        fields["Status"] = "Assumed";
        fields["SourceName"] = "<script>alert(1)</script>";
        fields["Note"] = "ước lượng\n dòng thứ hai";
        using var response = await client.PostAsync(Path(id), new FormUrlEncodedContent(fields));
        var raw = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("<script>alert(1)</script>", raw);
        var html = WebUtility.HtmlDecode(raw);
        Assert.Contains("Giả định", html);
        Assert.Contains("Dự kiến: 1/12", html);
        Assert.Contains("Thực tế: 11/12", html);
        Assert.Contains("Dự toán gốc", html);
        Assert.Contains("Ghi chú gốc", html);
        Assert.Contains("28/11/2026 10:00", html);
        Assert.Contains("ước lượng\n dòng thứ hai", html);
        var reopened = await client.GetStringAsync(Path(id));
        Assert.Equal("", Value(reopened, "Amount"));
        Assert.DoesNotContain("id=\"reconciliation-difference\"", reopened);
    }

    [Fact]
    public async Task Invalid_input_keeps_attempted_values_and_links_errors_to_fields_without_results()
    {
        await using var host = await DemoHost.StartAsync();
        using var client = new HttpClient { BaseAddress = host.Address };
        var id = await Save(host, Input());
        var initial = await client.GetStringAsync(Path(id));
        foreach (var (key, value) in new[] { ("Amount", ""), ("Amount", "-1"), ("Amount", "1.23.456"),
            ("Amount", "9999999999999999999999999999999999999"), ("Source", "999"), ("Status", "999"),
            ("Metric", "999"), ("Metric", ""), ("SourceName", " "), ("SourceName", new string('x', 201)),
            ("SourceYear", ""), ("FromMonth", "0"), ("ToMonth", "13"), ("ToMonth", "abc"),
            ("Note", new string('x', 2001)) })
        {
            var fields = Fields(initial); fields[key] = value;
            var html = await Post(client, id, fields);
            Assert.Contains("class=\"error-summary\"", html);
            Assert.Contains($"href=\"#{key}\"", html);
            Assert.Contains($"id=\"error-{key}\"", html);
            Assert.Contains("aria-invalid=\"true\"", html);
            Assert.DoesNotContain("id=\"reconciliation-difference\"", html);
            if (key is "Amount" or "SourceName" or "SourceYear" or "FromMonth" or "ToMonth")
                Assert.Equal(value, Value(html, key));
        }
        var reversed = Fields(initial); reversed["FromMonth"] = "10"; reversed["ToMonth"] = "2";
        Assert.Contains("Tháng cuối phải bằng hoặc sau tháng đầu", await Post(client, id, reversed));
        var assumed = Fields(initial); assumed["Status"] = "Assumed"; assumed["Note"] = "";
        Assert.Contains("Ghi cách hình thành số giả định", await Post(client, id, assumed));
    }

    [Fact]
    public async Task Csrf_missing_revision_stale_revision_and_deleted_profile_are_refused()
    {
        await using var host = await DemoHost.StartAsync();
        using var client = new HttpClient { BaseAddress = host.Address };
        var id = await Save(host, Input());
        var fields = Fields(await client.GetStringAsync(Path(id)));
        using (var csrf = await client.PostAsync(Path(id), new FormUrlEncodedContent(fields.Where(f => f.Key != "__RequestVerificationToken"))))
            Assert.Equal(HttpStatusCode.BadRequest, csrf.StatusCode);
        var missingRevision = new Dictionary<string, string>(fields); missingRevision.Remove("Revision");
        using (var response = await client.PostAsync(Path(id), new FormUrlEncodedContent(missingRevision)))
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using (var context = host.Services.GetRequiredService<SyntheticDatabase>().Open())
            await new ForecastStore(context).UpdateAsync(id, 1, ForecastDraftReader.Encode(Input()));
        using (var stale = await client.PostAsync(Path(id), new FormUrlEncodedContent(fields)))
            Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        await using (var context = host.Services.GetRequiredService<SyntheticDatabase>().Open())
            await new ForecastStore(context).DeleteAsync(id, 2);
        using var deleted = await client.GetAsync(Path(id));
        Assert.Equal(HttpStatusCode.NotFound, deleted.StatusCode);
    }

    [Theory]
    [InlineData("DELETE FROM DataProtectionMetadata")]
    [InlineData("UPDATE ForecastDocuments SET PayloadEncrypted = randomblob(64)")]
    public async Task Unreadable_profile_fails_closed_even_for_empty_lookup_when_sentinel_is_missing(string sql)
    {
        await using var host = await DemoHost.StartAsync();
        using var client = new HttpClient { BaseAddress = host.Address };
        var id = await Save(host, Input());
        await using (var context = host.Services.GetRequiredService<SyntheticDatabase>().Open())
            await context.Database.ExecuteSqlRawAsync(sql);
        using var response = await client.GetAsync(Path(id));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.True(response.Headers.CacheControl!.NoStore);
        if (sql.Contains("Metadata"))
        {
            using var absent = await client.GetAsync(Path(Guid.NewGuid()));
            Assert.Equal(HttpStatusCode.ServiceUnavailable, absent.StatusCode);
        }
    }

    private sealed class FixedClock : TimeProvider { public override DateTimeOffset GetUtcNow() => new(2026, 11, 28, 3, 0, 0, TimeSpan.Zero); }
    private static readonly DateTimeOffset Entered = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static ForecastAnnualInput Input() => new(2026, Enumerable.Range(1, 12).Select(m => new ForecastMonth(m,
        new(30_000_000m, ForecastStatus.Actual, "Phiếu lương", null, Entered),
        new(3_000_000m, ForecastStatus.Actual, "Bảo hiểm", null, Entered),
        new(0, ForecastStatus.Actual, "Tự khai", null, Entered),
        new(1_500_000m, ForecastStatus.Actual, "Khấu trừ gốc", null, Entered))).ToArray(),
        [new("included", 6, new(5_000_000m, ForecastStatus.Actual, "Thưởng", null, Entered), true),
         new("outside", 11, new(10_000_000m, ForecastStatus.Planned, "Thưởng dự kiến", null, Entered), false)]);
    private static string Path(Guid id) => $"/ho-so/{id}/doi-chieu";
    private static async Task<Guid> Save(DemoHost host, ForecastAnnualInput input)
    {
        await using var context = host.Services.GetRequiredService<SyntheticDatabase>().Open();
        return await new ForecastStore(context).CreateAsync(ForecastDraftReader.Encode(input));
    }
    private static Dictionary<string, string> Fields(string html)
    {
        // Razor emits repeated __Invariant fields for numeric inputs. This probe only sends
        // plain integer period values; money has its own explicit format marker.
        var fields = Regex.Matches(html, "<input\\b[^>]*>").Select(m => m.Value)
            .Where(s => s.Contains("type=\"hidden\"") && !s.Contains("name=\"__Invariant\""))
            .ToDictionary(s => WebUtility.HtmlDecode(Regex.Match(s, "name=\"([^\"]*)\"").Groups[1].Value),
                s => WebUtility.HtmlDecode(Regex.Match(s, "value=\"([^\"]*)\"").Groups[1].Value));
        foreach (var (key, value) in new[] { ("Source", "Company"), ("SourceName", "Bảng tổng hợp công ty"),
            ("SourceYear", "2026"), ("FromMonth", "1"), ("ToMonth", "12"), ("Metric", "Withheld"),
            ("Amount", "18.000.000"), ("AmountFormat", "vi-VN"), ("Status", "Actual"), ("Note", "") }) fields[key] = value;
        return fields;
    }
    private static async Task<string> Post(HttpClient client, Guid id, Dictionary<string, string> fields)
    {
        using var response = await client.PostAsync(Path(id), new FormUrlEncodedContent(fields));
        response.EnsureSuccessStatusCode();
        return WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
    }
    private static string Value(string html, string name) => WebUtility.HtmlDecode(Regex.Match(
        Regex.Match(html, $"<input\\b[^>]*name=\"{name}\"[^>]*>").Value, "value=\"([^\"]*)\"").Groups[1].Value);
    private static string Text(string html, string id) => Regex.Replace(Regex.Match(html,
        $"<[^>]*id=\"{id}\"[^>]*>(.*?)</[^>]+>", RegexOptions.Singleline).Groups[1].Value, "<[^>]+>", "").Trim();
}
