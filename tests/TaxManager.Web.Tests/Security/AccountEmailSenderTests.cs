using System.Net;
using System.Text.Json;
using TaxManager.Web.Accounts;
using Xunit;

namespace TaxManager.Web.Tests.Security;

public sealed class AccountEmailSenderTests
{
    [Fact]
    public async Task Adapter_posts_only_expected_plain_text_to_fixed_https_endpoint()
    {
        var handler = new Handler(async request =>
        {
            Assert.Equal("https://api.resend.com/emails", request.RequestUri!.AbsoluteUri);
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
            Assert.Equal("synthetic-key", request.Headers.Authorization.Parameter);
            using var json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
            Assert.Equal("demo@example.invalid", json.RootElement.GetProperty("from").GetString());
            Assert.Equal("alice@example.invalid", json.RootElement.GetProperty("to")[0].GetString());
            Assert.Equal("Synthetic recovery", json.RootElement.GetProperty("subject").GetString());
            Assert.Equal("https://localhost/synthetic-token", json.RootElement.GetProperty("text").GetString());
            Assert.False(json.RootElement.TryGetProperty("html", out _));
            return new(HttpStatusCode.OK);
        });
        await Sender(handler).SendAsync(Email(), CancellationToken.None);
        Assert.Equal(1, handler.Calls);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.Redirect)]
    public async Task Upstream_error_or_redirect_returns_safe_exception_without_body(HttpStatusCode status)
    {
        var handler = new Handler(_ => Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent("synthetic-provider-secret"),
            Headers = { Location = new Uri("https://outside.invalid/") }
        }));
        var exception = await Assert.ThrowsAsync<AccountEmailDeliveryException>(() => Sender(handler).SendAsync(Email(), CancellationToken.None));
        Assert.Null(exception.InnerException);
        Assert.DoesNotContain("synthetic", exception.ToString());
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task Transport_error_does_not_expose_link_or_api_key_in_exception()
    {
        var handler = new Handler(_ => throw new HttpRequestException("synthetic-key https://localhost/synthetic-token"));
        var exception = await Assert.ThrowsAsync<AccountEmailDeliveryException>(() => Sender(handler).SendAsync(Email(), CancellationToken.None));
        Assert.Null(exception.InnerException);
        Assert.DoesNotContain("synthetic", exception.ToString());
    }

    private static AccountEmail Email() => new("alice@example.invalid", "Synthetic recovery", "https://localhost/synthetic-token");
    private static ResendAccountEmailSender Sender(Handler handler) => new(new Factory(new HttpClient(handler)), "demo@example.invalid", "synthetic-key");
    private sealed class Factory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) { Assert.Equal("account-email", name); return client; }
    }
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> action) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) { Calls++; return action(request); }
    }
}
