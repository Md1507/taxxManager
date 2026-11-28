using System.Collections.Concurrent;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaxManager.Web.Accounts;
using TaxManager.Web.Demo;
using Xunit;

namespace TaxManager.Web.Tests.Security;

public sealed class EmailRecoveryHttpTests
{
    private const string Password = "synthetic password 123";
    private const string NewPassword = "new synthetic password 456";
    private const string Login = "/tai-khoan/dang-nhap";
    private const string Manage = "/tai-khoan/email";
    private const string Forgot = "/tai-khoan/quen-mat-khau";
    private const string Address = "alice@example.invalid";

    [Fact]
    public async Task Disabled_recovery_has_no_entry_and_endpoints_are_unavailable()
    {
        await using var host = await DemoHost.StartAsync(accounts: true);
        using var client = host.AccountClient();
        Assert.DoesNotContain("Quên mật khẩu", Html(await client.GetStringAsync(Login)));
        foreach (var path in new[] { Forgot, "/tai-khoan/dat-lai-mat-khau", "/tai-khoan/xac-minh-email" })
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(path)).StatusCode);
    }

    [Theory]
    [InlineData("http://localhost:5088")]
    [InlineData("https://example.com")]
    [InlineData("https://localhost/redirect")]
    [InlineData("https://localhost?redirect=elsewhere")]
    public async Task Enabled_recovery_rejects_unsafe_origins(string origin)
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => DemoHost.StartAsync(accounts: true,
            extraArgs: Args(origin)));
    }

    [Fact]
    public async Task Email_add_requires_csrf_and_password_and_does_not_change_email_before_confirmation()
    {
        var sender = new Sender();
        await using var host = await Host(sender);
        var user = await Account(host);
        using var client = host.AccountClient();
        await SignIn(client);
        var csrf = Token(await client.GetStringAsync(Manage));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync(Manage, Form("", ("Email", Address), ("Password", Password)))).StatusCode);
        using var denied = await client.PostAsync(Manage, Form(csrf, ("Email", Address), ("Password", "wrong-secret")));
        Assert.Contains("Chưa xác nhận được mật khẩu", Html(await denied.Content.ReadAsStringAsync()));
        Assert.DoesNotContain("wrong-secret", await denied.Content.ReadAsStringAsync());
        Assert.Empty(sender.Messages);
        using var requested = await client.PostAsync(Manage, Form(csrf, ("Email", Address), ("Password", Password)));
        Assert.Equal(HttpStatusCode.Redirect, requested.StatusCode);
        var email = Assert.Single(sender.Messages);
        Assert.Equal(Address, email.To);
        Assert.StartsWith("https://localhost:5088/tai-khoan/xac-minh-email?", Link(email).AbsoluteUri);
        Assert.Null((await Read(host, user.Id)).Email);
        using var anonymous = host.AccountClient();
        await RequestReset(anonymous, Address);
        Assert.Single(sender.Messages); // unverified addresses cannot reset anything
        using var preview = await anonymous.GetAsync(Link(email).PathAndQuery);
        Assert.Contains("Xác minh email", Html(await preview.Content.ReadAsStringAsync()));
        Assert.Equal("no-referrer", preview.Headers.GetValues("Referrer-Policy").Single());
        Assert.True(preview.Headers.CacheControl!.NoStore);
        Assert.Null((await Read(host, user.Id)).Email); // link scanners cannot mutate
        Assert.Equal(HttpStatusCode.BadRequest, (await anonymous.PostAsync(Link(email).PathAndQuery, Form(""))).StatusCode);
        using var confirmed = await anonymous.PostAsync(Link(email).PathAndQuery, Form(Token(await preview.Content.ReadAsStringAsync())));
        Assert.Equal(HttpStatusCode.Redirect, confirmed.StatusCode);
        var after = await Read(host, user.Id);
        Assert.True(after.EmailConfirmed);
        Assert.Equal(Address, after.Email);
        Assert.Equal(user.StoreRoot, after.StoreRoot);
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/ho-so")).StatusCode);
        Assert.DoesNotContain("<form", await anonymous.GetStringAsync(Link(email).PathAndQuery));
    }

    [Fact]
    public async Task Valid_reset_changes_password_revokes_sessions_clears_lockout_and_is_single_use()
    {
        var sender = new Sender();
        await using var host = await Host(sender);
        var user = await Account(host, confirmed: true);
        using var signedIn = host.AccountClient();
        await SignIn(signedIn);
        await WithUser(host, user.Id, async (users, account) =>
        {
            Assert.True((await users.SetLockoutEndDateAsync(account, DateTimeOffset.UtcNow.AddMinutes(5))).Succeeded);
        });
        using var client = host.AccountClient();
        await RequestReset(client, Address);
        var link = Link(Assert.Single(sender.Messages)).PathAndQuery;
        var page = await client.GetStringAsync(link);
        Assert.Contains("id=\"Password\"", page);
        Assert.Equal(user.PasswordHash, (await Read(host, user.Id)).PasswordHash);
        using var reset = await client.PostAsync(link, Form(Token(page), ("Password", NewPassword), ("ConfirmPassword", NewPassword)));
        Assert.Equal(HttpStatusCode.Redirect, reset.StatusCode);
        Assert.Contains("reset=True", reset.Headers.Location!.OriginalString, StringComparison.OrdinalIgnoreCase);
        var after = await Read(host, user.Id);
        Assert.Null(after.LockoutEnd);
        Assert.NotEqual(user.SecurityStamp, after.SecurityStamp);
        Assert.Equal(user.StoreRoot, after.StoreRoot);
        Assert.Equal(HttpStatusCode.Redirect, (await signedIn.GetAsync("/ho-so")).StatusCode);
        Assert.DoesNotContain("id=\"Password\"", await client.GetStringAsync(link));
        using var replay = await client.PostAsync(link, Form(Token(page), ("Password", Password), ("ConfirmPassword", Password)));
        Assert.NotEqual(HttpStatusCode.Redirect, replay.StatusCode);
        await WithUser(host, user.Id, async (users, account) =>
        {
            Assert.False(await users.CheckPasswordAsync(account, Password));
            Assert.True(await users.CheckPasswordAsync(account, NewPassword));
        });
        await SignIn(client, NewPassword);
    }

    [Fact]
    public async Task Unknown_unverified_and_delivery_failure_have_same_public_response_and_no_token_leak()
    {
        var sender = new Sender { Fail = true };
        await using var host = await Host(sender);
        await Account(host, confirmed: true);
        await Account(host, "bob", email: "bob@example.invalid");
        using var client = host.AccountClient();
        foreach (var email in new[] { "missing@example.invalid", "bob@example.invalid", Address })
        {
            using var response = await RequestReset(client, email);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var html = Html(await response.Content.ReadAsStringAsync());
            Assert.Contains("Nếu email này đã được xác minh", html);
            Assert.DoesNotContain(email, html);
            Assert.DoesNotContain("requestId=", html);
            Assert.DoesNotContain("synthetic-api-key", html);
        }
        Assert.Empty(sender.Messages);
        using var scope = host.Services.CreateScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<LocalIdentityDb>().UserTokens.ToArrayAsync());
    }

    [Theory]
    [InlineData("short")]
    [InlineData("        ")]
    [InlineData("mismatch")]
    public async Task Invalid_password_is_rejected_without_consuming_reset_or_echoing_password(string password)
    {
        var sender = new Sender();
        await using var host = await Host(sender);
        var user = await Account(host, confirmed: true);
        using var client = host.AccountClient();
        await RequestReset(client, Address);
        var link = Link(Assert.Single(sender.Messages)).PathAndQuery;
        var page = await client.GetStringAsync(link);
        using var rejected = await client.PostAsync(link, Form(Token(page), ("Password", password), ("ConfirmPassword", password == "mismatch" ? "different" : password)));
        Assert.Equal(HttpStatusCode.OK, rejected.StatusCode);
        Assert.DoesNotContain("value=\"" + password + "\"", await rejected.Content.ReadAsStringAsync());
        Assert.Equal(user.PasswordHash, (await Read(host, user.Id)).PasswordHash);
        Assert.Contains("id=\"Password\"", await client.GetStringAsync(link));
    }

    [Fact]
    public async Task Expired_replaced_tampered_and_wrong_purpose_links_fail_closed()
    {
        var sender = new Sender();
        var clock = new Clock();
        await using var host = await Host(sender, clock);
        var user = await Account(host, confirmed: true);
        using var client = host.AccountClient();
        await RequestReset(client, Address);
        var first = Link(Assert.Single(sender.Messages));
        var query = QueryHelpers.ParseQuery(first.Query);
        using (var scope = host.Services.CreateScope())
        {
            var recovery = scope.ServiceProvider.GetRequiredService<EmailRecovery>();
            Assert.False(await recovery.CanConfirmAsync(user.Id, query["requestId"]!, query["code"]!));
            Assert.False(await recovery.CanResetAsync(user.Id, query["requestId"]!, "bad!"));
            Assert.False(await recovery.CanResetAsync(Guid.NewGuid().ToString(), query["requestId"]!, query["code"]!));
        }
        clock.Advance(TimeSpan.FromMinutes(2));
        await RequestReset(client, Address);
        Assert.Equal(2, sender.Messages.Count);
        Assert.DoesNotContain("id=\"Password\"", await client.GetStringAsync(first.PathAndQuery));
        var latest = Link(sender.Messages.Last()).PathAndQuery;
        Assert.Contains("id=\"Password\"", await client.GetStringAsync(latest));
        clock.Advance(TimeSpan.FromMinutes(31));
        Assert.DoesNotContain("id=\"Password\"", await client.GetStringAsync(latest));
    }

    [Fact]
    public async Task Email_change_retains_old_address_and_cancel_invalidates_link()
    {
        var sender = new Sender();
        await using var host = await Host(sender);
        var user = await Account(host, confirmed: true);
        using var client = host.AccountClient();
        await SignIn(client);
        using var request = await client.PostAsync(Manage, Form(Token(await client.GetStringAsync(Manage)), ("Email", "new@example.invalid"), ("Password", Password)));
        Assert.Equal(HttpStatusCode.Redirect, request.StatusCode);
        Assert.Equal(Address, (await Read(host, user.Id)).Email);
        var link = Link(Assert.Single(sender.Messages)).PathAndQuery;
        using var canceled = await client.PostAsync(Manage + "?handler=Cancel", Form(Token(await client.GetStringAsync(Manage))));
        Assert.Equal(HttpStatusCode.Redirect, canceled.StatusCode);
        using var anonymous = host.AccountClient();
        Assert.DoesNotContain("<form", await anonymous.GetStringAsync(link));
        Assert.Equal(Address, (await Read(host, user.Id)).Email);
    }

    [Fact]
    public async Task Already_verified_email_cannot_be_claimed_by_another_account()
    {
        var sender = new Sender();
        await using var host = await Host(sender);
        await Account(host, "bob", confirmed: true);
        var user = await Account(host);
        using var client = host.AccountClient();
        await SignIn(client);
        using var request = await client.PostAsync(Manage, Form(Token(await client.GetStringAsync(Manage)), ("Email", Address), ("Password", Password)));
        var link = Link(Assert.Single(sender.Messages)).PathAndQuery;
        using var anonymous = host.AccountClient();
        using var denied = await anonymous.PostAsync(link, Form(Token(await anonymous.GetStringAsync(link))));
        Assert.Equal(HttpStatusCode.OK, denied.StatusCode);
        Assert.False((await Read(host, user.Id)).EmailConfirmed);
        Assert.Null((await Read(host, user.Id)).Email);
    }

    [Fact]
    public async Task Removing_email_requires_password_and_invalidates_existing_reset_links()
    {
        var sender = new Sender();
        await using var host = await Host(sender);
        var user = await Account(host, confirmed: true);
        using var anonymous = host.AccountClient();
        await RequestReset(anonymous, Address);
        var link = Link(Assert.Single(sender.Messages)).PathAndQuery;
        using var client = host.AccountClient();
        await SignIn(client);
        var csrf = Token(await client.GetStringAsync(Manage));
        using var denied = await client.PostAsync(Manage + "?handler=Remove", Form(csrf));
        Assert.Equal(HttpStatusCode.OK, denied.StatusCode);
        Assert.Equal(Address, (await Read(host, user.Id)).Email);
        using var removed = await client.PostAsync(Manage + "?handler=Remove", Form(csrf, ("Password", Password)));
        Assert.Equal(HttpStatusCode.Redirect, removed.StatusCode);
        Assert.Null((await Read(host, user.Id)).Email);
        Assert.DoesNotContain("id=\"Password\"", await anonymous.GetStringAsync(link));
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/ho-so")).StatusCode);
    }

    [Fact]
    public async Task Concurrent_reuse_can_reset_password_only_once()
    {
        var sender = new Sender();
        await using var host = await Host(sender);
        var user = await Account(host, confirmed: true);
        using var first = host.AccountClient();
        using var second = host.AccountClient();
        await RequestReset(first, Address);
        var link = Link(Assert.Single(sender.Messages)).PathAndQuery;
        var firstToken = Token(await first.GetStringAsync(link));
        var secondToken = Token(await second.GetStringAsync(link));
        var results = await Task.WhenAll(first.PostAsync(link, Form(firstToken, ("Password", NewPassword), ("ConfirmPassword", NewPassword))),
            second.PostAsync(link, Form(secondToken, ("Password", Password), ("ConfirmPassword", Password))));
        Assert.Single(results, result => result.StatusCode == HttpStatusCode.Redirect);
        var after = await Read(host, user.Id);
        Assert.NotEqual(user.SecurityStamp, after.SecurityStamp);
        foreach (var result in results) result.Dispose();
    }

    [Fact]
    public async Task Concurrent_email_claims_allow_only_one_verified_owner()
    {
        var sender = new Sender();
        await using var host = await Host(sender);
        await Account(host, "alice");
        await Account(host, "bob");
        using var alice = host.AccountClient();
        using var bob = host.AccountClient();
        await SignIn(alice);
        using var bobLogin = await bob.PostAsync(Login, Form(Token(await bob.GetStringAsync(Login)), ("Username", "bob"), ("Password", Password)));
        Assert.Equal(HttpStatusCode.Redirect, bobLogin.StatusCode);
        foreach (var client in new[] { alice, bob })
        {
            using var request = await client.PostAsync(Manage, Form(Token(await client.GetStringAsync(Manage)), ("Email", Address), ("Password", Password)));
            Assert.Equal(HttpStatusCode.Redirect, request.StatusCode);
        }
        var links = sender.Messages.Select(message => Link(message).PathAndQuery).ToArray();
        Assert.Equal(2, links.Length);
        using var first = host.AccountClient();
        using var second = host.AccountClient();
        var firstToken = Token(await first.GetStringAsync(links[0]));
        var secondToken = Token(await second.GetStringAsync(links[1]));
        var results = await Task.WhenAll(first.PostAsync(links[0], Form(firstToken)), second.PostAsync(links[1], Form(secondToken)));
        Assert.Single(results, response => response.StatusCode == HttpStatusCode.Redirect);
        Assert.All(results, response => Assert.Contains(response.StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.Redirect }));
        using var scope = host.Services.CreateScope();
        Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<LocalIdentityDb>().Users.CountAsync(user => user.EmailConfirmed));
        foreach (var response in results) response.Dispose();
    }

    [Fact]
    public async Task Legacy_duplicate_verified_addresses_do_not_choose_an_arbitrary_account()
    {
        var sender = new Sender();
        await using var host = await Host(sender);
        await Account(host, "alice", confirmed: true);
        await Account(host, "bob", confirmed: true);
        using var client = host.AccountClient();
        using var response = await RequestReset(client, Address);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(sender.Messages);
    }

    [Fact]
    public async Task Pending_deletion_blocks_reset_and_confirmation_even_with_an_issued_link()
    {
        var sender = new Sender();
        await using var host = await Host(sender);
        var user = await Account(host, confirmed: true);
        using var client = host.AccountClient();
        await RequestReset(client, Address);
        var link = Link(Assert.Single(sender.Messages)).PathAndQuery;
        await WithUser(host, user.Id, async (users, account) =>
            Assert.True((await users.AddClaimAsync(account, new(AccountDeletion.ClaimType, "synthetic pending deletion"))).Succeeded));
        Assert.DoesNotContain("id=\"Password\"", await client.GetStringAsync(link));
        await RequestReset(client, Address);
        Assert.Single(sender.Messages);
        Assert.Equal(user.PasswordHash, (await Read(host, user.Id)).PasswordHash);
    }

    [Fact]
    public async Task Sender_failure_retains_confirmed_address_and_removes_pending_request()
    {
        var sender = new Sender { Fail = true };
        await using var host = await Host(sender);
        var user = await Account(host, confirmed: true);
        using var client = host.AccountClient();
        await SignIn(client);
        using var failed = await client.PostAsync(Manage, Form(Token(await client.GetStringAsync(Manage)), ("Email", "new@example.invalid"), ("Password", Password)));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
        Assert.Equal(Address, (await Read(host, user.Id)).Email);
        using var scope = host.Services.CreateScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<LocalIdentityDb>().UserTokens.ToArrayAsync());
    }

    [Fact]
    public async Task Reset_send_is_throttled_per_account_and_export_omits_recovery_secrets()
    {
        var sender = new Sender();
        await using var host = await Host(sender);
        await Account(host, confirmed: true);
        using var client = host.AccountClient();
        await RequestReset(client, Address);
        await RequestReset(client, Address.ToUpperInvariant());
        Assert.Single(sender.Messages);
        await SignIn(client);
        using var exported = await client.PostAsync("/tai-khoan/xuat-du-lieu", Form(Token(await client.GetStringAsync("/tai-khoan/xuat-du-lieu")), ("Password", Password)));
        var json = await exported.Content.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        Assert.Equal(Address, doc.RootElement.GetProperty("account").GetProperty("email").GetString());
        Assert.True(doc.RootElement.GetProperty("account").GetProperty("emailConfirmed").GetBoolean());
        Assert.DoesNotContain("ResetRequest", json);
        var query = QueryHelpers.ParseQuery(Link(sender.Messages.Single()).Query);
        Assert.DoesNotContain(query["requestId"].ToString(), json);
        Assert.DoesNotContain(query["code"].ToString(), json);
    }

    [Fact]
    public async Task Host_restart_retains_account_and_email_but_expires_old_ephemeral_recovery_links()
    {
        var sender = new Sender();
        string root;
        LocalUser user;
        string link;
        await using (var host = await Host(sender))
        {
            root = host.Services.GetRequiredService<SyntheticDatabase>().RootPath;
            user = await Account(host, confirmed: true);
            using var client = host.AccountClient();
            await RequestReset(client, Address);
            link = Link(sender.Messages.Single()).PathAndQuery;
        }
        await using var reopened = await DemoHost.StartAsync(accounts: true, syntheticRoot: root, extraArgs: Args(),
            configure: services => services.Replace(ServiceDescriptor.Singleton<IAccountEmailSender>(sender)));
        Assert.Equal(Address, (await Read(reopened, user.Id)).Email);
        Assert.Equal(user.StoreRoot, (await Read(reopened, user.Id)).StoreRoot);
        using var reader = reopened.AccountClient();
        Assert.DoesNotContain("id=\"Password\"", await reader.GetStringAsync(link));
    }

    private static string[] Args(string origin = "https://localhost:5088") => ["--EmailRecovery:Enabled=true", "--EmailRecovery:PublicOrigin=" + origin,
        "--EmailRecovery:Resend:ApiKey=synthetic-api-key", "--EmailRecovery:Resend:From=demo@example.invalid"];
    private static Task<DemoHost> Host(Sender sender, TimeProvider? clock = null) => DemoHost.StartAsync(accounts: true, clock: clock,
        extraArgs: Args(), configure: services => services.Replace(ServiceDescriptor.Singleton<IAccountEmailSender>(sender)));
    private static async Task<LocalUser> Account(DemoHost host, string name = "alice", bool confirmed = false, string? email = null)
    {
        using var store = new SyntheticDatabase(Directory.GetCurrentDirectory());
        using var scope = host.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<LocalUser>>();
        var user = new LocalUser { UserName = name, StoreRoot = store.RootPath, Email = email ?? (confirmed ? Address : null), EmailConfirmed = confirmed };
        Assert.True((await users.CreateAsync(user, Password)).Succeeded);
        Assert.True((await scope.ServiceProvider.GetRequiredService<LocalDataUse>().AcceptAsync(user)).Succeeded);
        return user;
    }
    private static async Task<LocalUser> Read(DemoHost host, string id)
    {
        using var scope = host.Services.CreateScope();
        return (await scope.ServiceProvider.GetRequiredService<UserManager<LocalUser>>().FindByIdAsync(id))!;
    }
    private static async Task WithUser(DemoHost host, string id, Func<UserManager<LocalUser>, LocalUser, Task> action)
    {
        using var scope = host.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<LocalUser>>();
        await action(users, (await users.FindByIdAsync(id))!);
    }
    private static async Task SignIn(HttpClient client, string password = Password)
    {
        using var response = await client.PostAsync(Login, Form(Token(await client.GetStringAsync(Login)), ("Username", "alice"), ("Password", password)));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }
    private static async Task<HttpResponseMessage> RequestReset(HttpClient client, string email) =>
        await client.PostAsync(Forgot, Form(Token(await client.GetStringAsync(Forgot)), ("Email", email)));
    private static Uri Link(AccountEmail email) => new(Regex.Match(email.Text, "https://[^\\s]+").Value);
    private static string Html(string html) => WebUtility.HtmlDecode(html);
    private static string Token(string html)
    {
        var token = Html(Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value);
        Assert.NotEmpty(token);
        return token;
    }
    private static FormUrlEncodedContent Form(string token, params (string Key, string Value)[] fields) =>
        new(fields.Select(field => new KeyValuePair<string, string>(field.Key, field.Value)).Append(new("__RequestVerificationToken", token)));
    private sealed class Sender : IAccountEmailSender
    {
        public ConcurrentQueue<AccountEmail> Messages { get; } = new();
        public bool Fail { get; set; }
        public Task SendAsync(AccountEmail email, CancellationToken token)
        {
            if (Fail) throw new AccountEmailDeliveryException();
            Messages.Enqueue(email);
            return Task.CompletedTask;
        }
    }
    private sealed class Clock : TimeProvider
    {
        private DateTimeOffset now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => now;
        public void Advance(TimeSpan time) => now += time;
    }
}
