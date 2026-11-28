using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualBasic.FileIO;
using TaxManager.Core.Forecasting;
using TaxManager.Data.Audit;
using TaxManager.Data.Forecasting;
using TaxManager.Web.Demo;
using Xunit;

namespace TaxManager.Web.Tests.Forecasting;

public sealed class ForecastReportHttpTests
{
    [Fact]
    public async Task Report_and_csv_keep_missing_zero_metadata_and_read_only_history()
    {
        await using var host = await DemoHost.StartAsync();
        using var client = new HttpClient { BaseAddress = host.Address };
        var input = Input(0, false);
        var formulas = new[] { "=1+1", "  +SUM(1,2)", "\t@SUM(1,2)", "-1+1" };
        input = input with { Months = input.Months.Select((m, i) => i < formulas.Length ? m with {
            Gross = new ForecastValue<decimal>(0, i == 2 ? ForecastStatus.Assumed : ForecastStatus.Actual, formulas[i], "Ghi chú, \"hai dòng\"\nDòng sau", Entered)
        } : m).ToArray() };
        input = input with { Bonuses = [new("bonus", 6, new(10, ForecastStatus.Planned, "Phiếu thưởng", "Đã nằm trong gross", Entered), true)] };
        var id = await Save(host, input);
        int audit;
        await using (var context = host.Services.GetRequiredService<SyntheticDatabase>().Open())
            audit = await context.Set<AuditEvent>().CountAsync();
        using var page = await client.GetAsync($"/ho-so/{id}/bao-cao");
        page.EnsureSuccessStatusCode();
        Assert.True(page.Headers.CacheControl!.NoStore);
        var html = WebUtility.HtmlDecode(await page.Content.ReadAsStringAsync());
        Assert.Contains("Checklist dữ liệu", html);
        Assert.Contains("Chưa nhập", html);
        Assert.Contains("0 đ", html);
        Assert.Single(Regex.Matches(html, "class=\"button primary\""));
        using var csv = await client.GetAsync(ExportLink(html));
        csv.EnsureSuccessStatusCode();
        Assert.True(csv.Headers.CacheControl!.NoStore);
        Assert.Equal("attachment", csv.Content.Headers.ContentDisposition!.DispositionType);
        Assert.Equal("text/csv", csv.Content.Headers.ContentType!.MediaType);
        var bytes = await csv.Content.ReadAsByteArrayAsync();
        Assert.Equal(new byte[] { 0xef, 0xbb, 0xbf }, bytes[..3]);
        var rows = Parse(bytes);
        foreach (var row in rows) Assert.Equal(11, row.Length);
        var zero = Assert.Single(rows, r => r[2] == "1" && r[3] == "Bảo hiểm");
        Assert.Equal("0", zero[4]);
        var missing = Assert.Single(rows, r => r[2] == "2" && r[3] == "Bảo hiểm");
        Assert.Equal("", missing[4]);
        Assert.Equal("Chưa nhập", missing[6]);
        for (var i = 0; i < formulas.Length; i++)
        {
            var gross = Assert.Single(rows, r => r[2] == (i + 1).ToString() && r[3] == "Lương gross");
            Assert.Equal("'" + formulas[i], gross[7]);
            Assert.Equal(i == 2 ? "Giả định" : "Thực tế", gross[6]);
            Assert.Equal("Ghi chú, \"hai dòng\"\nDòng sau", gross[8].Replace("\r\n", "\n"));
            Assert.Equal(Entered.ToString("O", CultureInfo.InvariantCulture), gross[9]);
        }
        Assert.Equal("Đã gồm gross", Assert.Single(rows, r => r[3] == "Thưởng")[10]);
        await using var check = host.Services.GetRequiredService<SyntheticDatabase>().Open();
        Assert.Equal(audit, await check.Set<AuditEvent>().CountAsync());
        Assert.Single(await new ForecastStore(check).HistoryAsync(id));
        Assert.Empty(await new AnnualForecastService(check).HistoryAsync(id));
        Assert.Equal(ForecastDraftReader.Encode(input), (await new ForecastStore(check).ReadAsync(id))!.Payload);
    }

