// Loại đầu vào: decimal chính xác; missing khác 0; trạng thái độc lập; metadata Unicode; giai đoạn và ngoại lệ; phiên mới; cập nhật cũ; lỗi audit; xóa xác nhận; protection trống
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using TaxManager.Web.Demo;
using Xunit;

namespace TaxManager.Web.Tests.Forecasting;

/// <summary>
/// Independent acceptance probes. Writes use advertised HTTP form actions, never a forecast
/// service or a forecast serializer. SQL is only an observer and an audit-failure injector.
/// The fixed product input is twelve months, not a bulk-import/performance contract.
/// </summary>
public sealed class ForecastPersistenceBlackBoxTests
{
    [Fact]
    public async Task New_host_upgrades_stage_two_store_and_computes_without_rewriting_saved_payload()
    {
        string root;
        Guid id;
        string payload;
        await using (var first = await DemoHost.StartAsync())
        {
            root = Storage(first).RootPath;
            using var browser = new Browser(first.Address);
            var values = new Dictionary<string, string> { ["Gross"] = "30000000" };
            for (var i = 0; i < 12; i++)
            {
                values[$"Exceptions[{i}].Month"] = (i + 1).ToString();
                values[$"Exceptions[{i}].Insurance"] = "0";
                values[$"Exceptions[{i}].Dependents"] = "0";
            }
            await Save(browser, await browser.Get("/Forecast"), values);
            await using var context = Storage(first).Open();
            var saved = Assert.Single(await new TaxManager.Data.Forecasting.ForecastStore(context).ListAsync());
            id = saved.Id;
            payload = saved.Payload!;
            await context.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>()
                .MigrateAsync("20261126101326_AddForecastSnapshots");
        }
        await using var second = await DemoHost.StartAsync(syntheticRoot: root);
        using var reopened = new Browser(second.Address);
        var page = await reopened.Get("/Forecast?id=" + id);
        var result = await reopened.Submit(page, "Tính thuế năm");
        Assert.Contains("11.400.000", WebUtility.HtmlDecode(result.Html));
        await using var upgraded = Storage(second).Open();
        Assert.Equal(payload, (await new TaxManager.Data.Forecasting.ForecastStore(upgraded).ReadAsync(id))!.Payload);
        Assert.Single(await new TaxManager.Data.Forecasting.AnnualForecastService(upgraded).HistoryAsync(id));
        Assert.False(upgraded.Database.HasPendingModelChanges());
    }

