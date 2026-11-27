// Loại đầu vào: gross theo giai đoạn và ngoại lệ, bảo hiểm nhập tay, người phụ thuộc từng tháng, người phụ thuộc theo khoảng tháng, thưởng thực tế và dự kiến đã gồm hoặc chưa gồm gross, khấu trừ đủ và thiếu, nghĩa vụ thiếu khác số zero, Actual Planned Assumed độc lập, metadata Unicode nhiều dòng và ngày nhập, tiền lẻ dấu phẩy và dấu chấm, tiền không hợp lệ, form chưa lưu, hồ sơ cũ khi tính và xóa, catalog thiếu kỳ, lịch sử năm append-only và audit lỗi, khóa sai thiếu và sentinel lỗi
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaxManager.Web.Demo;
using Xunit;

namespace TaxManager.Web.Tests.Forecasting;

/// <summary>
/// Stage 3 acceptance probes derived from the supplied request, with no task-plan input.
/// All user writes follow advertised HTTP actions, including antiforgery. SQL observes
/// persisted bytes or injects faults; it never manufactures a tax result or forecast input.
/// Money oracles are arithmetic constants, not a call to the production calculator.
/// The product input is one fixed twelve-month profile, not a bulk-processing interface.
/// These HTTP probes do not claim visual/mobile or real keyboard QA.
/// </summary>
public sealed class AnnualSavedProfileBlackBoxTests
{
    [Theory]
    [InlineData("gross theo giai đoạn và ngoại lệ", false, 2700000, 0)]
    [InlineData("bảo hiểm nhập tay", true, 2100000, 12000000)]
    public async Task Annual_obligation_uses_one_annual_clamp_and_independent_arithmetic(
        string inputType, bool insurance, decimal expectedTax, decimal expectedInsurance)
    {
        Assert.NotEmpty(inputType);
        await using var host = await DemoHost.StartAsync();
        using var browser = new Browser(host.Address);
        var input = CompleteInputs();
        input["Gross"] = "0"; // Explicitly supplied zero for January-June.
        input["Periods[0].Gross"] = "40000000";
        for (var i = 6; i < 12; i++)
        {
            input[$"Exceptions[{i}].Insurance"] = insurance ? "2000000" : "0";
            input[$"Exceptions[{i}].Withheld"] = "1950000";
        }
        await Save(browser, await browser.Get("/Forecast"), input);
        var opened = await OpenSaved(browser, input);
        var result = await Calculate(browser, opened);
        // 240m - 186m - insurance = 54m or 42m; all in the 5% annual band.
        // Without insurance, summing monthly taxes gives 11.7m, NOT annual 2.7m.
        AssertMetric(result, "tổng.*thu nhập|thu nhập.*(chịu thuế|gross)", 240_000_000m);
        AssertMetric(result, "bảo hiểm", expectedInsurance);
        AssertMetric(result, "thu nhập tính thuế", 54_000_000m - expectedInsurance);
        AssertLiability(result, expectedTax);
        AssertMetric(result, "(ước tính.*)?(nộp thừa|khấu trừ thừa|thừa đã khấu trừ)", 11_700_000m - expectedTax);
        AssertCoverage(result, "(nghĩa vụ|đủ dữ liệu|thực tế)", 12);
        AssertCoverage(result, "khấu trừ", 12);
        Assert.Matches("(?i)bậc|biểu thuế|lũy tiến", Output(result));
        Assert.Matches("5\\s*%|0[,.]05", Output(result));
        Assert.Matches("(?i)ước tính|tham khảo", Output(result));
        Assert.DoesNotMatch("(?i)(chắc chắn|đương nhiên|đảm bảo).{0,40}(hoàn|nhận lại)", Output(result));
        Assert.Matches("(?i)chưa (được )?(xác minh|kiểm chứng)|Unverified|chưa thẩm định", Output(result));
        Assert.Contains("2026", Output(result));
        Assert.True(Regex.IsMatch(result.Html, @"href\s*=\s*[""']https://[^""']*(chinhphu\.vn|vbpl\.vn)", RegexOptions.IgnoreCase),
            "The result must expose the existing source links and their original legal status.");
        await AssertNoMonthlyPromotion(host);
    }