    [Fact]
    public async Task Export_pins_revision_and_saved_calculation_including_no_calculation()
    {
        var clock = new ReportClock();
        await using var host = await DemoHost.StartAsync(clock: clock);
        using var client = new HttpClient { BaseAddress = host.Address };
        var id = await Save(host, Input(0, true));
        var noResultLink = ExportLink(await client.GetStringAsync($"/ho-so/{id}/bao-cao"));
        AnnualCalculationSnapshot first;
        await using (var context = host.Services.GetRequiredService<SyntheticDatabase>().Open())
            first = (await new AnnualForecastService(context).CalculateAsync(id, 1)).Snapshot!;
        var pinnedLink = ExportLink(await client.GetStringAsync($"/ho-so/{id}/bao-cao"));
        clock.Now = clock.Now.AddHours(1);
        await using (var context = host.Services.GetRequiredService<SyntheticDatabase>().Open())
            await new AnnualForecastService(context).CalculateAsync(id, 1);
        await using (var context = host.Services.GetRequiredService<SyntheticDatabase>().Open())
            await new ForecastStore(context).UpdateAsync(id, 1, ForecastDraftReader.Encode(Input(1, true)));

        var noResult = Parse(await client.GetByteArrayAsync(noResultLink));
        Assert.Contains(noResult, r => r[6] == "Chưa có kết quả năm đã lưu");
        Assert.DoesNotContain(noResult, r => r[3] == "Nghĩa vụ thuế năm ước tính");
        var pinned = Parse(await client.GetByteArrayAsync(pinnedLink));
        var obligation = Assert.Single(pinned, r => r[3] == "Nghĩa vụ thuế năm ước tính");
        Assert.Equal(0m, decimal.Parse(obligation[4], CultureInfo.InvariantCulture));
        Assert.Equal(first.CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture), obligation[9]);
        Assert.Equal("0", Assert.Single(pinned, r => r[2] == "1" && r[3] == "Lương gross")[4]);
        Assert.Contains(pinned, r => r[3].StartsWith("Luật ") && r[6] == "Unverified" && r[7].Length > 0);
        Assert.All(pinned.Skip(1), r => Assert.Equal("1", r[1]));
        var current = WebUtility.HtmlDecode(await client.GetStringAsync($"/ho-so/{id}/bao-cao"));
        Assert.Contains("Phiên bản 2", current);
        Assert.Contains("Chưa có kết quả năm đã lưu cho phiên bản này", current);
        using var wrongRevision = await client.GetAsync($"/ho-so/{id}/bao-cao?revision=2&record={first.HistoryId}");
        Assert.Equal(HttpStatusCode.NotFound, wrongRevision.StatusCode);
        await using var check = host.Services.GetRequiredService<SyntheticDatabase>().Open();
        Assert.Equal(2, (await new AnnualForecastService(check).HistoryAsync(id)).Count);
    }

    [Fact]
    public async Task Invalid_deleted_and_corrupt_reports_fail_without_download()
    {
        await using var host = await DemoHost.StartAsync();
        using var client = new HttpClient { BaseAddress = host.Address };
        var id = await Save(host, Input(0, false));
        foreach (var query in new[] { "revision=0", "revision=bad", "record=bad" })
        {
            using var bad = await client.GetAsync($"/ho-so/{id}/bao-cao?handler=Csv&{query}");
            Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
            Assert.Null(bad.Content.Headers.ContentDisposition);
        }
        using (var missing = await client.GetAsync($"/ho-so/{id}/bao-cao?revision=99"))
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        using (var missing = await client.GetAsync($"/ho-so/{Guid.NewGuid()}/bao-cao"))
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        await using (var context = host.Services.GetRequiredService<SyntheticDatabase>().Open())
            await context.Database.ExecuteSqlRawAsync("UPDATE ForecastDocuments SET PayloadEncrypted = randomblob(64) WHERE Id = {0}", id);
        using (var corrupt = await client.GetAsync($"/ho-so/{id}/bao-cao?handler=Csv"))
        {
            Assert.Equal(HttpStatusCode.ServiceUnavailable, corrupt.StatusCode);
            Assert.Null(corrupt.Content.Headers.ContentDisposition);
            Assert.DoesNotContain("Xuất CSV", WebUtility.HtmlDecode(await corrupt.Content.ReadAsStringAsync()));
        }
        var deletedId = await Save(host, Input(0, true));
        await using (var context = host.Services.GetRequiredService<SyntheticDatabase>().Open())
            await new ForecastStore(context).DeleteAsync(deletedId, 1);
        using var deleted = await client.GetAsync($"/ho-so/{deletedId}/bao-cao?handler=Csv&revision=1");
        Assert.Equal(HttpStatusCode.NotFound, deleted.StatusCode);
        await using (var context = host.Services.GetRequiredService<SyntheticDatabase>().Open())
            await context.Database.ExecuteSqlRawAsync("DELETE FROM DataProtectionMetadata");
        using var unprotectedEmptyRead = await client.GetAsync($"/ho-so/{Guid.NewGuid()}/bao-cao?handler=Csv");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, unprotectedEmptyRead.StatusCode);
        Assert.Null(unprotectedEmptyRead.Content.Headers.ContentDisposition);
    }

    private static readonly DateTimeOffset Entered = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static ForecastAnnualInput Input(decimal gross, bool complete) => new(2026,
        Enumerable.Range(1, 12).Select(m => new ForecastMonth(m,
            new(gross, ForecastStatus.Actual, "Phiếu lương", null, Entered),
            complete || m == 1 ? new(0, ForecastStatus.Actual, "Bảo hiểm", null, Entered) : null,
            new(0, ForecastStatus.Actual, "Tự khai", null, Entered),
            complete ? new(0, ForecastStatus.Actual, "Khấu trừ", null, Entered) : null)).ToArray(), []);

    private static async Task<Guid> Save(DemoHost host, ForecastAnnualInput input)
    {
        await using var context = host.Services.GetRequiredService<SyntheticDatabase>().Open();
        return await new ForecastStore(context).CreateAsync(ForecastDraftReader.Encode(input));
    }
    private static string ExportLink(string html) => WebUtility.HtmlDecode(
        Regex.Match(html, "<a\\b[^>]*href=\"([^\"]*handler=Csv[^\"]*)\"").Groups[1].Value);
    private static List<string[]> Parse(byte[] csv)
    {
        using var parser = new TextFieldParser(new StringReader(Encoding.UTF8.GetString(csv).TrimStart('\ufeff'))) {
            TextFieldType = FieldType.Delimited, HasFieldsEnclosedInQuotes = true, TrimWhiteSpace = false
        };
        parser.SetDelimiters(",");
        var rows = new List<string[]>();
        while (!parser.EndOfData) rows.Add(parser.ReadFields()!);
        return rows;
    }
    private sealed class ReportClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = Entered;
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
