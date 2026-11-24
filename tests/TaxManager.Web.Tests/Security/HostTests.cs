using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace TaxManager.Web.Tests.Security;

public sealed class HostTests
{
    [Fact]
    public async Task Remote_host_header_is_rejected_even_on_loopback()
    {
        await using var host = await DemoHost.StartAsync();
        using var client = new HttpClient { BaseAddress = host.Address };
        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Host = "attacker.example";
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Post_without_antiforgery_token_is_rejected()
    {
        await using var host = await DemoHost.StartAsync();
        using var client = new HttpClient { BaseAddress = host.Address };
        using var response = await client.PostAsync("/", new FormUrlEncodedContent(new Dictionary<string, string> { ["year"] = "2025" }));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void Production_or_missing_opt_in_cannot_start_demo()
    {
        Assert.Throws<InvalidOperationException>(() => DemoApplication.Build(["--SyntheticDemo=true"], "Production", 0));
        Assert.Throws<InvalidOperationException>(() => DemoApplication.Build([], "Development", 0));
    }

    [Theory]
    [InlineData("application/json")]
    [InlineData("text/plain")]
    public async Task Unsupported_content_type_with_valid_token_returns_vietnamese_error(string contentType)
    {
        await using var host = await DemoHost.StartAsync();
        using var client = new HttpClient(new HttpClientHandler { UseCookies = true }) { BaseAddress = host.Address };
        var html = await client.GetStringAsync("/");
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
        Assert.NotEmpty(token);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/") { Content = new StringContent("{\"year\":2025}", Encoding.UTF8, contentType) };
        request.Headers.Add("RequestVerificationToken", token);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        var message = await response.Content.ReadAsStringAsync();
        Assert.Contains("định dạng", message);
        Assert.DoesNotContain("Kết quả 12 tháng", message);
    }
}