    [Fact]
    public async Task Gross_periods_zero_exception_and_monthly_dependents_keep_their_separate_meanings()
    {
        await using var host = await DemoHost.StartAsync();
        using var browser = new Browser(host.Address);
        var input = CompleteInputs();
        input["Periods[0].Gross"] = "40000000";
        input["Exceptions[2].Gross"] = "0";
        input["Exceptions[2].Status"] = "Actual";
        input["Exceptions[2].Source"] = "SYN_ANNUAL_LEAVE";
        input["Exceptions[2].Note"] = "Nghỉ không lương tháng 3";
        input["Exceptions[7].Gross"] = "55000000";
        input["Exceptions[7].Status"] = "Actual";
        input["Exceptions[7].Source"] = "SYN_ANNUAL_EXCEPTION_AUGUST";
        for (var i = 0; i < 12; i++)
        {
            input[$"Exceptions[{i}].Insurance"] = "1000000";
            input[$"Exceptions[{i}].Dependents"] = i < 6 ? "0" : "1";
        }
        await Save(browser, await browser.Get("/Forecast"), input);
        var reopened = await OpenSaved(browser, input);
        AssertInputs(input, reopened);
        var result = await Calculate(browser, reopened);
        // 6*30m - 30m + 6*40m + 15m = 405m gross; 12m insurance;
        // six dependent-months*6.2m = 37.2m; taxable 169.8m; tax 10.98m.
        AssertMetric(result, "tổng.*thu nhập|thu nhập.*(chịu thuế|gross)", 405_000_000m);
        AssertMetric(result, "bảo hiểm", 12_000_000m);
        AssertMetric(result, "thu nhập tính thuế", 169_800_000m);
        AssertLiability(result, 10_980_000m);
        AssertCoverage(result, "nghĩa vụ|đủ dữ liệu|thực tế", 12);
        await AssertNoMonthlyPromotion(host);
    }

    [Fact]
    public async Task Fractional_money_with_comma_and_dot_is_not_reinterpreted_as_grouped_thousands()
    {
        await using var host = await DemoHost.StartAsync();
        using var browser = new Browser(host.Address);
        var input = CompleteInputs();
        input["Gross"] = "30000000,50";
        input["Periods[0].Gross"] = "30000000.50";
        for (var i = 0; i < 12; i++)
        {
            input[$"Exceptions[{i}].Insurance"] = "0,50";
            input[$"Exceptions[{i}].Withheld"] = "950000,00";
        }
        await Save(browser, await browser.Get("/Forecast"), input);
        var reopened = await OpenSaved(browser, input);
        Assert.Equal(30_000_000.50m, decimal.Parse(Fields(reopened.Html)["Gross"].Replace(',', '.'), CultureInfo.InvariantCulture));
        Assert.Equal(.50m, decimal.Parse(Fields(reopened.Html)["Exceptions[0].Insurance"].Replace(',', '.'), CultureInfo.InvariantCulture));
        var result = await Calculate(browser, reopened);
        // The fractional gross and insurance cancel: 360000006 - 6 - 186m = 174m.
        AssertMetric(result, "tổng.*thu nhập|thu nhập.*(chịu thuế|gross)", 360_000_006m);
        AssertMetric(result, "bảo hiểm", 6m);
        AssertLiability(result, 11_400_000m);
        AssertMetric(result, "đã khấu trừ|tổng.*khấu trừ", 11_400_000m);
    }

    [Theory]
    [InlineData("khấu trừ chưa nhập cả năm", 0)]
    [InlineData("khấu trừ thiếu tháng 12", 11)]
    [InlineData("khấu trừ zero tường minh", 12)]
    public async Task Withholding_coverage_is_independent_and_missing_never_becomes_zero(string inputType, int supplied)
    {
        Assert.NotEmpty(inputType);
        await using var host = await DemoHost.StartAsync();
        using var browser = new Browser(host.Address);
        var input = CompleteInputs();
        for (var i = supplied; i < 12; i++) input[$"Exceptions[{i}].Withheld"] = "";
        await Save(browser, await browser.Get("/Forecast"), input);
        var result = await Calculate(browser, await OpenSaved(browser, input));
        // 360m - 186m = 174m; 120m*5% + 54m*10% = 11.4m.
        AssertLiability(result, 11_400_000m);
        AssertCoverage(result, "khấu trừ", supplied);
        if (supplied == 12) AssertMetric(result, "nộp thêm|phải nộp thêm", 11_400_000m);
        else
        {
            AssertNoNumericDifference(result);
            Assert.Matches("(?i)(thiếu|chưa (nhập|đủ|có)).{0,100}khấu trừ|khấu trừ.{0,100}(thiếu|chưa)", Output(result));
            var reopened = await OpenSaved(browser, input);
            Assert.Equal("", Fields(reopened.Html)["Exceptions[11].Withheld"]);
        }
        await AssertNoMonthlyPromotion(host);
    }

