using Xunit;
using Microsoft.Extensions.DependencyInjection;
using System.Text.RegularExpressions;
using System.Net;

namespace TaxManager.Web.Tests.Forecasting;

public sealed class ForecastHttpTests
{
    private sealed class AdjustableClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; }
        public override DateTimeOffset GetUtcNow() => Now;
    }

    [Fact]
    public async Task Repeated_preview_keeps_entry_time_for_unchanged_values()
    {
        var clock = new AdjustableClock { Now = new DateTimeOffset(2026, 1, 2, 3, 4, 0, TimeSpan.Zero) };
        await using var host = await DemoHost.StartAsync(clock: clock);
        using var client = new HttpClient(new HttpClientHandler { UseCookies = true }) { BaseAddress = host.Address };
        var initial = await client.GetStringAsync("/Forecast");
        var token = Regex.Match(initial, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
        var values = new Dictionary<string, string> { ["__RequestVerificationToken"] = token, ["Gross"] = "30000000", ["Exceptions[0].Insurance"] = "0" };
        for (var i = 0; i < 12; i++) values[$"Exceptions[{i}].Month"] = (i + 1).ToString();
        using var first = await client.PostAsync("/Forecast?handler=Preview", new FormUrlEncodedContent(values));
        var firstHtml = WebUtility.HtmlDecode(await first.Content.ReadAsStringAsync());
        Assert.Contains("02/01/2026 10:04 giờ Việt Nam", firstHtml);
        var draft = Regex.Match(firstHtml, "name=\"DraftToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        Assert.NotEmpty(draft);
        values["DraftToken"] = draft;
        clock.Now = new DateTimeOffset(2026, 1, 2, 4, 5, 0, TimeSpan.Zero);
        using var second = await client.PostAsync("/Forecast?handler=Preview", new FormUrlEncodedContent(values));
        var secondHtml = WebUtility.HtmlDecode(await second.Content.ReadAsStringAsync());
        Assert.Contains("02/01/2026 10:04 giờ Việt Nam", secondHtml);
        Assert.DoesNotContain("02/01/2026 11:05 giờ Việt Nam", secondHtml);
        values["DraftToken"] = Regex.Match(secondHtml, "name=\"DraftToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        values["Exceptions[0].InsuranceNote"] = "ghi chú mới";
        using var third = await client.PostAsync("/Forecast?handler=Preview", new FormUrlEncodedContent(values));
        var thirdHtml = WebUtility.HtmlDecode(await third.Content.ReadAsStringAsync());
        Assert.Contains("02/01/2026 10:04 giờ Việt Nam", thirdHtml);
        Assert.Matches("Bảo hiểm tháng 1: 0 đ[^<]*<span>[^<]*ghi chú mới[^<]*02/01/2026 11:05 giờ Việt Nam", thirdHtml);
    }
    [Fact]
    public async Task Removing_earlier_period_preserves_remaining_entry_time()
    {
        var clock = new AdjustableClock { Now = new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero) };
        await using var host = await DemoHost.StartAsync(clock: clock);
        using var client = new HttpClient(new HttpClientHandler { UseCookies = true }) { BaseAddress = host.Address };
        var initial = await client.GetStringAsync("/Forecast");
        var token = Regex.Match(initial, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
        var values = new Dictionary<string, string> { ["__RequestVerificationToken"] = token,
            ["Periods[0].FromMonth"] = "3", ["Periods[0].ToMonth"] = "6", ["Periods[0].Gross"] = "100",
            ["Periods[1].FromMonth"] = "7", ["Periods[1].ToMonth"] = "12", ["Periods[1].Gross"] = "200" };
        for (var i = 0; i < 12; i++) values[$"Exceptions[{i}].Month"] = (i + 1).ToString();
        using var first = await client.PostAsync("/Forecast?handler=Preview", new FormUrlEncodedContent(values));
        var html = await first.Content.ReadAsStringAsync();
        values["DraftToken"] = Regex.Match(html, "name=\"DraftToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        clock.Now = clock.Now.AddDays(1);
        using var removed = await client.PostAsync("/Forecast?handler=RemovePeriod&index=0", new FormUrlEncodedContent(values));
        var removedHtml = await removed.Content.ReadAsStringAsync();
        values["DraftToken"] = Regex.Match(removedHtml, "name=\"DraftToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        foreach (var field in new[] { "FromMonth", "ToMonth", "Gross" }) { values[$"Periods[0].{field}"] = values[$"Periods[1].{field}"]; values.Remove($"Periods[1].{field}"); }
        using var second = await client.PostAsync("/Forecast?handler=Preview", new FormUrlEncodedContent(values));
        var result = WebUtility.HtmlDecode(await second.Content.ReadAsStringAsync());
        Assert.Matches("(?s)Tháng 7</th>.*?01/11/2026 07:00 giờ Việt Nam", result);
    }
    [Fact]
    public async Task New_host_reopens_synthetic_store_and_saved_form()
    {
        string root;
        using var clientHandler = new HttpClientHandler { UseCookies = true };
        await using (var first = await DemoHost.StartAsync())
        {
            root = first.Services.GetRequiredService<TaxManager.Web.Demo.SyntheticDatabase>().RootPath;
            using var client = new HttpClient(clientHandler, false) { BaseAddress = first.Address };
            var page = await client.GetStringAsync("/Forecast");
            var token = Regex.Match(page, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
            var values = new Dictionary<string, string> { ["__RequestVerificationToken"] = token, ["Gross"] = "12345.67", ["Source"] = "synthetic source" };
            for (var i = 0; i < 12; i++) values[$"Exceptions[{i}].Month"] = (i + 1).ToString();
            using var response = await client.PostAsync("/Forecast?handler=Save", new FormUrlEncodedContent(values));
            response.EnsureSuccessStatusCode();
            Assert.Contains("synthetic source", await response.Content.ReadAsStringAsync());
        }
        await using var second = await DemoHost.StartAsync(syntheticRoot: root);
        using var reopened = new HttpClient { BaseAddress = second.Address };
        var list = await reopened.GetStringAsync("/Forecast");
        var link = Regex.Match(list, "href=\"([^\"]*Forecast\\?id=[^\"]+)\"").Groups[1].Value;
        Assert.NotEmpty(link);
        var form = await reopened.GetStringAsync(WebUtility.HtmlDecode(link));
        Assert.Contains("12345.67", form);
        Assert.Contains("synthetic source", form);
    }
    [Fact]
    public async Task Empty_added_period_gross_has_specific_inline_error_and_preserves_inputs()
    {
        await using var host = await DemoHost.StartAsync();
        using var client = new HttpClient(new HttpClientHandler { UseCookies = true }) { BaseAddress = host.Address };
        var page = await client.GetStringAsync("/Forecast");
        var token = Regex.Match(page, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
        var values = new Dictionary<string, string> { ["__RequestVerificationToken"] = token, ["Gross"] = "30000000", ["Periods[0].FromMonth"] = "7", ["Periods[0].ToMonth"] = "12", ["Periods[0].Gross"] = "", ["Periods[0].Source"] = "Nguồn mẫu", ["Exceptions[2].Gross"] = "0" };
        for (var i = 0; i < 12; i++) values[$"Exceptions[{i}].Month"] = (i + 1).ToString();
        using var response = await client.PostAsync("/Forecast?handler=Preview", new FormUrlEncodedContent(values));
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Matches("data-valmsg-for=\"Periods\\[0\\].Gross\"[^>]*>[^<]*Giai đoạn thay đổi 1: hãy nhập lương gross", html);
        Assert.Contains("value=\"30000000\"", html);
        Assert.Contains("Nguồn mẫu", html);
        Assert.Contains("Tháng 3 đã có dữ liệu", html);
        Assert.Contains("autofocus=\"autofocus\"", html);
        Assert.DoesNotContain("<h2>Xem trước 12 tháng</h2>", html);
    }
    [Fact]
    public async Task Invalid_period_metadata_and_range_identify_fields_and_open_details()
    {
        await using var host = await DemoHost.StartAsync();
        using var client = new HttpClient(new HttpClientHandler { UseCookies = true }) { BaseAddress = host.Address };
        var page = await client.GetStringAsync("/Forecast");
        var token = Regex.Match(page, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
        var values = new Dictionary<string, string> { ["__RequestVerificationToken"] = token, ["Gross"] = "30000000", ["Status"] = "Assumed", ["Periods[0].Gross"] = "40000000", ["Periods[0].FromMonth"] = "12", ["Periods[0].ToMonth"] = "7", ["Periods[0].Status"] = "Assumed" };
        for (var i = 0; i < 12; i++) values[$"Exceptions[{i}].Month"] = (i + 1).ToString();
        using var response = await client.PostAsync("/Forecast?handler=Preview", new FormUrlEncodedContent(values));
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Matches("<details open=\"open\"><summary>Thông tin lương giai đoạn</summary>", html);
        Assert.Matches("<details open=\"open\"><summary>Thông tin lương giai đoạn thay đổi</summary>", html);
        Assert.Contains("data-valmsg-for=\"Note\"", html);
        Assert.Contains("data-valmsg-for=\"Periods[0].Note\"", html);
        Assert.Contains("data-valmsg-for=\"Periods[0].ToMonth\"", html);
        Assert.Contains("Giai đoạn thay đổi 1", html);
        Assert.Contains("value=\"40000000\"", html);
    }
    [Fact]
    public async Task Preview_allows_unknown_base_salary_and_shows_known_months()
    {
        await using var host = await DemoHost.StartAsync();
        using var client = new HttpClient(new HttpClientHandler { UseCookies = true }) { BaseAddress = host.Address };
        var page = await client.GetStringAsync("/Forecast");
        var token = Regex.Match(page, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
        var values = new Dictionary<string, string> { ["__RequestVerificationToken"] = token, ["Gross"] = "", ["Exceptions[0].Gross"] = "30000000", ["Exceptions[0].Insurance"] = "0" };
        for (var i = 0; i < 12; i++) values[$"Exceptions[{i}].Month"] = (i + 1).ToString();
        using var response = await client.PostAsync("/Forecast?handler=Preview", new FormUrlEncodedContent(values));
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains("30.000.000 đ", html);
        Assert.Contains("Bảo hiểm tháng 1: 0 đ", html);
        Assert.Contains("Bảo hiểm tháng 2: Chưa nhập", html);
        Assert.Contains("data-label=\"Lương\">Chưa nhập", html);
    }
    [Fact]
    public async Task Default_submit_previews_without_removing_a_period()
    {
        await using var host = await DemoHost.StartAsync();
        using var client = new HttpClient(new HttpClientHandler { UseCookies = true }) { BaseAddress = host.Address };
        var page = await client.GetStringAsync("/Forecast");
        var token = Regex.Match(page, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
        var values = new Dictionary<string, string> { ["__RequestVerificationToken"] = token, ["Gross"] = "30000000", ["ToMonth"] = "6", ["Periods[0].FromMonth"] = "7", ["Periods[0].ToMonth"] = "12", ["Periods[0].Gross"] = "40000000", ["Periods[0].Source"] = "synthetic raise", ["SelectedMonth"] = "1", ["EditingMonth"] = "1", ["Exceptions[0].Gross"] = "0" };
        for (var i = 0; i < 12; i++) values[$"Exceptions[{i}].Month"] = (i + 1).ToString();
        using var editor = await client.PostAsync("/Forecast?handler=SelectMonth", new FormUrlEncodedContent(values));
        var html = WebUtility.HtmlDecode(await editor.Content.ReadAsStringAsync());
        var form = Regex.Match(html, "<form\\b[\\s\\S]*?</form>").Value;
        var firstSubmit = Regex.Match(form, "<button\\b[^>]*type=\"submit\"[^>]*>[\\s\\S]*?</button>").Value;
        Assert.Contains("Xem trước", firstSubmit);
        Assert.DoesNotContain("formaction=", firstSubmit);
        using var preview = await client.PostAsync("/Forecast?handler=Preview", new FormUrlEncodedContent(values));
        var result = WebUtility.HtmlDecode(await preview.Content.ReadAsStringAsync());
        Assert.Contains("40.000.000 đ", result);
        Assert.Contains("synthetic raise", result);
        Assert.Contains("Tháng 1 là ngoại lệ và được giữ nguyên", result);
    }
    [Theory]
    [InlineData("Gross")]
    [InlineData("Periods[0].Gross")]
    [InlineData("Exceptions[0].Gross")]
    [InlineData("Exceptions[0].Insurance")]
    [InlineData("Exceptions[0].Withheld")]
    public async Task Invalid_amount_is_retained_and_never_previewed(string field)
    {
        await using var host = await DemoHost.StartAsync();
        using var client = new HttpClient(new HttpClientHandler { UseCookies = true }) { BaseAddress = host.Address };
        var page = await client.GetStringAsync("/Forecast");
        var token = Regex.Match(page, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
        var values = new Dictionary<string, string> { ["__RequestVerificationToken"] = token, ["Gross"] = "30000000", ["Periods[0].Gross"] = "40000000", [field] = "1,234,567" };
        for (var i = 0; i < 12; i++) values[$"Exceptions[{i}].Month"] = (i + 1).ToString();
        using var response = await client.PostAsync("/Forecast?handler=Preview", new FormUrlEncodedContent(values));
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains("value=\"1,234,567\"", html);
        Assert.Contains("Nhập số tiền không âm", html);
        Assert.DoesNotContain("<h2>Xem trước 12 tháng</h2>", html);
    }
    [Fact]
    public async Task Invalid_hidden_month_opens_editor_with_field_error_and_retains_input()
    {
        await using var host = await DemoHost.StartAsync();
        using var client = new HttpClient(new HttpClientHandler { UseCookies = true }) { BaseAddress = host.Address };
        var page = await client.GetStringAsync("/Forecast");
        var token = Regex.Match(page, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
        var values = new Dictionary<string, string> { ["__RequestVerificationToken"] = token, ["Gross"] = "30000000", ["Exceptions[4].Insurance"] = "1,2,3" };
        for (var i = 0; i < 12; i++) values[$"Exceptions[{i}].Month"] = (i + 1).ToString();
        using var response = await client.PostAsync("/Forecast?handler=Preview", new FormUrlEncodedContent(values));
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains("Chỉnh tháng 5", html);
        Assert.Contains("Tháng 5 — Bảo hiểm", html);
        Assert.Contains("value=\"1,2,3\"", html);
        Assert.Contains("data-valmsg-for=\"Exceptions[4].Insurance\"", html);
    }
    [Fact]
    public async Task Remove_period_preserves_other_inputs()
    {
        await using var host = await DemoHost.StartAsync();
        using var client = new HttpClient(new HttpClientHandler { UseCookies = true }) { BaseAddress = host.Address };
        var page = await client.GetStringAsync("/Forecast");
        var token = Regex.Match(page, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
        var values = new Dictionary<string, string> { ["__RequestVerificationToken"] = token, ["Gross"] = "30000000", ["Periods[0].Gross"] = "111", ["Periods[1].Gross"] = "222", ["Exceptions[2].Gross"] = "0", ["index"] = "0" };
        for (var i = 0; i < 12; i++) values[$"Exceptions[{i}].Month"] = (i + 1).ToString();
        using var response = await client.PostAsync("/Forecast?handler=RemovePeriod", new FormUrlEncodedContent(values));
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains("value=\"222\"", html);
        Assert.DoesNotContain("value=\"111\"", html);
        Assert.Contains("value=\"30000000\"", html);
        Assert.Contains("Tháng 3 đã có dữ liệu", html);
    }
    [Fact]
    public async Task Preview_parses_fractional_amounts_consistently_across_fields()
    {
        await using var host = await DemoHost.StartAsync();
        using var client = new HttpClient(new HttpClientHandler { UseCookies = true }) { BaseAddress = host.Address };
        var page = await client.GetStringAsync("/Forecast");
        var token = Regex.Match(page, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
        var values = new Dictionary<string, string> { ["__RequestVerificationToken"] = token, ["Gross"] = "30000000,50", ["FromMonth"] = "3", ["Periods[0].FromMonth"] = "2", ["Periods[0].ToMonth"] = "2", ["Periods[0].Gross"] = "40000000,25", ["Exceptions[0].Gross"] = "10000000,75", ["Exceptions[0].Insurance"] = "1000000,50", ["Exceptions[0].Withheld"] = "2000000,50" };
        for (var i = 0; i < 12; i++) values[$"Exceptions[{i}].Month"] = (i + 1).ToString();
        using var response = await client.PostAsync("/Forecast?handler=Preview", new FormUrlEncodedContent(values));
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains("30.000.000,50 đ", html);
        Assert.Contains("40.000.000,25 đ", html);
        Assert.Contains("10.000.000,75 đ", html);
        Assert.Contains("Bảo hiểm tháng 1: 1.000.000,50 đ", html);
        Assert.Contains("Khấu trừ tháng 1: 2.000.000,50 đ", html);
    }

    [Fact]
    public async Task Clear_month_requires_confirmation_and_preserves_other_month()
    {
        await using var host = await DemoHost.StartAsync();
        using var client = new HttpClient(new HttpClientHandler { UseCookies = true }) { BaseAddress = host.Address };
        var page = await client.GetStringAsync("/Forecast");
        var token = Regex.Match(page, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
        var values = new Dictionary<string, string> { ["__RequestVerificationToken"] = token, ["SelectedMonth"] = "7", ["EditingMonth"] = "3", ["Gross"] = "30000000", ["Exceptions[2].Gross"] = "0", ["Exceptions[2].Source"] = "Xóa tháng ba", ["Exceptions[6].Gross"] = "123", ["Exceptions[6].Source"] = "Giữ tháng bảy" };
        for (var i = 0; i < 12; i++) values[$"Exceptions[{i}].Month"] = (i + 1).ToString();
        using var confirmation = await client.PostAsync("/Forecast?handler=ConfirmClearMonth", new FormUrlEncodedContent(values));
        var confirmHtml = WebUtility.HtmlDecode(await confirmation.Content.ReadAsStringAsync());
        Assert.Contains("Xác nhận xóa tháng 3", confirmHtml);
        Assert.Contains("Xóa tháng ba", confirmHtml);
        using var response = await client.PostAsync("/Forecast?handler=ClearMonth", new FormUrlEncodedContent(values));
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.DoesNotContain("Xóa tháng ba", html);
        Assert.Contains("Giữ tháng bảy", html);
        Assert.Contains("Tháng 7 đã có dữ liệu", html);
    }

    [Fact]
    public async Task Month_selector_preserves_other_months_and_invalid_values()
    {
        await using var host = await DemoHost.StartAsync();
        using var client = new HttpClient(new HttpClientHandler { UseCookies = true }) { BaseAddress = host.Address };
        var page = await client.GetStringAsync("/Forecast");
        var token = Regex.Match(page, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
        Assert.Contains("Chọn tháng", WebUtility.HtmlDecode(page));
        var values = new Dictionary<string, string> { ["__RequestVerificationToken"] = token, ["SelectedMonth"] = "5", ["Gross"] = "30000000", ["Exceptions[2].Gross"] = "0", ["Exceptions[2].Source"] = "Phiếu mẫu", ["Exceptions[4].Insurance"] = "sai" };
        for (var i = 0; i < 12; i++) values[$"Exceptions[{i}].Month"] = (i + 1).ToString();
        using var response = await client.PostAsync("/Forecast?handler=SelectMonth", new FormUrlEncodedContent(values));
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains("Chỉnh tháng 5", html);
        Assert.Contains("Phiếu mẫu", html);
        Assert.Contains("sai", html);
        Assert.Contains("Tháng 3 đã có dữ liệu", html);
        Assert.DoesNotContain("<legend>Tháng 1</legend>", html);
    }

    [Fact]
    public async Task Preview_identifies_salary_columns_for_narrow_screens()
    {
        await using var host = await DemoHost.StartAsync();
        using var client = new HttpClient(new HttpClientHandler { UseCookies = true }) { BaseAddress = host.Address };
        var page = await client.GetStringAsync("/Forecast");
        var token = Regex.Match(page, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
        var values = new Dictionary<string, string> { ["__RequestVerificationToken"] = token, ["FromMonth"] = "1", ["ToMonth"] = "12", ["Gross"] = "20000000" };
        for (var i = 0; i < 12; i++) values[$"Exceptions[{i}].Month"] = (i + 1).ToString();
        using var response = await client.PostAsync("/Forecast?handler=Preview", new FormUrlEncodedContent(values));
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        foreach (var label in new[] { "Lương gross", "Bảo hiểm", "Người phụ thuộc", "Khấu trừ" })
            Assert.Contains(label, html);
        Assert.Contains("aria-label=\"Dữ liệu 12 tháng\"", html);
        Assert.Contains("badge planned", html);
        Assert.Contains("Nguồn và ghi chú tháng 1", html);
    }

    [Fact]
    public async Task Preview_distinguishes_missing_and_zero_dependents_and_withholding()
    {
        await using var host = await DemoHost.StartAsync();
        using var client = new HttpClient(new HttpClientHandler { UseCookies = true }) { BaseAddress = host.Address };
        var page = await client.GetStringAsync("/Forecast");
        var token = Regex.Match(page, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
        var values = new Dictionary<string, string> { ["__RequestVerificationToken"] = token,
            ["FromMonth"] = "1", ["ToMonth"] = "12", ["Gross"] = "20000000", ["Status"] = "Planned",
            ["Exceptions[0].Dependents"] = "0", ["Exceptions[0].DependentsStatus"] = "Actual",
            ["Exceptions[0].Withheld"] = "0", ["Exceptions[0].WithheldStatus"] = "Actual",
            ["Exceptions[0].WithheldSource"] = "Phiếu lương" };
        for (var i = 0; i < 12; i++) values[$"Exceptions[{i}].Month"] = (i + 1).ToString();
        using var response = await client.PostAsync("/Forecast?handler=Preview", new FormUrlEncodedContent(values));
        response.EnsureSuccessStatusCode();
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains("Người phụ thuộc tháng 1: 0", html);
        Assert.Contains("Người phụ thuộc tháng 2: Chưa nhập", html);
        Assert.Contains("Khấu trừ tháng 1: 0 đ", html);
        Assert.Contains("Khấu trừ tháng 2: Chưa nhập", html);
        Assert.Contains("Nguồn: Phiếu lương", html);
    }

    [Fact]
    public async Task Preview_distinguishes_missing_and_zero_insurance_for_each_month()
    {
        await using var host = await DemoHost.StartAsync();
        using var client = new HttpClient(new HttpClientHandler { UseCookies = true }) { BaseAddress = host.Address };
        var page = await client.GetStringAsync("/Forecast");
        var token = Regex.Match(page, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
        var values = new Dictionary<string, string> { ["__RequestVerificationToken"] = token,
            ["FromMonth"] = "1", ["ToMonth"] = "12", ["Gross"] = "20000000", ["Status"] = "Planned",
            ["Exceptions[0].Insurance"] = "0", ["Exceptions[0].InsuranceStatus"] = "Actual",
            ["Exceptions[0].InsuranceSource"] = "Phiếu bảo hiểm" };
        for (var i = 0; i < 12; i++) values[$"Exceptions[{i}].Month"] = (i + 1).ToString();
        using var response = await client.PostAsync("/Forecast?handler=Preview", new FormUrlEncodedContent(values));
        response.EnsureSuccessStatusCode();
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains("Bảo hiểm tháng 1: 0 đ", html);
        Assert.Contains("Nguồn: Phiếu bảo hiểm", html);
        Assert.Contains("Bảo hiểm tháng 2: Chưa nhập", html);
    }

    [Fact]
    public async Task Overlapping_periods_require_correction_before_preview_or_save()
    {
        await using var host = await DemoHost.StartAsync();
        using var client = new HttpClient(new HttpClientHandler { UseCookies = true }) { BaseAddress = host.Address };
        var page = await client.GetStringAsync("/Forecast");
        var token = Regex.Match(page, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
        var values = new Dictionary<string, string> { ["__RequestVerificationToken"] = token,
            ["FromMonth"] = "1", ["ToMonth"] = "12", ["Gross"] = "20000000", ["Status"] = "Planned",
            ["Periods[0].FromMonth"] = "7", ["Periods[0].ToMonth"] = "8", ["Periods[0].Gross"] = "30000000", ["Periods[0].Status"] = "Planned" };
        for (var i = 0; i < 12; i++) values[$"Exceptions[{i}].Month"] = (i + 1).ToString();
        using var response = await client.PostAsync("/Forecast?handler=Preview", new FormUrlEncodedContent(values));
        response.EnsureSuccessStatusCode();
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains("Hai giai đoạn cùng áp dụng cho tháng 7, 8", html);
        Assert.Contains("href=\"#Periods_0__FromMonth\"", html);
        Assert.Contains("value=\"30000000\"", html);
        Assert.DoesNotContain("id=\"bang-thang\"", html);
        using var save = await client.PostAsync("/Forecast?handler=Save", new FormUrlEncodedContent(values));
        Assert.Contains("Sửa khoảng tháng trước khi lưu", WebUtility.HtmlDecode(await save.Content.ReadAsStringAsync()));
        await using var context = host.Services.GetRequiredService<TaxManager.Web.Demo.SyntheticDatabase>().Open();
        Assert.Empty(await new TaxManager.Data.Forecasting.ForecastStore(context).ListAsync());
    }

    [Fact]
    public async Task Adding_period_keeps_unparsed_form_input_for_correction()
    {
        await using var host = await DemoHost.StartAsync();
        using var client = new HttpClient(new HttpClientHandler { UseCookies = true }) { BaseAddress = host.Address };
        var page = await client.GetStringAsync("/Forecast");
        var token = Regex.Match(page, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
        var values = new Dictionary<string, string> { ["__RequestVerificationToken"] = token,
            ["FromMonth"] = "1", ["ToMonth"] = "12", ["Gross"] = "123x", ["Status"] = "Planned" };
        for (var i = 0; i < 12; i++) values[$"Exceptions[{i}].Month"] = (i + 1).ToString();
        using var response = await client.PostAsync("/Forecast?handler=AddPeriod", new FormUrlEncodedContent(values));
        response.EnsureSuccessStatusCode();
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains("123x", html);
        Assert.Contains("Giai đoạn thay đổi 1", html);
    }

    [Fact]
    public async Task Preview_accepts_a_salary_change_during_the_year()
    {
        await using var host = await DemoHost.StartAsync();
        using var client = new HttpClient(new HttpClientHandler { UseCookies = true }) { BaseAddress = host.Address };
        var page = await client.GetStringAsync("/Forecast");
        var token = Regex.Match(page, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
        var values = new Dictionary<string, string> { ["__RequestVerificationToken"] = token,
            ["FromMonth"] = "1", ["ToMonth"] = "6", ["Gross"] = "20000000", ["Status"] = "Planned",
            ["Periods[0].FromMonth"] = "7", ["Periods[0].ToMonth"] = "12", ["Periods[0].Gross"] = "30000000", ["Periods[0].Status"] = "Planned" };
        for (var i = 0; i < 12; i++) values[$"Exceptions[{i}].Month"] = (i + 1).ToString();
        using var response = await client.PostAsync("/Forecast?handler=Preview", new FormUrlEncodedContent(values));
        response.EnsureSuccessStatusCode();
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains("20.000.000 đ", html);
        Assert.Contains("30.000.000 đ", html);
        var salaryTable = html[html.IndexOf("<table class=\"forecast-preview\">", StringComparison.Ordinal)..html.IndexOf("</table>", StringComparison.Ordinal)];
        Assert.DoesNotContain("Chưa nhập", string.Join("", Regex.Matches(salaryTable, "data-label=\"Lương\">([^<]+)").Select(m => m.Groups[1].Value)));
    }

    [Fact]
    public async Task Invalid_period_keeps_entered_values()
    {
        await using var host = await DemoHost.StartAsync();
        using var client = new HttpClient(new HttpClientHandler { UseCookies = true }) { BaseAddress = host.Address };
        var page = await client.GetStringAsync("/Forecast");
        var token = Regex.Match(page, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
        var values = new Dictionary<string, string> { ["__RequestVerificationToken"] = token,
            ["FromMonth"] = "9", ["ToMonth"] = "2", ["Gross"] = "1234567.25", ["Status"] = "Planned", ["Source"] = "Nguồn mẫu" };
        using var response = await client.PostAsync("/Forecast?handler=Preview", new FormUrlEncodedContent(values));
        response.EnsureSuccessStatusCode();
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains("Hãy nhập tháng hợp lệ", html);
        Assert.Contains("1234567.25", html);
        Assert.Contains("Nguồn mẫu", html);
    }

    [Fact]
    public async Task Preview_period_preserves_explicit_month_exception()
    {
        await using var host = await DemoHost.StartAsync();
        using var client = new HttpClient(new HttpClientHandler { UseCookies = true }) { BaseAddress = host.Address };
        var page = await client.GetStringAsync("/Forecast");
        var token = Regex.Match(page, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
        Assert.NotEmpty(token);
        var values = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token, ["FromMonth"] = "1", ["ToMonth"] = "12",
            ["Gross"] = "30000000", ["Status"] = "Planned", ["Source"] = "Kế hoạch",
            ["Exceptions[2].Month"] = "3", ["Exceptions[2].Gross"] = "0",
            ["Exceptions[2].Status"] = "Actual", ["Exceptions[2].Source"] = "Phiếu lương"
        };
        for (var i = 0; i < 12; i++) if (i != 2) values[$"Exceptions[{i}].Month"] = (i + 1).ToString();
        using var response = await client.PostAsync("/Forecast?handler=Preview", new FormUrlEncodedContent(values));
        response.EnsureSuccessStatusCode();
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains("Tháng 3 là ngoại lệ", html);
        Assert.Contains("30.000.000 đ", html);
        Assert.Contains("0 đ", html);
        Assert.Contains("Thực tế", html);
        Assert.Contains("Phiếu lương", html);
        Assert.Contains("Ngày nhập", html);
    }

    [Fact]
    public async Task Forecast_page_offers_period_entry_and_preview()
    {
        await using var host = await DemoHost.StartAsync();
        using var client = new HttpClient { BaseAddress = host.Address };
        var html = await client.GetStringAsync("/Forecast");
        Assert.Contains("Hồ sơ thuế năm 2026", html);
        Assert.Contains("Từ tháng", html);
        Assert.Contains("Đến tháng", html);
        Assert.Contains("Xem trước", html);
    }

    [Fact]
    public async Task Existing_home_page_links_to_forecast()
    {
        await using var host = await DemoHost.StartAsync();
        using var client = new HttpClient { BaseAddress = host.Address };
        var html = await client.GetStringAsync("/");
        Assert.Contains("href=\"/Forecast\"", html);
    }
}
