using Xunit;
using System.Globalization;

namespace TaxManager.Web.Tests.MonthlySalary;

public sealed class ValidationTests
{
    [Theory]
    [InlineData("-1")]
    [InlineData("abc")]
    [InlineData("1,000.1")]
    [InlineData("79228162514264337593543950336")]
    public async Task Invalid_amount_never_produces_results(string amount)
    {
        await using var host = await DemoHost.StartAsync();
        using var form = new FormClient(host.Address);
        var page = await form.PostAsync(gross: amount);
        Assert.Contains("Tháng 1: nhập số tiền không âm", page);
        Assert.DoesNotContain("<h2>Kết quả 12 tháng</h2>", page);
    }

    [Fact]
    public async Task Rejected_input_is_html_encoded_when_redisplayed()
    {
        await using var host = await DemoHost.StartAsync();
        using var form = new FormClient(host.Address);
        var html = await form.PostRawAsync(gross: "<script>alert(1)</script>");
        Assert.DoesNotContain("<script>alert(1)</script>", html);
        Assert.Contains("&lt;script&gt;", html);
    }

    [Theory]
    [InlineData(11)]
    [InlineData(13)]
    public async Task Missing_or_excess_months_are_rejected(int count)
    {
        await using var host = await DemoHost.StartAsync();
        using var form = new FormClient(host.Address);
        var page = await form.PostAsync(count: count);
        Assert.Contains("đủ chính xác 12 tháng", page);
        Assert.DoesNotContain("<h2>Kết quả 12 tháng</h2>", page);
    }

    [Fact]
    public async Task Unsupported_field_is_rejected_instead_of_ignored()
    {
        await using var host = await DemoHost.StartAsync();
        using var form = new FormClient(host.Address);
        var page = await form.PostAsync(extraKey: "bonus");
        Assert.Contains("trường nhập không được hỗ trợ", page);
        Assert.DoesNotContain("<h2>Kết quả 12 tháng</h2>", page);
    }

    [Theory]
    [InlineData("30000000,25")]
    [InlineData("30000000.25")]
    public async Task Decimal_input_is_preserved_without_float_rounding(string amount)
    {
        await using var host = await DemoHost.StartAsync();
        using var form = new FormClient(host.Address);
        var page = await form.PostAsync(gross: amount);
        Assert.Contains("30.000.000,25", page);
        Assert.Contains("2.150.000,05", page);
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("vi-VN")]
    public async Task Decimal_parsing_is_independent_of_server_culture(string culture)
    {
        var previous = CultureInfo.DefaultThreadCurrentCulture;
        try
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo(culture);
            await using var host = await DemoHost.StartAsync();
            using var form = new FormClient(host.Address);
            var page = await form.PostAsync(gross: "30000000,25");
            Assert.Contains("2.150.000,05", page);
        }
        finally { CultureInfo.DefaultThreadCurrentCulture = previous; }
    }
}