    [Theory]
    [InlineData("gross tháng 5 còn thiếu", "Gross", "lương|gross")]
    [InlineData("bảo hiểm tháng 5 còn thiếu", "Insurance", "bảo hiểm")]
    [InlineData("người phụ thuộc tháng 5 còn thiếu", "Dependents", "người phụ thuộc")]
    public async Task Missing_obligation_names_month_and_indicator_and_does_not_append_a_result(
        string inputType, string field, string indicator)
    {
        Assert.NotEmpty(inputType);
        await using var host = await DemoHost.StartAsync();
        using var browser = new Browser(host.Address);
        var input = CompleteInputs();
        if (field == "Gross")
        {
            // A blank exception would inherit known base gross. Leave a real schedule gap instead.
            input["ToMonth"] = "4";
            input["Periods[0].FromMonth"] = "6";
        }
        else input[$"Exceptions[4].{field}"] = "";
        await Save(browser, await browser.Get("/Forecast"), input);
        var opened = await OpenSaved(browser, input);
        var before = await DatabaseImage(host);
        var result = HasAction(opened, CalculateLabel)
            ? await Calculate(browser, opened)
            : await browser.Submit(opened, "Xem trước");
        var text = Output(result);
        Assert.Matches("(?i)thiếu|chưa nhập|chưa đủ", text);
        Assert.Matches("(?i)(tháng\\s*5.{0,100}(" + indicator + ")|(" + indicator + ").{0,100}tháng\\s*5)", text);
        Assert.DoesNotMatch("(?i)(nghĩa vụ thuế|thuế năm).{0,70}11[., ]?400[., ]?000\\s*(đồng|VND|₫)", text);
        AssertNoNumericDifference(result);
        Assert.Equal(before, await DatabaseImage(host));
    }

    [Theory]
    [InlineData("bảo hiểm Planned độc lập", "Insurance", "Planned", 11)]
    [InlineData("người phụ thuộc Assumed độc lập", "Dependents", "Assumed", 11)]
    [InlineData("khấu trừ Planned tách nghĩa vụ", "Withheld", "Planned", 12)]
    public async Task Actual_month_count_requires_each_obligation_group_and_status_survives_reopen(
        string inputType, string group, string status, int actual)
    {
        Assert.NotEmpty(inputType);
        var input = CompleteInputs();
        input[$"Exceptions[11].{group}Status"] = status;
        input[$"Exceptions[11].{group}Note"] = "SYN_ANNUAL giả định từ bảng lương\nChưa xác nhận thực tế — <b>ghi chú</b>";
        string root;
        string[] entered;
        var clock = new ProbeClock();
        await using (var host = await DemoHost.StartAsync(clock: clock))
        {
            root = Storage(host).RootPath;
            using var browser = new Browser(host.Address);
            await Save(browser, await browser.Get("/Forecast"), input);
            var opened = await OpenSaved(browser, input);
            entered = EntryDates(await browser.Submit(opened, "Xem trước"));
            Assert.NotEmpty(entered);
            var result = await Calculate(browser, opened);
            AssertLiability(result, 11_400_000m);
            AssertCoverage(result, "nghĩa vụ|đủ dữ liệu|thực tế", actual);
            Assert.Equal(status, Fields((await OpenSaved(browser, input)).Html)[$"Exceptions[11].{group}Status"]);
        }
        clock.Now = clock.Now.AddDays(3);
        await using var reopenedHost = await DemoHost.StartAsync(clock: clock, syntheticRoot: root);
        using var fresh = new Browser(reopenedHost.Address);
        var reopened = await OpenSaved(fresh, input);
        Assert.Equal(status, Fields(reopened.Html)[$"Exceptions[11].{group}Status"]);
        Assert.Equal(input[$"Exceptions[11].{group}Note"], Fields(reopened.Html)[$"Exceptions[11].{group}Note"]);
        Assert.Equal(entered, EntryDates(await fresh.Submit(reopened, "Xem trước")));
        await AssertNoMonthlyPromotion(reopenedHost);
    }