    private sealed class AdjustableClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; }
        public override DateTimeOffset GetUtcNow() => Now;
    }

    [Fact]
    public async Task Dependent_period_survives_restart_without_overwriting_month_data()
    {
        var clock = new AdjustableClock { Now = new DateTimeOffset(2026, 1, 2, 3, 4, 0, TimeSpan.Zero) };
        var input = Inputs();
        input["DependentPeriods[0].FromMonth"] = "7";
        input["DependentPeriods[0].ToMonth"] = "12";
        input["DependentPeriods[0].Count"] = "1";
        input["DependentPeriods[0].Status"] = "Planned";
        input["DependentPeriods[0].Source"] = "Tự khai";
        input["DependentPeriods[0].Note"] = "Chưa xác nhận đăng ký";
        input["Exceptions[2].Dependents"] = "2";
        string root;
        string[] dates;
        await using (var host = await DemoHost.StartAsync(clock: clock))
        {
            root = Storage(host).RootPath;
            using var browser = new Browser(host.Address);
            await Save(browser, await browser.Get("/Forecast"), input);
            var saved = await OpenSaved(browser, input);
            AssertInputs(input, saved);
            dates = EntryDates(await browser.Submit(saved, "Xem trước")).ToArray();
        }
        clock.Now = clock.Now.AddDays(1);
        await using var reopened = await DemoHost.StartAsync(syntheticRoot: root, clock: clock);
        using var fresh = new Browser(reopened.Address);
        var again = await OpenSaved(fresh, input);
        AssertInputs(input, again);
        Assert.Equal(dates, EntryDates(await fresh.Submit(again, "Xem trước")));
    }

    [Fact]
    public async Task Bonus_metadata_and_entry_time_survive_reopening_the_synthetic_store()
    {
        var clock = new AdjustableClock { Now = new DateTimeOffset(2026, 1, 2, 3, 4, 0, TimeSpan.Zero) };
        var input = Inputs();
        input["Bonuses[0].Month"] = "12";
        input["Bonuses[0].Amount"] = "20000000";
        input["Bonuses[0].Status"] = "Planned";
        input["Bonuses[0].IncludedInGross"] = "true";
        input["Bonuses[0].Source"] = "Kế hoạch thưởng";
        input["Bonuses[0].Note"] = "Dòng một\nDòng hai";
        string root;
        string[] dates;
        await using (var host = await DemoHost.StartAsync(clock: clock))
        {
            root = Storage(host).RootPath;
            using var browser = new Browser(host.Address);
            await Save(browser, await browser.Get("/Forecast"), input);
            var opened = await OpenSaved(browser, input);
            AssertInputs(input, opened);
            var preview = await browser.Submit(opened, "Xem trước");
            Assert.Contains("Kế hoạch thưởng", WebUtility.HtmlDecode(preview.Html));
            dates = EntryDates(preview).ToArray();
        }
        clock.Now = clock.Now.AddDays(1);
        await using var reopened = await DemoHost.StartAsync(syntheticRoot: root, clock: clock);
        using var fresh = new Browser(reopened.Address);
        var saved = await OpenSaved(fresh, input);
        AssertInputs(input, saved);
        Assert.Equal(dates, EntryDates(await fresh.Submit(saved, "Xem trước")));
    }

    [Fact]
    public async Task Reopening_host_rejects_wrong_or_missing_key_even_after_profile_deletion()
    {
        string root;
        await using (var host = await DemoHost.StartAsync())
        {
            root = Storage(host).RootPath;
            using var browser = new Browser(host.Address);
            var input = Inputs();
            await Save(browser, await browser.Get("/Forecast"), input);
            var confirm = await RequestDelete(browser, await OpenSaved(browser, input));
            await browser.Submit(confirm, "Xác nhận.*xóa|Xóa.*xác nhận");
        }
        var key = Path.Combine(root, "synthetic-key.json");
        var original = await File.ReadAllBytesAsync(key);
        try
        {
            await File.WriteAllTextAsync(key, "{\"activeKeyId\":\"synthetic\",\"keys\":{\"synthetic\":\"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=\"}}");
            await AssertRejected(root);
            File.Move(key, key + ".test-missing");
            await AssertRejected(root);
        }
        finally
        {
            await File.WriteAllBytesAsync(key, original);
        }
        await using var healthy = await DemoHost.StartAsync(syntheticRoot: root);
        using var fresh = new Browser(healthy.Address);
        Assert.DoesNotContain("SYN_BGOAL_PROFILE_2026", (await fresh.Get("/Forecast")).Html);

        static async Task AssertRejected(string root)
        {
            await using var host = await DemoHost.StartAsync(syntheticRoot: root);
            using var client = new HttpClient { BaseAddress = host.Address };
            using var response = await client.GetAsync("/Forecast");
            Assert.False(response.IsSuccessStatusCode);
            Assert.DoesNotContain("SYN_BGOAL_PROFILE_2026", await response.Content.ReadAsStringAsync());
        }
    }

    [Fact]
    public async Task New_host_reopens_and_updates_all_forecast_inputs()
    {
        string root;
        var input = Inputs();
        string[] dates;
        await using (var first = await DemoHost.StartAsync())
        {
            root = Storage(first).RootPath;
            using var browser = new Browser(first.Address);
            await Save(browser, await browser.Get("/Forecast"), input);
            var opened = await OpenSaved(browser, input);
            dates = EntryDates(await browser.Submit(opened, "Xem trước")).ToArray();
            Assert.NotEmpty(dates);
        }
        await using (var second = await DemoHost.StartAsync(syntheticRoot: root))
        {
            using var browser = new Browser(second.Address);
            var opened = await OpenSaved(browser, input);
            AssertInputs(input, opened);
            var preview = await browser.Submit(opened, "Xem trước");
            Assert.Equal(dates, EntryDates(preview));
            input["Periods[0].Gross"] = "61864197.3210987654321098765";
            await Save(browser, opened, new() { ["Periods[0].Gross"] = input["Periods[0].Gross"] });
        }
        await using var third = await DemoHost.StartAsync(syntheticRoot: root);
        using var fresh = new Browser(third.Address);
        AssertInputs(input, await OpenSaved(fresh, input));
    }

    [Theory]
    [InlineData("Planned", "Assumed", "Actual")]
    [InlineData("Actual", "Planned", "Assumed")]
    public async Task New_browser_reopens_exact_inputs_and_updates_a_period_without_flattening_exceptions(
        string baseStatus, string periodStatus, string exceptionStatus)
    {
        await using var host = await DemoHost.StartAsync();
        using var first = new Browser(host.Address);
        var input = Inputs(baseStatus, periodStatus, exceptionStatus);
        await Save(first, await first.Get("/Forecast"), input);
        var persisted = await DatabaseImage(host);

        using var second = new Browser(host.Address); // no cookies, POST state, or antiforgery token from first
        var reopened = await OpenSaved(second, input);
        AssertInputs(input, reopened);
        var preview = await second.Submit(reopened, "Xem trước");
        Assert.Equal(persisted, await DatabaseImage(host));
        AssertInputs(input, preview);
        var repeated = await second.Submit(preview, "Xem trước");
        Assert.NotEmpty(EntryDates(preview));
        Assert.Equal(EntryDates(preview), EntryDates(repeated));
        Assert.Equal(persisted, await DatabaseImage(host));
        var text = Text(preview.Html);
        Assert.Contains("2026", text);
        Assert.Contains("31.847.529,1234567890123456789", text);
        Assert.Contains("42.958.631,234567890123456789", text);
        Assert.Contains("Bảo hiểm tháng 1: 0", text);
        Assert.Contains("Bảo hiểm tháng 2: Chưa nhập", text);
        Assert.Contains("Người phụ thuộc tháng 1: 0", text);
        Assert.Contains("Người phụ thuộc tháng 2: Chưa nhập", text);
        Assert.Contains("Khấu trừ tháng 1: 0", text);
        Assert.Contains("Khấu trừ tháng 2: Chưa nhập", text);
        Assert.DoesNotContain("<script>window.bgoal", preview.Html, StringComparison.OrdinalIgnoreCase);

        // Change the period, keeping a separately entered zero salary in month 3.
        input["Gross"] = "51975318.9876543210987654321";
        input["Periods[0].Gross"] = "62864197.3210987654321098765";
        await Save(second, reopened, new() { ["Gross"] = input["Gross"], ["Periods[0].Gross"] = input["Periods[0].Gross"] });
        using var third = new Browser(host.Address);
        var updated = await OpenSaved(third, input);
        AssertInputs(input, updated);
        var after = await third.Submit(updated, "Xem trước");
        var rows = Regex.Matches(after.Html, @"<tr\b[^>]*>[\s\S]*?</tr>", RegexOptions.IgnoreCase)
            .Select(m => Text(m.Value)).ToArray();
        var march = Assert.Single(rows.Where(r => Regex.IsMatch(r, @"Tháng\s+3\b")));
        Assert.Matches(@"Tháng\s+3\s+0\s+đồng", march);
        var december = Assert.Single(rows.Where(r => Regex.IsMatch(r, @"Tháng\s+12\b")));
        Assert.Contains("Chưa nhập", december);
        Assert.Contains("51.975.318,9876543210987654321", Text(after.Html));
        Assert.Contains("62.864.197,3210987654321098765", Text(after.Html));

        var editor = await third.Submit(updated, "Mở tháng đã chọn", new() { ["SelectedMonth"] = "3" });
        input["Exceptions[2].Insurance"] = "0";
        await Save(third, editor, new() { ["Exceptions[2].Insurance"] = "0" });
        using var fourth = new Browser(host.Address);
        AssertInputs(input, await OpenSaved(fourth, input));

        await using var context = Storage(host).Open();
        context.ValidateProtection();
        Assert.Empty(await context.MonthlyIncomes.ToListAsync());
        Assert.Empty(await context.CalculationHistory.ToListAsync()); // no invented annual/monthly result
        Assert.NotEmpty(await context.Set<TaxManager.Data.Audit.AuditEvent>().ToListAsync());
    }

    [Fact]
    public async Task Preview_and_invalid_save_do_not_write_and_invalid_text_survives()
    {
        await using var host = await DemoHost.StartAsync();
        using var browser = new Browser(host.Address);
        var initial = await browser.Get("/Forecast");
        var before = await DatabaseImage(host);
        initial = await browser.Submit(initial, "Thêm giai đoạn");
        var preview = await browser.Submit(initial, "Xem trước", Inputs());
        Assert.Equal(before, await DatabaseImage(host));
        var invalid = await Save(browser, preview, new() { ["Exceptions[2].Insurance"] = "1,2,3" }, success: false);
        Assert.Equal(before, await DatabaseImage(host));
        Assert.Equal("1,2,3", Fields(invalid.Html)["Exceptions[2].Insurance"]);
        Assert.Contains("SYN_BGOAL_PERIOD_NOTE", Text(invalid.Html));
        Assert.True(Regex.IsMatch(Text(invalid.Html), "không|lỗi|kiểm tra|hợp lệ|nhập số", RegexOptions.IgnoreCase),
            "An invalid save needs an understandable error while preserving the user's input.");
    }

    [Fact]
    public async Task Stale_browser_cannot_overwrite_a_newer_saved_version()
    {
        await using var host = await DemoHost.StartAsync();
        using var creator = new Browser(host.Address);
        var input = Inputs();
        await Save(creator, await creator.Get("/Forecast"), input);
        using var a = new Browser(host.Address);
        using var b = new Browser(host.Address);
        var oldA = await OpenSaved(a, input);
        var oldB = await OpenSaved(b, input);
        await Save(a, oldA, new() { ["Note"] = "SYN_BGOAL_NEWER_NOTE" });
        var before = await DatabaseImage(host);
        var rejected = await Save(b, oldB, new() { ["Note"] = "SYN_BGOAL_STALE_NOTE" }, success: false);
        Assert.Equal(before, await DatabaseImage(host));
        Assert.Equal("SYN_BGOAL_STALE_NOTE", Fields(rejected.Html)["Note"]);
        Assert.Matches("(?i)cập nhật|thay đổi|xung đột|phiên bản|mới hơn", Text(rejected.Html));
        input["Note"] = "SYN_BGOAL_NEWER_NOTE";
        using var fresh = new Browser(host.Address);
        AssertInputs(input, await OpenSaved(fresh, input));
    }

    [Theory]
    [InlineData("create")]
    [InlineData("update")]
    [InlineData("delete")]
    public async Task Audit_failure_rolls_back_the_whole_HTTP_operation(string operation)
    {
        await using var host = await DemoHost.StartAsync();
        using var browser = new Browser(host.Address);
        var input = Inputs();
        var page = await browser.Get("/Forecast");
        if (operation != "create")
        {
            await Save(browser, page, input);
            page = await OpenSaved(browser, input);
        }
        if (operation == "delete") page = await RequestDelete(browser, page);
        var before = await DatabaseImage(host);
        await Sql(host, "CREATE TRIGGER BGoalRejectAudit BEFORE INSERT ON AuditEvents " +
            "BEGIN SELECT RAISE(ABORT, 'synthetic b-goal audit failure'); END");
        Page failed;
        try
        {
            failed = operation == "delete"
                ? await browser.Submit(page, "Xác nhận.*xóa|Xóa.*xác nhận", expectedSuccess: false)
                : await Save(browser, page, operation == "create" ? input : new() { ["Note"] = "SYN_BGOAL_RETRY_NOTE" }, success: false);
            Assert.Equal(before, await DatabaseImage(host));
            Assert.Matches("(?i)lỗi|không|chưa|thất bại", Text(failed.Html));
            if (operation != "delete")
                Assert.Equal(operation == "create" ? input["Note"] : "SYN_BGOAL_RETRY_NOTE", Fields(failed.Html)["Note"]);
        }
        finally { await Sql(host, "DROP TRIGGER BGoalRejectAudit"); }

        // Prove that the injected fault, rather than permanently invalid input, caused the rejection.
        if (operation == "delete")
        {
            var open = await OpenSaved(browser, input);
            var confirm = await RequestDelete(browser, open);
            await browser.Submit(confirm, "Xác nhận.*xóa|Xóa.*xác nhận");
        }
        else
        {
            await Save(browser, failed);
            if (operation == "update") input["Note"] = "SYN_BGOAL_RETRY_NOTE";
            using var fresh = new Browser(host.Address);
            AssertInputs(input, await OpenSaved(fresh, input));
        }
    }

    [Fact]
    public async Task Delete_requires_a_separate_confirmation_and_preserves_existing_history_and_audit()
    {
        await using var host = await DemoHost.StartAsync();
        // Existing monthly snapshots are independent of forecast inputs and must survive this flow.
        using (var monthly = new TaxManager.Web.Tests.MonthlySalary.FormClient(host.Address))
            Assert.Contains("Kết quả 12 tháng", await monthly.PostAsync(year: "2026"));
        var oldHistory = await ExistingHistory(host);
        using var browser = new Browser(host.Address);
        var input = Inputs();
        await Save(browser, await browser.Get("/Forecast"), input);
        var opened = await OpenSaved(browser, input);
        input["Note"] = "SYN_BGOAL_EDIT_WITH_OLD_HISTORY";
        await Save(browser, opened, new() { ["Note"] = input["Note"] });
        opened = await OpenSaved(browser, input);
        Assert.Equal(oldHistory, await ExistingHistory(host));
        var audit = await AuditRows(host);
        var before = await DatabaseImage(host);
        var confirmation = await RequestDelete(browser, opened);
        Assert.Equal(before, await DatabaseImage(host));
        var explanation = Text(confirmation.Html);
        Assert.Matches("(?i)lịch sử", explanation);
        Assert.Matches("(?i)audit|nhật ký", explanation);
        Assert.Matches("(?i)bản sao|sao lưu|WAL|vật lý", explanation);
        // Leaving the confirmation page is a cancellation and cannot delete the record.
        AssertInputs(input, await OpenSaved(browser, input));
        await browser.Submit(confirmation, "Xác nhận.*xóa|Xóa.*xác nhận");
        using var fresh = new Browser(host.Address);
        var oldUrl = await fresh.Get(opened.Address.PathAndQuery, expectedSuccess: false);
        Assert.False(Fields(oldUrl.Html).TryGetValue("Source", out var source) && source == input["Source"],
            "A deleted forecast must no longer reopen as an editable live profile.");
        Assert.Equal(oldHistory, await ExistingHistory(host));
        var retained = await AuditRows(host);
        Assert.True(retained.Count > audit.Count, "Deletion must append audit evidence.");
        foreach (var row in audit) Assert.Contains(row, retained);
    }

    [Fact]
    public async Task Saved_money_and_sensitive_metadata_are_not_plaintext_in_SQLite_or_sidecars()
    {
        await using var host = await DemoHost.StartAsync();
        using var browser = new Browser(host.Address);
        var input = Inputs();
        await Save(browser, await browser.Get("/Forecast"), input);
        using var fresh = new Browser(host.Address);
        AssertInputs(input, await OpenSaved(fresh, input)); // prevents a no-op from satisfying confidentiality
        await using var context = Storage(host).Open();
        context.ValidateProtection();
        var path = Path.GetFullPath(context.Database.GetDbConnection().DataSource);
        Assert.StartsWith(Path.GetFullPath(Path.GetTempPath()), path, StringComparison.OrdinalIgnoreCase);
        var markers = input.Values.SelectMany(v => Regex.Matches(v, @"SYN_BGOAL_[A-Z0-9_]+").Select(m => m.Value))
            .Concat(new[] { "SYN_BGOAL_PROFILE_2026", input["Gross"], input["Periods[0].Gross"], input["Exceptions[2].Insurance"] }).Distinct();
        foreach (var file in Directory.EnumerateFiles(Path.GetDirectoryName(path)!))
        {
            var bytes = await File.ReadAllBytesAsync(file);
            foreach (var marker in markers)
                foreach (var encoding in new[] { Encoding.UTF8, Encoding.Unicode, Encoding.BigEndianUnicode })
                    Assert.DoesNotContain(marker, encoding.GetString(bytes));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Invalid_or_missing_key_sentinel_cannot_turn_a_missing_record_into_a_successful_empty_read(bool missing)
    {
        await using var host = await DemoHost.StartAsync();
        using var browser = new Browser(host.Address);
        var input = Inputs();
        await Save(browser, await browser.Get("/Forecast"), input);
        var opened = await OpenSaved(browser, input);
        var confirmation = await RequestDelete(browser, opened);
        await browser.Submit(confirmation, "Xác nhận.*xóa|Xóa.*xác nhận");
        using var healthyBrowser = new Browser(host.Address);
        var healthyMissing = await healthyBrowser.Get(opened.Address.PathAndQuery, expectedSuccess: false);
        // The requested record is now absent, but the initialized database still has protected data/audit.
        await Sql(host, missing ? "DELETE FROM DataProtectionMetadata"
            : "UPDATE DataProtectionMetadata SET KeyCheck = zeroblob(length(KeyCheck))");
        var damaged = await DatabaseImage(host);
        using var fresh = new Browser(host.Address);
        var rejected = await fresh.Get(opened.Address.PathAndQuery, expectedSuccess: false);
        Assert.Matches("(?i)khóa|bảo vệ|mã hóa|không.*mở|không.*đọc|không.*tải|lỗi", Text(rejected.Html));
        Assert.NotEqual(Text(healthyMissing.Html), Text(rejected.Html));
        Assert.DoesNotContain(input["Source"], Text(rejected.Html));
        Assert.Equal(damaged, await DatabaseImage(host)); // no silent sentinel repair or default replacement
    }

    private static Dictionary<string, string> Inputs(string baseStatus = "Planned", string periodStatus = "Assumed", string exceptionStatus = "Actual")
    {
        var values = new Dictionary<string, string>
        {
            ["FromMonth"] = "1", ["ToMonth"] = "6", ["Gross"] = "31847529.1234567890123456789",
            ["Status"] = baseStatus, ["Source"] = "SYN_BGOAL_BASE_SOURCE",
            ["Note"] = "SYN_BGOAL_BASE_NOTE — dự kiến, không phải thực tế <script>window.bgoal=1</script>",
            ["Periods[0].FromMonth"] = "7", ["Periods[0].ToMonth"] = "11",
            ["Periods[0].Gross"] = "42958631.234567890123456789", ["Periods[0].Status"] = periodStatus,
            ["Periods[0].Source"] = "SYN_BGOAL_PERIOD_SOURCE", ["Periods[0].Note"] = "SYN_BGOAL_PERIOD_NOTE — lý do giả định tăng lương",
            ["Exceptions[2].Gross"] = "0", ["Exceptions[2].Status"] = exceptionStatus,
            ["Exceptions[2].Source"] = "SYN_BGOAL_ZERO_SOURCE", ["Exceptions[2].Note"] = "SYN_BGOAL_ZERO_NOTE — nghỉ không lương",
            ["Exceptions[0].Insurance"] = "0", ["Exceptions[0].InsuranceStatus"] = baseStatus,
            ["Exceptions[0].InsuranceSource"] = "SYN_BGOAL_INS_SOURCE", ["Exceptions[0].InsuranceNote"] = "SYN_BGOAL_INS_NOTE",
            ["Exceptions[0].Dependents"] = "0", ["Exceptions[0].DependentsStatus"] = periodStatus,
            ["Exceptions[0].DependentsSource"] = "SYN_BGOAL_DEP_SOURCE", ["Exceptions[0].DependentsNote"] = "SYN_BGOAL_DEP_NOTE",
            ["Exceptions[0].Withheld"] = "0", ["Exceptions[0].WithheldStatus"] = exceptionStatus,
            ["Exceptions[0].WithheldSource"] = "SYN_BGOAL_TAX_SOURCE", ["Exceptions[0].WithheldNote"] = "SYN_BGOAL_TAX_NOTE — giả định chưa khấu trừ",
            ["Exceptions[2].Insurance"] = "1357924.1234567890123456789", ["Exceptions[2].InsuranceStatus"] = periodStatus,
            ["Exceptions[2].InsuranceSource"] = "SYN_BGOAL_INS3_SOURCE", ["Exceptions[2].InsuranceNote"] = "SYN_BGOAL_INS3_NOTE — nhập tay",
            ["Exceptions[2].Dependents"] = "2", ["Exceptions[2].DependentsStatus"] = exceptionStatus,
            ["Exceptions[2].DependentsSource"] = "SYN_BGOAL_DEP3_SOURCE", ["Exceptions[2].DependentsNote"] = "SYN_BGOAL_DEP3_NOTE",
            ["Exceptions[2].Withheld"] = "2468135.9876543210123456789", ["Exceptions[2].WithheldStatus"] = baseStatus,
            ["Exceptions[2].WithheldSource"] = "SYN_BGOAL_TAX3_SOURCE", ["Exceptions[2].WithheldNote"] = "SYN_BGOAL_TAX3_NOTE"
        };
        for (var i = 0; i < 12; i++)
        {
            values[$"Exceptions[{i}].Month"] = (i + 1).ToString(CultureInfo.InvariantCulture);
            foreach (var amount in new[] { "Gross", "Insurance", "Dependents", "Withheld" })
                values.TryAdd($"Exceptions[{i}].{amount}", "");
        }
        return values;
    }

    private static async Task<Page> Save(Browser browser, Page page, Dictionary<string, string>? changes = null, bool success = true)
    {
        if (changes?.ContainsKey("Periods[0].Gross") == true && !Fields(page.Html).ContainsKey("Periods[0].Gross"))
            page = await browser.Submit(page, "Thêm giai đoạn");
        const string save = @"\bLưu\b|Cập nhật";
        if (!Regex.Matches(page.Html, @"<button\b[^>]*>[\s\S]*?</button>", RegexOptions.IgnoreCase)
            .Any(m => Regex.IsMatch(Text(m.Value), save, RegexOptions.IgnoreCase)))
        {
            page = await browser.Submit(page, "Xem trước", changes);
            changes = null;
        }
        return await browser.Submit(page, save, changes, success);
    }

    private static async Task<Page> RequestDelete(Browser browser, Page page)
        => await browser.Act(page, @"Xóa", @"tháng|giai đoạn");

    private static async Task<Page> OpenSaved(Browser browser, Dictionary<string, string> expected)
    {
        // Start from navigation, never reuse the save response or the creator's cookies.
        var queue = new Queue<(string Url, int Depth)>();
        queue.Enqueue(("/Forecast", 0));
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (queue.TryDequeue(out var next))
        {
            if (!seen.Add(next.Url)) continue;
            var page = await browser.Get(next.Url);
            var fields = Fields(page.Html);
            if (fields.TryGetValue("Source", out var source) && source == expected["Source"] &&
                fields.TryGetValue("Periods[0].Source", out var period) && period == expected["Periods[0].Source"])
                return page;
            if (next.Depth == 3) continue;
            foreach (Match link in Regex.Matches(page.Html, @"<a\b[^>]*>[\s\S]*?</a>", RegexOptions.IgnoreCase))
            {
                var label = Text(link.Value);
                if (!Regex.IsMatch(label, "mở|sửa|hồ sơ|đã lưu|dự báo|tiếp tục|2026|SYN_BGOAL_PROFILE", RegexOptions.IgnoreCase) ||
                    Regex.IsMatch(label, "xóa|mới|tạo", RegexOptions.IgnoreCase)) continue;
                var href = Attr(link.Value, "href");
                if (href.Length == 0 || href.StartsWith('#')) continue;
                var uri = new Uri(page.Address, href);
                if (uri.GetLeftPart(UriPartial.Authority) == page.Address.GetLeftPart(UriPartial.Authority))
                    queue.Enqueue((uri.PathAndQuery, next.Depth + 1));
            }
            // A list may advertise a POST Open button instead of an anchor.
            foreach (var form in Forms(page.Html))
                foreach (Match button in Regex.Matches(form, @"<button\b[^>]*>[\s\S]*?</button>", RegexOptions.IgnoreCase))
                    if (Regex.IsMatch(Text(button.Value), @"^(Mở|Chỉnh sửa|Tiếp tục)\b", RegexOptions.IgnoreCase))
                    {
                        var opened = await browser.Send(page, form, button.Value, null, true);
                        if (Fields(opened.Html).GetValueOrDefault("Source") == expected["Source"]) return opened;
                    }
        }
        Assert.Fail("A new browser must be able to find and reopen the saved 2026 forecast through advertised navigation.");
        throw new InvalidOperationException();
    }

    private static void AssertInputs(Dictionary<string, string> expected, Page page)
    {
        var actual = Fields(page.Html);
        foreach (var (name, value) in expected)
        {
            Assert.True(actual.TryGetValue(name, out var found), $"Reopened editable form lost field {name}.");
            if (value.Length > 0 && Regex.IsMatch(name, @"(^Gross$|\.(Gross|Insurance|Withheld)$)"))
                Assert.Equal(decimal.Parse(value, CultureInfo.InvariantCulture),
                    decimal.Parse(found!.Replace(',', '.'), CultureInfo.InvariantCulture));
            else Assert.Equal(value, found);
        }
    }

    private static string[] EntryDates(Page page)
    {
        var dates = Fields(page.Html).Where(p => Regex.IsMatch(p.Key, "Entered|Nhập|Timestamp", RegexOptions.IgnoreCase))
            .OrderBy(p => p.Key).ToArray();
        var displayed = Regex.Matches(Text(page.Html), @"\b\d{2}/\d{2}/\d{4}\s+\d{2}:\d{2}(?::\d{2})?\s+UTC\b")
            .Select(m => m.Value).Order().ToArray();
        return dates.Select(p => p.Key + "=" + p.Value).Concat(displayed).Order(StringComparer.Ordinal).ToArray();
    }

    private static SyntheticDatabase Storage(DemoHost host) => host.Services.GetRequiredService<SyntheticDatabase>();

    private static async Task Sql(DemoHost host, string sql)
    {
        await using var context = Storage(host).Open();
        await context.Database.ExecuteSqlRawAsync(sql);
    }

    private static async Task<string> DatabaseImage(DemoHost host)
    {
        await using var context = Storage(host).Open();
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();
        var tables = new List<string>();
        await using (var names = connection.CreateCommand())
        {
            names.CommandText = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name";
            await using var reader = await names.ExecuteReaderAsync();
            while (await reader.ReadAsync()) tables.Add(reader.GetString(0));
        }
        var result = new SortedDictionary<string, string[]>();
        foreach (var table in tables)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM \"" + table.Replace("\"", "\"\"") + "\"";
            await using var reader = await command.ExecuteReaderAsync();
            var rows = new List<string>();
            while (await reader.ReadAsync())
            {
                var row = new object[reader.FieldCount];
                reader.GetValues(row);
                rows.Add(JsonSerializer.Serialize(row.Select(v => v is byte[] bytes ? Convert.ToHexString(bytes) : v)));
            }
            result[table] = rows.Order(StringComparer.Ordinal).ToArray();
        }
        return JsonSerializer.Serialize(result);
    }

    private static async Task<string> ExistingHistory(DemoHost host)
    {
        await using var c = Storage(host).Open();
        c.ValidateProtection();
        return JsonSerializer.Serialize((await c.CalculationHistory.AsNoTracking().OrderBy(x => x.Id).ToListAsync())
            .Select(x => new { x.Id, x.ProfileId, x.Year, x.Month, x.CreatedAtUtc, x.PayloadEncrypted, x.Snapshot }));
    }

    private static async Task<List<string>> AuditRows(DemoHost host)
    {
        await using var c = Storage(host).Open();
        c.ValidateProtection();
        return (await c.Set<TaxManager.Data.Audit.AuditEvent>().AsNoTracking().OrderBy(x => x.Id).ToListAsync())
            .Select(x => JsonSerializer.Serialize(new { x.Id, x.OperationId, x.Ordinal, x.ProfileId,
                x.Entity, x.EntityKey, x.Action, x.TimestampUtc, x.Payload, Content = c.ReadAudit(x) })).ToList();
    }

    private sealed record Page(Uri Address, string Html);

    private sealed class Browser : IDisposable
    {
        private readonly HttpClient client;
        public Browser(Uri address) => client = new(new HttpClientHandler { UseCookies = true })
            { BaseAddress = address, Timeout = TimeSpan.FromSeconds(30) };
        public async Task<Page> Get(string url, bool expectedSuccess = true)
        {
            using var response = await client.GetAsync(url);
            if (expectedSuccess) response.EnsureSuccessStatusCode();
            return new(response.RequestMessage!.RequestUri!, await response.Content.ReadAsStringAsync());
        }
        public async Task<Page> Submit(Page page, string label, Dictionary<string, string>? changes = null, bool expectedSuccess = true)
        {
            foreach (var form in Forms(page.Html))
                foreach (Match button in Regex.Matches(form, @"<button\b[^>]*>[\s\S]*?</button>|<input\b[^>]*type\s*=\s*[""']submit[""'][^>]*>", RegexOptions.IgnoreCase))
                    if (Regex.IsMatch(Text(button.Value) + " " + Attr(button.Value, "value"), label, RegexOptions.IgnoreCase))
                        return await Send(page, form, button.Value, changes, expectedSuccess);
            Assert.Fail($"The rendered UI has no submit control matching '{label}'. URL: {page.Address.PathAndQuery}");
            throw new InvalidOperationException();
        }
        public async Task<Page> Act(Page page, string label, string exclude)
        {
            foreach (Match link in Regex.Matches(page.Html, @"<a\b[^>]*>[\s\S]*?</a>", RegexOptions.IgnoreCase))
                if (Regex.IsMatch(Text(link.Value), label, RegexOptions.IgnoreCase) &&
                    !Regex.IsMatch(Text(link.Value), exclude, RegexOptions.IgnoreCase))
                    return await Get(new Uri(page.Address, Attr(link.Value, "href")).PathAndQuery);
            foreach (var form in Forms(page.Html))
                foreach (Match button in Regex.Matches(form, @"<button\b[^>]*>[\s\S]*?</button>", RegexOptions.IgnoreCase))
                    if (Regex.IsMatch(Text(button.Value), label, RegexOptions.IgnoreCase) &&
                        !Regex.IsMatch(Text(button.Value), exclude, RegexOptions.IgnoreCase))
                        return await Send(page, form, button.Value, null, true);
            Assert.Fail("The saved profile needs a discoverable intentional delete action.");
            throw new InvalidOperationException();
        }
        public async Task<Page> Send(Page page, string form, string button, Dictionary<string, string>? changes, bool expectedSuccess)
        {
            var values = Fields(form);
            foreach (var entry in changes ?? []) values[entry.Key] = entry.Value;
            foreach (Match label in Regex.Matches(form, @"<label\b[^>]*>[\s\S]*?</label>", RegexOptions.IgnoreCase))
            {
                if (!Regex.IsMatch(Text(label.Value), "tên.*(hồ sơ|bản lưu|dự báo)", RegexOptions.IgnoreCase)) continue;
                var id = Attr(label.Value, "for");
                foreach (Match input in Regex.Matches(form, @"<input\b[^>]*>", RegexOptions.IgnoreCase))
                    if (id.Length > 0 && Attr(input.Value, "id") == id &&
                        values.GetValueOrDefault(Attr(input.Value, "name")) == "")
                        values[Attr(input.Value, "name")] = "SYN_BGOAL_PROFILE_2026";
            }
            if (Regex.IsMatch(Text(button), "Xác nhận.*xóa|Xóa.*xác nhận", RegexOptions.IgnoreCase))
                foreach (Match checkbox in Regex.Matches(form, @"<input\b[^>]*>", RegexOptions.IgnoreCase))
                    if (Attr(checkbox.Value, "type") == "checkbox" && Attr(checkbox.Value, "name") is { Length: > 0 } checkName)
                        values[checkName] = Attr(checkbox.Value, "value");
            if (Attr(button, "name") is { Length: > 0 } name) values[name] = Attr(button, "value");
            var action = Attr(button, "formaction");
            if (action.Length == 0) action = Attr(form[..(form.IndexOf('>') + 1)], "action");
            var address = new Uri(page.Address, action.Length == 0 ? page.Address.PathAndQuery : action);
            Assert.Equal(client.BaseAddress!.GetLeftPart(UriPartial.Authority), address.GetLeftPart(UriPartial.Authority));
            Assert.True(values.ContainsKey("__RequestVerificationToken"), "Use the real form's antiforgery token.");
            using var response = await client.PostAsync(address, new FormUrlEncodedContent(values));
            if (expectedSuccess) response.EnsureSuccessStatusCode();
            return new(response.RequestMessage!.RequestUri!, await response.Content.ReadAsStringAsync());
        }
        public void Dispose() => client.Dispose();
    }

    private static IEnumerable<string> Forms(string html) =>
        Regex.Matches(html, @"<form\b[^>]*>[\s\S]*?</form>", RegexOptions.IgnoreCase).Select(m => m.Value);

    private static Dictionary<string, string> Fields(string html)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Match tag in Regex.Matches(html, @"<input\b[^>]*>|<textarea\b[^>]*>[\s\S]*?</textarea>|<select\b[^>]*>[\s\S]*?</select>", RegexOptions.IgnoreCase))
        {
            var name = Attr(tag.Value, "name");
            if (name.Length == 0 || Regex.IsMatch(tag.Value.Split('>')[0], @"\sdisabled(?:\s|=|>)", RegexOptions.IgnoreCase)) continue;
            var type = Attr(tag.Value, "type");
            if (type is "submit" or "button") continue;
            if ((type is "checkbox" or "radio") && !Regex.IsMatch(tag.Value, @"\schecked(?:\s|=|/?>)", RegexOptions.IgnoreCase)) continue;
            string value;
            if (tag.Value.StartsWith("<textarea", StringComparison.OrdinalIgnoreCase))
                value = WebUtility.HtmlDecode(tag.Value[(tag.Value.IndexOf('>') + 1)..tag.Value.LastIndexOf('<')]);
            else if (tag.Value.StartsWith("<select", StringComparison.OrdinalIgnoreCase))
            {
                var options = Regex.Matches(tag.Value, @"<option\b[^>]*>[\s\S]*?</option>", RegexOptions.IgnoreCase).Select(m => m.Value).ToArray();
                var selected = options.FirstOrDefault(o => Regex.IsMatch(o.Split('>')[0], @"\sselected(?:\s|=|$)", RegexOptions.IgnoreCase)) ?? options.FirstOrDefault() ?? "";
                value = Attr(selected, "value");
            }
            else value = Attr(tag.Value, "value");
            result.TryAdd(name, value); // checkbox's checked value precedes the hidden false fallback
        }
        return result;
    }

    private static string Attr(string tag, string name)
    {
        var opening = tag.Split('>')[0];
        var match = Regex.Match(opening, @"(?:\s|^)" + Regex.Escape(name) + @"\s*=\s*(?:""([^""]*)""|'([^']*)')", RegexOptions.IgnoreCase);
        return WebUtility.HtmlDecode(match.Success ? match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value : "");
    }
    private static string Text(string html) => Regex.Replace(WebUtility.HtmlDecode(Regex.Replace(html, @"<[^>]+>", " ")), @"\s+", " ").Trim();
}