    [Theory]
    [InlineData("thưởng thực tế chưa gồm gross", false, "Actual", 13400000, 12)]
    [InlineData("thưởng dự kiến đã gồm gross", true, "Planned", 11400000, 11)]
    public async Task Current_employer_bonus_roundtrips_its_provenance_and_never_double_counts(
        string inputType, bool included, string status, decimal expectedTax, int actual)
    {
        Assert.NotEmpty(inputType);
        var input = CompleteInputs();
        string root;
        Dictionary<string, string> bonus;
        await using (var host = await DemoHost.StartAsync())
        {
            root = Storage(host).RootPath;
            using var browser = new Browser(host.Address);
            var editor = await browser.Get("/Forecast");
            editor = await EnsureGroup(browser, editor, "thưởng");
            var section = Section(editor.Html, "thưởng");
            bonus = new()
            {
                [LabelField(section, "số tiền|tiền thưởng|thưởng.*đồng")] = "20000000",
                [LabelField(section, "trạng thái")] = status,
                [LabelField(section, "nguồn")] = "SYN_ANNUAL_BONUS bảng thưởng nơi trả hiện tại",
                [LabelField(section, "ghi chú|lý do")] = "SYN_ANNUAL_BONUS_NOTE thưởng năm 2025\nNhận trong 2026 <script>probe()</script>",
                [LabelField(section, "(gồm|trong|vào).*gross|gross.*(gồm|chứa)")] = included ? "true" : "false"
            };
            var period = LabelField(section, "kỳ|tháng|ngày (nhận|trả|chi)");
            var tag = Controls(section).First(t => Attr(t, "name") == period);
            bonus[period] = Attr(tag, "type") switch { "date" => "2026-12-20", "month" => "2026-12", _ => "12" };
            foreach (var entry in bonus) input[entry.Key] = entry.Value;
            await Save(browser, editor, input);
            var result = await Calculate(browser, await OpenSaved(browser, input));
            AssertLiability(result, expectedTax);
            AssertMetric(result, "tổng.*thu nhập|thu nhập.*(chịu thuế|gross)", included ? 360_000_000m : 380_000_000m);
            Assert.Matches("(?i)thưởng.{0,160}(gross|cộng thêm)|gross.{0,160}thưởng", Output(result));
            AssertCoverage(result, "nghĩa vụ|đủ dữ liệu|thực tế", actual);
            Assert.DoesNotContain("<script>probe()", result.Html, StringComparison.OrdinalIgnoreCase);
        }
        await using var second = await DemoHost.StartAsync(syntheticRoot: root);
        using var fresh = new Browser(second.Address);
        var saved = await OpenSaved(fresh, input);
        foreach (var (name, value) in bonus)
        {
            var actualValue = Fields(saved.Html)[name];
            if (bool.TryParse(value, out var expectedFlag)) Assert.Equal(expectedFlag, bool.Parse(actualValue));
            else if (decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var expectedAmount))
                Assert.Equal(expectedAmount, decimal.Parse(actualValue.Replace(',', '.'), CultureInfo.InvariantCulture));
            else Assert.Equal(value, actualValue);
        }
        AssertLiability(await Calculate(fresh, saved), expectedTax);
        await AssertNoMonthlyPromotion(second);
    }

    [Fact]
    public async Task Dependent_month_period_is_editable_without_erasing_independent_month_data_or_confirming_registration()
    {
        await using var host = await DemoHost.StartAsync();
        using var browser = new Browser(host.Address);
        var input = CompleteInputs();
        input["Exceptions[2].Dependents"] = "2";
        input["Exceptions[2].DependentsNote"] = "SYN_ANNUAL riêng tháng 3: hai người, giữ nguyên";
        var editor = await EnsureGroup(browser, await browser.Get("/Forecast"), "người phụ thuộc");
        var section = Section(editor.Html, "người phụ thuộc");
        var from = LabelField(section, "từ tháng|bắt đầu");
        var to = LabelField(section, "đến tháng|kết thúc");
        input[from] = MonthValue(section, from, 7);
        input[to] = MonthValue(section, to, 12);
        var count = OptionalLabelField(section, "số người|số lượng");
        if (count is not null) input[count] = "1";
        var identifier = OptionalLabelField(section, "tên|định danh|\\bmã\\b");
        if (identifier is not null) input[identifier] = "SYN_ANNUAL_CHILD";
        input[LabelField(section, "trạng thái")] = "Planned";
        input[LabelField(section, "nguồn")] = "SYN_ANNUAL_DEP chưa xác nhận đăng ký";
        input[LabelField(section, "ghi chú|lý do")] = "Dự kiến được tính từ tháng 7 đến tháng 12";
        // Remove explicitly entered monthly zeros for the period to supply, leaving Jan-Jun intact.
        for (var i = 6; i < 12; i++) input[$"Exceptions[{i}].Dependents"] = "";
        await Save(browser, editor, input);
        var reopened = await OpenSaved(browser, input);
        Assert.Equal("2", Fields(reopened.Html)["Exceptions[2].Dependents"]);
        Assert.Equal(input["Exceptions[2].DependentsNote"], Fields(reopened.Html)["Exceptions[2].DependentsNote"]);
        var result = await Calculate(browser, reopened);
        // Two dependent-months in March plus six from July-Dec = 49.6m deduction.
        // (360m - 186m - 49.6m) = 124.4m => 6m + 4.4m*10% = 6.44m.
        AssertLiability(result, 6_440_000m);
        AssertMetric(result, "(giảm trừ|khấu trừ).*người phụ thuộc|người phụ thuộc.*(giảm trừ|khấu trừ)", 49_600_000m);
        AssertCoverage(result, "nghĩa vụ|đủ dữ liệu|thực tế", 6);
        var afterCalculation = await OpenSaved(browser, input);
        Assert.Equal("Planned", Fields(afterCalculation.Html)[LabelField(section, "trạng thái")]);
        Assert.Equal(input[LabelField(section, "nguồn")], Fields(afterCalculation.Html)[LabelField(section, "nguồn")]);
    }

    [Theory]
    [InlineData("tiền âm", "-1")]
    [InlineData("dấu phân nhóm không hợp lệ", "1,234,567")]
    [InlineData("tràn số", "999999999999999999999999999999999999999")]
    public async Task Invalid_supplemental_money_retains_input_and_never_creates_result(string inputType, string value)
    {
        Assert.NotEmpty(inputType);
        await using var host = await DemoHost.StartAsync();
        using var browser = new Browser(host.Address);
        var input = CompleteInputs();
        await Save(browser, await browser.Get("/Forecast"), input);
        var page = await OpenSaved(browser, input);
        var before = await DatabaseImage(host);
        var failed = await Save(browser, page, new() { ["Exceptions[4].Insurance"] = value }, success: false);
        Assert.Equal(value, Fields(failed.Html)["Exceptions[4].Insurance"]);
        Assert.Matches("(?i)hợp lệ|không âm|kiểm tra|nhập số|lỗi", Text(failed.Html));
        Assert.Equal(before, await DatabaseImage(host));
    }

    [Fact]
    public async Task Unsaved_form_is_explicitly_rejected_or_saved_and_calculated_as_one_consistent_operation()
    {
        await using var host = await DemoHost.StartAsync();
        using var browser = new Browser(host.Address);
        var input = CompleteInputs();
        await Save(browser, await browser.Get("/Forecast"), input);
        var page = await OpenSaved(browser, input);
        var before = await DatabaseImage(host);
        var changed = new Dictionary<string, string> { ["Gross"] = "40000000", ["Periods[0].Gross"] = "40000000" };
        var response = await browser.Submit(page, CalculateLabel, changed, false);
        if (HasLiability(response, 23_400_000m))
        {
            // 480m - 186m = 294m => 6m + 174m*10% = 23.4m.
            input["Gross"] = input["Periods[0].Gross"] = "40000000";
            AssertInputs(changed, await OpenSaved(browser, input));
        }
        else
        {
            Assert.Equal(before, await DatabaseImage(host));
            Assert.Matches("(?i)(lưu.{0,100}(trước|tính)|chưa lưu|thay đổi.{0,80}lưu)", Output(response));
            AssertInputs(changed, response);
        }
    }

    [Fact]
    public async Task Each_calculation_appends_encrypted_history_and_catalog_failure_preserves_the_old_snapshot()
    {
        await using var host = await DemoHost.StartAsync();
        using var browser = new Browser(host.Address);
        var input = CompleteInputs();
        input["Source"] = "SYN_ANNUAL_SECRET_SNAPSHOT_SOURCE";
        await Save(browser, await browser.Get("/Forecast"), input);
        var opened = await OpenSaved(browser, input);
        var before = JsonSerializer.Deserialize<Dictionary<string, string[]>>(await DatabaseImage(host))!;
        AssertLiability(await Calculate(browser, opened), 11_400_000m);
        var first = JsonSerializer.Deserialize<Dictionary<string, string[]>>(await DatabaseImage(host))!;
        // Discover the separate result contract from observable database writes, without a
        // dependency on the new CLR service/entity/table names chosen by the implementer.
        var excluded = new[] { "AuditEvents", "ForecastDocuments", "ForecastSnapshots", "MonthlyIncomes", "CalculationHistory" };
        var contracts = first.Keys.Where(k => !excluded.Contains(k) && first[k].Length == before.GetValueOrDefault(k, []).Length + 1).ToArray();
        Assert.NotEmpty(contracts);
        AssertLiability(await Calculate(browser, await OpenSaved(browser, input)), 11_400_000m);
        var second = JsonSerializer.Deserialize<Dictionary<string, string[]>>(await DatabaseImage(host))!;
        Assert.Contains(contracts, k => second[k].Length == first[k].Length + 1 && first[k].All(second[k].Contains));
        foreach (var contract in contracts)
            foreach (var row in first[contract]) Assert.Contains(row, second[contract]);
        await AssertNoMonthlyPromotion(host);
        foreach (var file in Directory.EnumerateFiles(Path.GetDirectoryName(await DatabasePath(host))!))
            foreach (var encoding in new[] { Encoding.UTF8, Encoding.Unicode, Encoding.BigEndianUnicode })
                Assert.DoesNotContain(input["Source"], encoding.GetString(await File.ReadAllBytesAsync(file)));

        // Corrupt only the catalog, after both results exist. A cached/nearest rule must not
        // let the next calculation succeed; earlier encrypted snapshots must stay intact.
        await Sql(host, "DELETE FROM TaxRulePeriod WHERE PeriodFrom <= '2026-01-01' AND PeriodTo >= '2026-01-01'");
        var damaged = await DatabaseImage(host);
        var failed = await browser.Submit(await OpenSaved(browser, input), CalculateLabel, expectedSuccess: false);
        Assert.Matches("(?i)chưa.*tính|không.*tính|thiếu.*(luật|kỳ|quy tắc)|căn cứ.*(thiếu|chưa)|lỗi", Output(failed));
        Assert.Equal(damaged, await DatabaseImage(host));
        var final = JsonSerializer.Deserialize<Dictionary<string, string[]>>(damaged)!;
        foreach (var contract in contracts) Assert.Equal(second[contract], final[contract]);
    }

    [Fact]
    public async Task Annual_history_and_audit_roll_back_together_then_retry_can_succeed()
    {
        await using var host = await DemoHost.StartAsync();
        using var browser = new Browser(host.Address);
        var input = CompleteInputs();
        await Save(browser, await browser.Get("/Forecast"), input);
        var page = await OpenSaved(browser, input);
        Assert.True(HasAction(page, CalculateLabel), "Saved profile must advertise annual calculation.");
        var before = await DatabaseImage(host);
        await Sql(host, "CREATE TRIGGER AnnualProbeRejectAudit BEFORE INSERT ON AuditEvents BEGIN SELECT RAISE(ABORT, 'synthetic annual audit fault'); END");
        try
        {
            var failed = await browser.Submit(page, CalculateLabel, expectedSuccess: false);
            Assert.Matches("(?i)lỗi|không|chưa|thất bại", Output(failed));
            Assert.Equal(before, await DatabaseImage(host));
        }
        finally { await Sql(host, "DROP TRIGGER AnnualProbeRejectAudit"); }
        AssertLiability(await Calculate(browser, await OpenSaved(browser, input)), 11_400_000m);
    }

    [Theory]
    [InlineData("hồ sơ stale khi tính", false)]
    [InlineData("hồ sơ stale khi xóa", true)]
    public async Task Stale_revision_cannot_calculate_or_delete_a_newer_profile(string inputType, bool delete)
    {
        Assert.NotEmpty(inputType);
        await using var host = await DemoHost.StartAsync();
        using var a = new Browser(host.Address);
        using var b = new Browser(host.Address);
        var input = CompleteInputs();
        await Save(a, await a.Get("/Forecast"), input);
        var stale = await OpenSaved(b, input);
        if (delete) stale = await b.Act(stale, "Xóa", "tháng|giai đoạn|thưởng|người phụ thuộc");
        await Save(a, await OpenSaved(a, input), new() { ["Note"] = "SYN_ANNUAL_NEW_REVISION" });
        var before = await DatabaseImage(host);
        var failed = await b.Submit(stale, delete ? "Xác nhận.*xóa|Xóa.*xác nhận" : CalculateLabel, expectedSuccess: false);
        Assert.Matches("(?i)thay đổi|xung đột|phiên bản|mới hơn|mở lại", Output(failed));
        Assert.Equal(before, await DatabaseImage(host));
    }

    [Theory]
    [InlineData("khóa sai")]
    [InlineData("khóa thiếu")]
    [InlineData("sentinel lỗi")]
    [InlineData("sentinel thiếu")]
    public async Task Reopen_with_invalid_protection_fails_even_for_metadata_or_absent_profile(string inputType)
    {
        string root, existingUrl, missingUrl, pristine;
        var input = CompleteInputs();
        await using (var first = await DemoHost.StartAsync())
        {
            root = Storage(first).RootPath;
            using var browser = new Browser(first.Address);
            await Save(browser, await browser.Get("/Forecast"), input);
            var opened = await OpenSaved(browser, input);
            existingUrl = opened.Address.PathAndQuery;
            missingUrl = Regex.Replace(existingUrl, @"[0-9a-fA-F]{8}(?:-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12}", Guid.NewGuid().ToString());
            Assert.NotEqual(existingUrl, missingUrl);
            AssertLiability(await Calculate(browser, opened), 11_400_000m);
            pristine = await DatabaseImage(first);
            if (inputType == "sentinel lỗi")
                await Sql(first, "UPDATE DataProtectionMetadata SET KeyCheck = zeroblob(length(KeyCheck))");
            if (inputType == "sentinel thiếu") await Sql(first, "DELETE FROM DataProtectionMetadata");
        }
        var keyFile = Path.Combine(root, "synthetic-key.json");
        var original = await File.ReadAllBytesAsync(keyFile);
        try
        {
            if (inputType == "khóa sai")
                await File.WriteAllTextAsync(keyFile, "{\"activeKeyId\":\"synthetic\",\"keys\":{\"synthetic\":\"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=\"}}");
            if (inputType == "khóa thiếu") File.Move(keyFile, keyFile + ".annual-probe-missing");
            DemoHost? host = null;
            try { host = await DemoHost.StartAsync(syntheticRoot: root); }
            catch (Exception error) when (error is TaxManager.Data.Protection.DataProtectionException ||
                (error is InvalidOperationException && Regex.IsMatch(error.Message, "(?i)key|khóa|protec|sentinel")))
            {
                return; // Failing to open the protected store is also a valid public rejection.
            }
            await using (host)
            {
                using var client = new HttpClient { BaseAddress = host.Address };
                foreach (var url in new[] { "/Forecast", existingUrl, missingUrl })
                {
                    using var response = await client.GetAsync(url);
                    Assert.False(response.IsSuccessStatusCode, "Damaged protection must reject even metadata and empty reads: " + url);
                    Assert.DoesNotContain(input["Source"], await response.Content.ReadAsStringAsync());
                }
            }
        }
        finally
        {
            // Restore only synthetic test keys. Never repair a damaged sentinel.
            await File.WriteAllBytesAsync(keyFile, original);
        }
        if (inputType.StartsWith("khóa", StringComparison.Ordinal))
        {
            await using var healthy = await DemoHost.StartAsync(syntheticRoot: root);
            Assert.Equal(pristine, await DatabaseImage(healthy));
        }
    }

    private const string CalculateLabel = "(tính|tổng hợp).*(năm|nghĩa vụ)|tính lại";
    private sealed class ProbeClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 11, 20, 1, 2, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private static Dictionary<string, string> CompleteInputs()
    {
        var input = new Dictionary<string, string>
        {
            ["FromMonth"] = "1", ["ToMonth"] = "6", ["Gross"] = "30000000", ["Status"] = "Actual",
            ["Source"] = "SYN_ANNUAL_PROFILE_2026", ["Note"] = "Dữ liệu giả — hồ sơ năm 2026, một nơi trả lương",
            ["Periods[0].FromMonth"] = "7", ["Periods[0].ToMonth"] = "12", ["Periods[0].Gross"] = "30000000",
            ["Periods[0].Status"] = "Actual", ["Periods[0].Source"] = "SYN_ANNUAL_SECOND_PERIOD", ["Periods[0].Note"] = "Giữ giai đoạn riêng"
        };
        for (var i = 0; i < 12; i++)
        {
            input[$"Exceptions[{i}].Month"] = (i + 1).ToString(CultureInfo.InvariantCulture);
            input[$"Exceptions[{i}].Gross"] = "";
            foreach (var group in new[] { "Insurance", "Dependents", "Withheld" })
            {
                input[$"Exceptions[{i}].{group}"] = "0";
                input[$"Exceptions[{i}].{group}Status"] = "Actual";
                input[$"Exceptions[{i}].{group}Source"] = $"SYN_ANNUAL_{group}_{i + 1}";
                input[$"Exceptions[{i}].{group}Note"] = "Đã nhập zero tường minh từ chứng từ synthetic";
            }
        }
        return input;
    }

    private static async Task<Page> Calculate(Browser browser, Page page)
    {
        if (HasAction(page, CalculateLabel)) return await browser.Submit(page, CalculateLabel);
        foreach (Match link in Regex.Matches(page.Html, @"<a\b[^>]*>[\s\S]*?</a>", RegexOptions.IgnoreCase))
            if (Regex.IsMatch(Text(link.Value), CalculateLabel, RegexOptions.IgnoreCase))
            {
                var next = await browser.Get(new Uri(page.Address, Attr(link.Value, "href")).PathAndQuery);
                return HasAction(next, CalculateLabel) ? await browser.Submit(next, CalculateLabel) : next;
            }
        Assert.Fail("A saved 2026 profile has no advertised annual calculation action (stage 3 behavior is missing).");
        throw new InvalidOperationException();
    }

    private static bool HasAction(Page page, string label) => Forms(page.Html).Any(form => Regex.Matches(form,
        @"<button\b[^>]*>[\s\S]*?</button>", RegexOptions.IgnoreCase).Any(m => Regex.IsMatch(Text(m.Value), label, RegexOptions.IgnoreCase)));
    private static string Output(Page page)
    {
        var html = Regex.Replace(page.Html, @"<(script|style|nav|textarea|select|button)\b[^>]*>[\s\S]*?</\1>", " ", RegexOptions.IgnoreCase);
        return Text(Regex.Replace(html, @"<input\b[^>]*>", " ", RegexOptions.IgnoreCase));
    }
    private const string LiabilityLabel = "nghĩa vụ thuế|thuế (TNCN |thu nhập cá nhân )?năm|thuế năm ước tính|thuế phải nộp (cả )?năm";
    private static string Money(decimal value) => @"(?<!\d)" + string.Join(@"[.,\s]?", value.ToString("N0", CultureInfo.InvariantCulture).Split(',')) + @"(?!\d)";
    private static bool HasLiability(Page page, decimal expected) => Regex.IsMatch(Output(page),
        "(" + LiabilityLabel + ").{0,140}?" + Money(expected), RegexOptions.IgnoreCase);
    private static void AssertLiability(Page page, decimal expected) => Assert.True(HasLiability(page, expected),
        $"Expected annual obligation {expected.ToString(CultureInfo.InvariantCulture)} VND in rendered output: {Output(page)}");
    private static void AssertMetric(Page page, string label, decimal expected) => Assert.Matches(
        "(?i)(" + label + ").{0,140}?" + Money(expected), Output(page));
    private static void AssertCoverage(Page page, string label, int months) => Assert.Matches(
        "(?i)((" + label + ").{0,120}?\\b" + months + @"\s*/\s*12\b|\b" + months + @"\s*/\s*12\b.{0,100}?(" + label + "))", Output(page));
    private static void AssertNoNumericDifference(Page page) => Assert.DoesNotMatch(
        @"(?i)(nộp thêm|nộp thừa|chênh lệch)\s*[:—-]?\s*(?:\(?ước tính\)?\s*[:—-]?\s*)?-?\d[\d.,]*(?=\s|$)", Output(page));

    private static async Task AssertNoMonthlyPromotion(DemoHost host)
    {
        await using var context = Storage(host).Open();
        context.ValidateProtection();
        Assert.Empty(await context.MonthlyIncomes.ToListAsync());
        Assert.Empty(await context.CalculationHistory.ToListAsync());
    }
    private static async Task<string> DatabasePath(DemoHost host)
    {
        await using var context = Storage(host).Open();
        return Path.GetFullPath(context.Database.GetDbConnection().DataSource);
    }

    private static async Task<Page> EnsureGroup(Browser browser, Page page, string label)
    {
        if (FindSection(page.Html, label) is not null) return page;
        if (HasAction(page, "(thêm|nhập|mở).*?(" + label + ")"))
            return await browser.Submit(page, "(thêm|nhập|mở).*?(" + label + ")");
        Assert.Fail("No advertised editor for the required input type: " + label);
        throw new InvalidOperationException();
    }
    private static string Section(string html, string label) => FindSection(html, label)
        ?? throw new Xunit.Sdk.XunitException("The UI needs labelled input controls for " + label);
    private static string? FindSection(string html, string label)
    {
        // Balanced semantic containers allow nested fieldsets/details without assuming new names.
        var stack = new Stack<(string Tag, int Start)>();
        var sections = new List<string>();
        foreach (Match match in Regex.Matches(html, @"</?(fieldset|section|details)\b[^>]*>", RegexOptions.IgnoreCase))
        {
            if (!match.Value.StartsWith("</")) stack.Push((match.Groups[1].Value.ToLowerInvariant(), match.Index));
            else if (stack.TryPop(out var start) && start.Tag == match.Groups[1].Value.ToLowerInvariant())
                sections.Add(html[start.Start..(match.Index + match.Length)]);
        }
        return sections.OrderBy(s => s.Length).FirstOrDefault(s =>
        {
            var heading = Regex.Match(s, @"<(legend|summary|h[1-6])\b[^>]*>([\s\S]*?)</\1>", RegexOptions.IgnoreCase);
            if (!heading.Success || !Regex.IsMatch(Text(heading.Groups[2].Value), label, RegexOptions.IgnoreCase)) return false;
            // A nested metadata disclosure is not the editor for the amount/month range.
            return label.Contains("thưởng", StringComparison.OrdinalIgnoreCase)
                ? OptionalLabelField(s, "số tiền|tiền thưởng|thưởng.*đồng") is not null
                : OptionalLabelField(s, "từ tháng|bắt đầu") is not null && OptionalLabelField(s, "đến tháng|kết thúc") is not null;
        });
    }
    private static IEnumerable<string> Controls(string html) => Regex.Matches(html,
        @"<input\b[^>]*>|<textarea\b[^>]*>[\s\S]*?</textarea>|<select\b[^>]*>[\s\S]*?</select>", RegexOptions.IgnoreCase).Select(m => m.Value);
    private static string LabelField(string section, string label) => OptionalLabelField(section, label)
        ?? throw new Xunit.Sdk.XunitException("Required input has no labelled control matching: " + label);
    private static string MonthValue(string section, string name, int month)
    {
        var control = Controls(section).First(c => Attr(c, "name") == name);
        return Attr(control, "type") switch
        {
            "date" => $"2026-{month:00}-01", "month" => $"2026-{month:00}",
            _ => month.ToString(CultureInfo.InvariantCulture)
        };
    }
    private static string? OptionalLabelField(string section, string label)
    {
        foreach (Match item in Regex.Matches(section, @"<label\b[^>]*>[\s\S]*?</label>", RegexOptions.IgnoreCase))
        {
            if (!Regex.IsMatch(Text(item.Value), label, RegexOptions.IgnoreCase)) continue;
            var control = Controls(section).FirstOrDefault(c => Attr(c, "id") == Attr(item.Value, "for") && Attr(c, "name").Length > 0);
            if (control is not null) return Attr(control, "name");
        }
        return null;
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
        var displayed = Regex.Matches(Text(page.Html), @"\b\d{2}/\d{2}/\d{4}\s+\d{2}:\d{2}(?::\d{2})?\s+giờ Việt Nam\b")
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
