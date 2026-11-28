using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaxManager.Web.Accounts;
using TaxManager.Web.Demo;
using Xunit;

namespace TaxManager.Web.Tests.Security;

// Exercise the framework OAuth handler, state/correlation and real HTTPS cookies.
// Only the provider's token/userinfo network responses are synthetic.
public sealed class GoogleSignInHttpTests
{
    private const string Password = "synthetic google password 123";
    private const string Login = "/tai-khoan/dang-nhap";
    private const string Manage = "/tai-khoan/google";
    private const string Entry = "/tai-khoan/google/dang-nhap";
    private const string Result = "/tai-khoan/google/ket-qua";

    [Fact]
    public async Task Disabled_provider_has_no_button_and_enabled_provider_requires_configuration()
    {
        await using var host = await DemoHost.StartAsync(accounts: true);
        using var client = host.AccountClient();
        Assert.DoesNotContain("Đăng nhập bằng Google", Html(await client.GetStringAsync(Login)));
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(Entry)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(Result)).StatusCode);
        await Assert.ThrowsAsync<InvalidOperationException>(() => DemoHost.StartAsync(accounts: true,
            extraArgs: ["--Authentication:Google:Enabled=true"]));
    }

    [Fact]
    public async Task Challenge_requires_post_and_csrf_and_requests_only_openid_with_pkce()
    {
        await using var host = await Host();
        using var client = host.AccountClient();
        Assert.Contains("Đăng nhập bằng Google", Html(await client.GetStringAsync(Login)));
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(Entry)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync(Entry, Form(""))).StatusCode);
        using var start = await Start(client, false);
        var query = QueryHelpers.ParseQuery(start.Headers.Location!.Query);
        Assert.Equal("accounts.google.com", start.Headers.Location.Host);
        Assert.Equal("openid", query["scope"]);
        Assert.Equal("S256", query["code_challenge_method"]);
        Assert.False(string.IsNullOrEmpty(query["code_challenge"]));
        Assert.Equal("select_account", query["prompt"]);
        Assert.Equal(host.Address.GetLeftPart(UriPartial.Authority) + "/signin-google", query["redirect_uri"]);
        Assert.DoesNotContain("synthetic-client-secret", start.Headers.Location.OriginalString);
        Assert.True(start.Headers.CacheControl!.NoStore);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Callback_without_valid_state_or_correlation_fails_without_creating_accounts(bool validState)
    {
        await using var host = await Host();
        using var initiator = host.AccountClient();
        using var start = await Start(initiator, false);
        using var stranger = host.AccountClient();
        var state = validState ? QueryHelpers.ParseQuery(start.Headers.Location!.Query)["state"].ToString() : "invalid";
        using var callback = await stranger.GetAsync("/signin-google?code=synthetic&state=" + Uri.EscapeDataString(state));
        Assert.Equal(Result + "?failed=true", callback.Headers.Location!.OriginalString);
        var page = Html(await stranger.GetStringAsync(callback.Headers.Location));
        Assert.Contains("Chưa hoàn tất", page);
        Assert.DoesNotContain("synthetic-access-token", page);
        using var scope = host.Services.CreateScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<LocalIdentityDb>().Users.ToArrayAsync());
    }

    [Fact]
    public async Task Unlinked_google_never_creates_or_matches_an_account_by_email()
    {
        await using var host = await Host();
        await Account(host, "alice");
        using var client = host.AccountClient();
        using var start = await Start(client, false);
        using var callback = await Callback(client, start);
        var html = Html(await client.GetStringAsync(callback.Headers.Location));
        Assert.Contains("Bắt đầu với Google", html);
        Assert.DoesNotContain("type=\"password\"", html);
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/ho-so")).StatusCode);
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LocalIdentityDb>();
        Assert.Single(await db.Users.ToArrayAsync());
        Assert.Empty(await db.UserLogins.ToArrayAsync());
        Assert.Empty(await db.UserTokens.ToArrayAsync());
    }

    [Fact]
    public async Task Link_needs_google_and_current_session_without_password_and_reopens_same_account_after_host_restart()
    {
        string root;
        LocalUser original;
        await using (var host = await Host())
        {
            root = host.Services.GetRequiredService<SyntheticDatabase>().RootPath;
            original = await Account(host, "alice");
            using var client = host.AccountClient();
            await PasswordLogin(client);
            using var start = await Start(client, true);
            using var callback = await Callback(client, start);
            Assert.True(callback.Headers.CacheControl!.NoStore);
            Assert.Contains("Tiếp tục xác nhận", Html(await client.GetStringAsync(callback.Headers.Location)));
            var ready = await client.GetStringAsync(Manage);
            Assert.DoesNotContain("type=\"password\"", ready);
            var token = Token(ready);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync(Manage + "?handler=Complete", Form(""))).StatusCode);
            Assert.Empty(await Logins(host, original.Id));
            using var linked = await client.PostAsync(Manage + "?handler=Complete", Form(token));
            Assert.Equal(HttpStatusCode.Redirect, linked.StatusCode);
            var login = Assert.Single(await Logins(host, original.Id));
            Assert.Equal("google-subject-a", login.ProviderKey);
            using var export = await client.PostAsync("/tai-khoan/xuat-du-lieu", Form(Token(await client.GetStringAsync("/tai-khoan/xuat-du-lieu")), ("Password", Password)));
            using var document = JsonDocument.Parse(await export.Content.ReadAsStringAsync());
            Assert.Equal("google-subject-a", document.RootElement.GetProperty("account").GetProperty("linkedLogins")[0].GetProperty("providerKey").GetString());
            using var scope = host.Services.CreateScope();
            Assert.Empty(await scope.ServiceProvider.GetRequiredService<LocalIdentityDb>().UserTokens.ToArrayAsync());
        }
        await using var reopened = await Host(root);
        using var reader = reopened.AccountClient();
        using var loginStart = await Start(reader, false, "https://outside.invalid/");
        using var loginCallback = await Callback(reader, loginStart);
        using var signedIn = await reader.GetAsync(loginCallback.Headers.Location);
        Assert.Equal("/ho-so", signedIn.Headers.Location!.OriginalString);
        Assert.Contains("alice", Html(await reader.GetStringAsync("/tai-khoan")));
        Assert.Equal(HttpStatusCode.OK, (await reader.GetAsync("/ho-so")).StatusCode);
        using var afterScope = reopened.Services.CreateScope();
        var same = await afterScope.ServiceProvider.GetRequiredService<UserManager<LocalUser>>().FindByIdAsync(original.Id);
        Assert.Equal(original.StoreRoot, same!.StoreRoot);
    }

    [Fact]
    public async Task Google_owned_by_another_user_cannot_be_relinked()
    {
        await using var host = await Host();
        var alice = await Account(host, "alice");
        var bob = await Account(host, "bob");
        using (var scope = host.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<LocalUser>>();
            Assert.True((await users.AddLoginAsync((await users.FindByIdAsync(bob.Id))!, new("Google", "google-subject-a", "Google"))).Succeeded);
        }
        using var client = host.AccountClient();
        await PasswordLogin(client);
        using var start = await Start(client, true);
        using var callback = await Callback(client, start);
        using var complete = await client.PostAsync(Manage + "?handler=Complete", Form(Token(await client.GetStringAsync(Manage)), ("Password", Password)));
        Assert.Contains("Chưa thay đổi được liên kết", Html(await complete.Content.ReadAsStringAsync()));
        Assert.Empty(await Logins(host, alice.Id));
        Assert.Single(await Logins(host, bob.Id));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Missing_ticket_or_ticket_for_another_account_cannot_link(bool otherAccount)
    {
        await using var host = await Host();
        var alice = await Account(host, "alice");
        var bob = await Account(host, "bob");
        var cookies = new CookieContainer();
        using var client = host.AccountClient(cookies);
        await PasswordLogin(client);
        if (otherAccount)
        {
            using var start = await Start(client, true);
            using var callback = await Callback(client, start);
            // Replace only the application identity, preserving Alice's pending external ticket.
            var bobCookies = new CookieContainer();
            using var bobClient = host.AccountClient(bobCookies);
            await PasswordLogin(bobClient, "bob");
            foreach (Cookie cookie in bobCookies.GetCookies(host.Address))
                if (cookie.Name.StartsWith("__Host-taxmanager.identity", StringComparison.Ordinal))
                    cookies.Add(host.Address, cookie);
        }
        using var completed = await client.PostAsync(Manage + "?handler=Complete", Form(Token(await client.GetStringAsync(Manage)), ("Password", Password)));
        Assert.Equal(HttpStatusCode.OK, completed.StatusCode);
        Assert.Empty(await Logins(host, alice.Id));
        Assert.Empty(await Logins(host, bob.Id));
    }

    [Fact]
    public async Task Expired_google_ticket_cannot_link_and_errors_target_the_available_action()
    {
        var clock = new GoogleClock();
        await using var host = await Host(clock: clock);
        var alice = await Account(host, "alice");
        using var client = host.AccountClient();
        await PasswordLogin(client);
        using var start = await Start(client, true);
        using var callback = await Callback(client, start);
        var token = Token(await client.GetStringAsync(Manage));
        clock.Advance(TimeSpan.FromMinutes(6));
        using var complete = await client.PostAsync(Manage + "?handler=Complete", Form(token, ("Password", Password)));
        var html = Html(await complete.Content.ReadAsStringAsync());
        Assert.Contains("Xác nhận Google đã hết hạn", html);
        Assert.Contains("href=\"#google-start\"", html);
        Assert.Contains("id=\"google-start\"", html);
        Assert.Empty(await Logins(host, alice.Id));
    }

    [Fact]
    public async Task Provider_cancellation_is_generic_and_does_not_echo_upstream_details()
    {
        await using var host = await Host();
        using var client = host.AccountClient();
        using var start = await Start(client, false);
        var state = QueryHelpers.ParseQuery(start.Headers.Location!.Query)["state"].ToString();
        using var callback = await client.GetAsync("/signin-google?error=access_denied&error_description=synthetic-secret-detail&state=" + Uri.EscapeDataString(state));
        Assert.Equal(Result + "?failed=true", callback.Headers.Location!.OriginalString);
        var html = Html(await client.GetStringAsync(callback.Headers.Location));
        Assert.Contains("Chưa hoàn tất", html);
        Assert.DoesNotContain("synthetic-secret-detail", html);
    }

    [Fact]
    public async Task Cancel_discards_pending_ticket_and_does_not_link()
    {
        await using var host = await Host();
        var alice = await Account(host, "alice");
        using var client = host.AccountClient();
        await PasswordLogin(client);
        using var start = await Start(client, true);
        using var callback = await Callback(client, start);
        using var cancel = await client.PostAsync(Manage + "?handler=Cancel", Form(Token(await client.GetStringAsync(Manage))));
        Assert.Equal(HttpStatusCode.Redirect, cancel.StatusCode);
        using var complete = await client.PostAsync(Manage + "?handler=Complete", Form(Token(await client.GetStringAsync(Manage)), ("Password", Password)));
        Assert.Equal(HttpStatusCode.OK, complete.StatusCode);
        Assert.Empty(await Logins(host, alice.Id));
    }

    [Fact]
    public async Task Remove_needs_password_revokes_other_sessions_and_preserves_local_login()
    {
        await using var host = await Host();
        var alice = await Account(host, "alice", linked: true);
        using var first = host.AccountClient(); using var second = host.AccountClient();
        await PasswordLogin(first); await PasswordLogin(second);
        var token = Token(await first.GetStringAsync(Manage));
        using var wrong = await first.PostAsync(Manage + "?handler=Remove", Form(token, ("Password", "wrong password")));
        Assert.Equal(HttpStatusCode.OK, wrong.StatusCode);
        Assert.Single(await Logins(host, alice.Id));
        using var remove = await first.PostAsync(Manage + "?handler=Remove", Form(token, ("Password", Password)));
        Assert.Equal(Login, remove.Headers.Location!.OriginalString);
        Assert.Empty(await Logins(host, alice.Id));
        Assert.Equal(HttpStatusCode.Redirect, (await second.GetAsync("/ho-so")).StatusCode);
        using var start = await Start(first, false);
        using var callback = await Callback(first, start);
        Assert.Contains("Bắt đầu với Google", Html(await first.GetStringAsync(callback.Headers.Location)));
        await PasswordLogin(first);
        Assert.Equal(HttpStatusCode.OK, (await first.GetAsync("/ho-so")).StatusCode);
    }

    [Fact]
    public async Task Google_cannot_bypass_local_lockout()
    {
        await using var host = await Host();
        var alice = await Account(host, "alice", linked: true);
        using (var scope = host.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<LocalUser>>();
            Assert.True((await users.SetLockoutEndDateAsync((await users.FindByIdAsync(alice.Id))!, DateTimeOffset.UtcNow.AddMinutes(5))).Succeeded);
        }
        using var client = host.AccountClient();
        using var start = await Start(client, false);
        using var callback = await Callback(client, start);
        Assert.Contains("Chưa đăng nhập được", Html(await client.GetStringAsync(callback.Headers.Location)));
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/ho-so")).StatusCode);
    }

    private sealed class GoogleClock : TimeProvider
    {
        private DateTimeOffset now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => now;
        public void Advance(TimeSpan elapsed) => now += elapsed;
    }

    [Fact]
    public async Task New_google_account_requires_explicit_consent_then_signs_in_without_a_local_password_and_survives_restart()
    {
        string root, id, storeRoot;
        await using (var host = await Host())
        {
            root = host.Services.GetRequiredService<SyntheticDatabase>().RootPath;
            using var client = host.AccountClient();
            using var start = await Start(client, false);
            using var callback = await Callback(client, start);
            var html = await client.GetStringAsync(callback.Headers.Location);
            var token = Token(html);
            Assert.DoesNotContain("type=\"password\"", html);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync(Result + "?handler=Create", Form("", ("AcceptDataUse", "true")))).StatusCode);
            using var uncheckedPost = await client.PostAsync(Result + "?handler=Create", Form(token));
            Assert.Contains("Đọc và xác nhận", Html(await uncheckedPost.Content.ReadAsStringAsync()));
            using (var scope = host.Services.CreateScope())
                Assert.Empty(await scope.ServiceProvider.GetRequiredService<LocalIdentityDb>().Users.ToArrayAsync());
            using var created = await client.PostAsync(Result + "?handler=Create", Form(token, ("AcceptDataUse", "true"), ("Password", "ignored-forged-password"), ("UserId", "forged")));
            Assert.Equal("/ho-so", created.Headers.Location!.OriginalString);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/ho-so")).StatusCode);
            using (var scope = host.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<LocalIdentityDb>();
                var user = Assert.Single(await db.Users.ToArrayAsync());
                Assert.Null(user.PasswordHash);
                Assert.Null(user.Email);
                id = user.Id; storeRoot = user.StoreRoot;
                Assert.NotEqual("forged", id);
                Assert.NotNull(await scope.ServiceProvider.GetRequiredService<LocalDataUse>().ReadAsync(user));
                Assert.Single(await db.UserLogins.ToArrayAsync());
            }
            var linked = Html(await client.GetStringAsync(Manage));
            Assert.DoesNotContain("type=\"password\"", linked);
            Assert.Contains("cách đăng nhập duy nhất", linked);
            using var remove = await client.PostAsync(Manage + "?handler=Remove", Form(Token(await client.GetStringAsync("/tai-khoan")), ("Password", "forged password")));
            Assert.Single(await Logins(host, id));
        }
        await using var reopened = await Host(root);
        using var reader = reopened.AccountClient();
        using var loginStart = await Start(reader, false);
        using var loginCallback = await Callback(reader, loginStart);
        using var signedIn = await reader.GetAsync(loginCallback.Headers.Location);
        Assert.Equal("/ho-so", signedIn.Headers.Location!.OriginalString);
        using var afterScope = reopened.Services.CreateScope();
        var same = await afterScope.ServiceProvider.GetRequiredService<UserManager<LocalUser>>().FindByIdAsync(id);
        Assert.Equal(storeRoot, same!.StoreRoot);
        Assert.Null(same.PasswordHash);
    }

    [Fact]
    public async Task Missing_google_ticket_cannot_create_passwordless_account()
    {
        await using var host = await Host();
        using var client = host.AccountClient();
        using var response = await client.PostAsync(Result + "?handler=Create", Form(Token(await client.GetStringAsync(Login)), ("AcceptDataUse", "true")));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var scope = host.Services.CreateScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<LocalIdentityDb>().Users.ToArrayAsync());
    }

    [Fact]
    public async Task Failed_google_registration_acceptance_rolls_back_user_and_link()
    {
        await using var host = await Host();
        using var client = host.AccountClient();
        using var start = await Start(client, false);
        using var callback = await Callback(client, start);
        var token = Token(await client.GetStringAsync(callback.Headers.Location));
        using (var scope = host.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<LocalIdentityDb>().Database.ExecuteSqlRawAsync(
                "CREATE TRIGGER reject_google_acceptance BEFORE INSERT ON AspNetUserClaims BEGIN SELECT RAISE(ABORT, 'synthetic failure'); END");
        using var failed = await client.PostAsync(Result + "?handler=Create", Form(token, ("AcceptDataUse", "true")));
        Assert.Equal(HttpStatusCode.InternalServerError, failed.StatusCode);
        using var after = host.Services.CreateScope();
        var db = after.ServiceProvider.GetRequiredService<LocalIdentityDb>();
        Assert.Empty(await db.Users.ToArrayAsync());
        Assert.Empty(await db.UserLogins.ToArrayAsync());
        Assert.Empty(await db.UserClaims.ToArrayAsync());
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/ho-so")).StatusCode);
    }

    [Fact]
    public async Task Passwordless_export_needs_fresh_google_proof_for_same_user_and_is_consumed_once()
    {
        var provider = new GoogleBackchannel();
        await using var host = await Host(provider: provider);
        var alice = await Account(host, "alice", linked: true, passwordless: true);
        var cookies = new CookieContainer();
        using var client = host.AccountClient(cookies);
        await GoogleLogin(client);
        const string export = "/tai-khoan/xuat-du-lieu";
        var html = await client.GetStringAsync(export);
        Assert.DoesNotContain("type=\"password\"", html);
        using var denied = await client.PostAsync(export, Form(Token(html), ("Password", "forged")));
        Assert.Null(denied.Content.Headers.ContentDisposition);
        Assert.Contains("Xác nhận lại bằng Google", Html(await denied.Content.ReadAsStringAsync()));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync(export + "?handler=Google", Form(""))).StatusCode);
        provider.Subject = "different-google-subject";
        await ConfirmGoogle(client, export);
        using var wrong = await client.PostAsync(export, Form(Token(await client.GetStringAsync(export))));
        Assert.Null(wrong.Content.Headers.ContentDisposition);
        provider.Subject = "google-subject-a";
        await ConfirmGoogle(client, export);
        var token = Token(await client.GetStringAsync(export));
        var savedExternal = cookies.GetCookies(host.Address).Cast<Cookie>()
            .Where(cookie => cookie.Name.StartsWith("__Host-taxmanager.external", StringComparison.Ordinal))
            .Select(cookie => new Cookie(cookie.Name, cookie.Value, cookie.Path, cookie.Domain) { Secure = true, HttpOnly = true }).ToArray();
        Assert.NotEmpty(savedExternal);
        using var allowed = await client.PostAsync(export, Form(token));
        Assert.Equal("application/json", allowed.Content.Headers.ContentType!.MediaType);
        using var document = JsonDocument.Parse(await allowed.Content.ReadAsStringAsync());
        Assert.Equal(alice.Id, document.RootElement.GetProperty("account").GetProperty("id").GetString());
        foreach (var cookie in savedExternal) cookies.Add(host.Address, cookie);
        using var replay = await client.PostAsync(export, Form(token));
        Assert.Null(replay.Content.Headers.ContentDisposition);
    }

    [Fact]
    public async Task Passwordless_delete_requires_its_own_google_confirmation_checkbox_and_removes_google_login()
    {
        await using var host = await Host();
        var alice = await Account(host, "alice", linked: true, passwordless: true);
        using var client = host.AccountClient();
        await GoogleLogin(client);
        const string deletion = "/tai-khoan/xoa";
        using var denied = await client.PostAsync(deletion, Form(Token(await client.GetStringAsync(deletion)), ("ConfirmDeletion", "true")));
        Assert.Contains("Xác nhận lại bằng Google", Html(await denied.Content.ReadAsStringAsync()));
        // An export confirmation is not permission to delete.
        await ConfirmGoogle(client, "/tai-khoan/xuat-du-lieu");
        using var wrongPurpose = await client.PostAsync(deletion, Form(Token(await client.GetStringAsync(deletion)), ("ConfirmDeletion", "true")));
        Assert.True(Directory.Exists(alice.StoreRoot));
        Assert.Contains("Xác nhận lại bằng Google", Html(await wrongPurpose.Content.ReadAsStringAsync()));
        await ConfirmGoogle(client, deletion);
        var token = Token(await client.GetStringAsync(deletion));
        using var uncheckedPost = await client.PostAsync(deletion, Form(token));
        Assert.Contains("Đánh dấu xác nhận", Html(await uncheckedPost.Content.ReadAsStringAsync()));
        using var deleted = await client.PostAsync(deletion, Form(token, ("ConfirmDeletion", "true")));
        Assert.Contains("deleted=True", deleted.Headers.Location!.OriginalString, StringComparison.OrdinalIgnoreCase);
        Assert.False(Directory.Exists(alice.StoreRoot));
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LocalIdentityDb>();
        Assert.Empty(await db.Users.ToArrayAsync());
        Assert.Empty(await db.UserLogins.ToArrayAsync());
    }

    [Fact]
    public async Task Expired_sensitive_google_proof_cannot_export()
    {
        var clock = new GoogleClock();
        await using var host = await Host(clock: clock);
        await Account(host, "alice", linked: true, passwordless: true);
        using var client = host.AccountClient();
        await GoogleLogin(client);
        const string export = "/tai-khoan/xuat-du-lieu";
        await ConfirmGoogle(client, export);
        var token = Token(await client.GetStringAsync(export));
        clock.Advance(TimeSpan.FromMinutes(6));
        using var denied = await client.PostAsync(export, Form(token));
        Assert.Null(denied.Content.Headers.ContentDisposition);
        Assert.Contains("Xác nhận lại bằng Google", Html(await denied.Content.ReadAsStringAsync()));
    }

    [Fact]
    public async Task Passwordless_export_store_failure_returns_no_file_and_error_targets_google_retry()
    {
        await using var host = await Host();
        var alice = await Account(host, "alice", linked: true, passwordless: true);
        using var client = host.AccountClient();
        await GoogleLogin(client);
        const string export = "/tai-khoan/xuat-du-lieu";
        await ConfirmGoogle(client, export);
        var token = Token(await client.GetStringAsync(export));
        // The fixture owns this synthetic root. Account pages have not opened its tax store.
        File.Delete(Path.Combine(alice.StoreRoot, "synthetic-key.json"));
        using var failed = await client.PostAsync(export, Form(token));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, failed.StatusCode);
        Assert.Null(failed.Content.Headers.ContentDisposition);
        var html = Html(await failed.Content.ReadAsStringAsync());
        Assert.Contains("Chưa tạo được tệp dữ liệu", html);
        Assert.Contains("href=\"#google-confirm\"", html);
        Assert.Contains("id=\"google-confirm\"", html);
        Assert.DoesNotContain(alice.StoreRoot, html);
        Assert.False(File.Exists(Path.Combine(alice.StoreRoot, "synthetic-key.json")));
    }

    private static async Task GoogleLogin(HttpClient client)
    {
        using var start = await Start(client, false);
        using var callback = await Callback(client, start);
        using var signedIn = await client.GetAsync(callback.Headers.Location);
        Assert.Equal("/ho-so", signedIn.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Passwordless_email_management_requires_correct_google_purpose_and_consumes_proof()
    {
        var sender = new RecoverySender();
        await using var host = await Host(email: sender);
        var alice = await Account(host, "alice", linked: true, passwordless: true);
        using var client = host.AccountClient();
        await GoogleLogin(client);
        const string email = "/tai-khoan/email";
        var initial = await client.GetStringAsync(email);
        Assert.DoesNotContain("type=\"password\"", initial);
        using var missing = await client.PostAsync(email, Form(Token(initial)));
        var denied = Html(await missing.Content.ReadAsStringAsync());
        Assert.Contains("href=\"#google-confirm\"", denied);
        Assert.DoesNotContain("Nhập email khôi phục.", denied);
        await ConfirmGoogle(client, "/tai-khoan/xuat-du-lieu");
        using var wrongPurpose = await client.PostAsync(email, Form(Token(await client.GetStringAsync(email)), ("Email", "alice@example.invalid")));
        Assert.Empty(sender.Messages);
        await ConfirmGoogle(client, email);
        var ready = await client.GetStringAsync(email);
        Assert.Contains("id=\"Email\"", ready);
        Assert.DoesNotContain("type=\"password\"", ready);
        using var sent = await client.PostAsync(email, Form(Token(ready), ("Email", "alice@example.invalid")));
        Assert.Equal(HttpStatusCode.Redirect, sent.StatusCode);
        Assert.Single(sender.Messages);
        using var replay = await client.PostAsync(email, Form(Token(ready), ("Email", "other@example.invalid")));
        Assert.Contains("Xác nhận lại bằng Google", Html(await replay.Content.ReadAsStringAsync()));
        Assert.Single(sender.Messages);
        Assert.Null((await ReadGoogleUser(host, alice.Id)).Email);
    }

    [Fact]
    public async Task Verified_email_can_create_first_password_for_google_account_without_changing_store_or_google_link()
    {
        var sender = new RecoverySender();
        await using var host = await Host(email: sender);
        var alice = await Account(host, "alice", linked: true, passwordless: true);
        using (var scope = host.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<LocalUser>>();
            var user = (await users.FindByIdAsync(alice.Id))!;
            user.Email = "alice@example.invalid";
            user.EmailConfirmed = true;
            Assert.True((await users.UpdateAsync(user)).Succeeded);
        }
        using var client = host.AccountClient();
        const string forgot = "/tai-khoan/quen-mat-khau";
        using var requested = await client.PostAsync(forgot, Form(Token(await client.GetStringAsync(forgot)), ("Email", "alice@example.invalid")));
        var link = new Uri(Regex.Match(Assert.Single(sender.Messages).Text, "https://[^\\s]+").Value).PathAndQuery;
        using var reset = await client.PostAsync(link, Form(Token(await client.GetStringAsync(link)), ("Password", Password), ("ConfirmPassword", Password)));
        Assert.Equal(HttpStatusCode.Redirect, reset.StatusCode);
        await PasswordLogin(client);
        var after = await ReadGoogleUser(host, alice.Id);
        Assert.Equal(alice.StoreRoot, after.StoreRoot);
        Assert.NotNull(after.PasswordHash);
        Assert.Single(await Logins(host, alice.Id));
    }

    private static async Task<LocalUser> ReadGoogleUser(DemoHost host, string id)
    {
        using var scope = host.Services.CreateScope();
        return (await scope.ServiceProvider.GetRequiredService<UserManager<LocalUser>>().FindByIdAsync(id))!;
    }

    private sealed class RecoverySender : IAccountEmailSender
    {
        public List<AccountEmail> Messages { get; } = [];
        public Task SendAsync(AccountEmail message, CancellationToken token) { Messages.Add(message); return Task.CompletedTask; }
    }

    private static async Task ConfirmGoogle(HttpClient client, string page)
    {
        using var start = await client.PostAsync(page + "?handler=Google", Form(Token(await client.GetStringAsync(page))));
        Assert.Equal(HttpStatusCode.Redirect, start.StatusCode);
        using var callback = await Callback(client, start);
        Assert.Contains("Tiếp tục", Html(await client.GetStringAsync(callback.Headers.Location)));
    }

    private static Task<DemoHost> Host(string? root = null, TimeProvider? clock = null, GoogleBackchannel? provider = null, RecoverySender? email = null) => DemoHost.StartAsync(accounts: true, syntheticRoot: root, clock: clock,
        extraArgs: new[] { "--Authentication:Google:Enabled=true", "--Authentication:Google:ClientId=synthetic-client-id", "--Authentication:Google:ClientSecret=synthetic-client-secret" }
            .Concat(email is null ? [] : new[] { "--EmailRecovery:Enabled=true", "--EmailRecovery:PublicOrigin=https://localhost:5088", "--EmailRecovery:Resend:ApiKey=synthetic", "--EmailRecovery:Resend:From=demo@example.invalid" }).ToArray(),
        configure: services =>
        {
            services.Configure<GoogleOptions>("Google", options => options.Backchannel = new HttpClient(provider ?? new GoogleBackchannel()));
            if (email is not null) Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.Replace(services, ServiceDescriptor.Singleton<IAccountEmailSender>(email));
        });

    private sealed class GoogleBackchannel : HttpMessageHandler
    {
        public string Subject { get; set; } = "google-subject-a";
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            string json;
            if (request.RequestUri!.AbsoluteUri == GoogleDefaults.TokenEndpoint)
            {
                Assert.Equal(HttpMethod.Post, request.Method);
                var form = await request.Content!.ReadAsStringAsync(token);
                Assert.Contains("code_verifier=", form);
                json = """{"access_token":"synthetic-access-token","token_type":"Bearer","expires_in":300}""";
            }
            else
            {
                Assert.Equal(GoogleDefaults.UserInformationEndpoint, request.RequestUri.AbsoluteUri);
                Assert.Equal("synthetic-access-token", request.Headers.Authorization!.Parameter);
                json = JsonSerializer.Serialize(new { sub = Subject, email = "alice@example.invalid", name = "Not the local username" });
            }
            return new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        }
    }

    private static async Task<LocalUser> Account(DemoHost host, string name, bool linked = false, bool passwordless = false)
    {
        using var store = new SyntheticDatabase(Directory.GetCurrentDirectory());
        using var scope = host.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<LocalUser>>();
        var user = new LocalUser { UserName = name, StoreRoot = store.RootPath };
        Assert.True((passwordless ? await users.CreateAsync(user) : await users.CreateAsync(user, Password)).Succeeded);
        Assert.True((await scope.ServiceProvider.GetRequiredService<LocalDataUse>().AcceptAsync(user)).Succeeded);
        if (linked) Assert.True((await users.AddLoginAsync(user, new("Google", "google-subject-a", "Google"))).Succeeded);
        return user;
    }
    private static async Task<UserLoginInfo[]> Logins(DemoHost host, string id)
    {
        using var scope = host.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<LocalUser>>();
        return (await users.GetLoginsAsync((await users.FindByIdAsync(id))!)).ToArray();
    }
    private static async Task PasswordLogin(HttpClient client, string name = "alice")
    {
        using var response = await client.PostAsync(Login, Form(Token(await client.GetStringAsync(Login)), ("Username", name), ("Password", Password)));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }
    private static async Task<HttpResponseMessage> Start(HttpClient client, bool link, string returnUrl = "/ho-so")
    {
        var token = Token(await client.GetStringAsync(link ? Manage : Login));
        var response = await client.PostAsync(link ? Manage + "?handler=Start" : Entry, Form(token, ("returnUrl", returnUrl)));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return response;
    }
    private static async Task<HttpResponseMessage> Callback(HttpClient client, HttpResponseMessage start)
    {
        var state = QueryHelpers.ParseQuery(start.Headers.Location!.Query)["state"].ToString();
        var response = await client.GetAsync("/signin-google?code=synthetic&state=" + Uri.EscapeDataString(state));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith(Result, response.Headers.Location!.OriginalString);
        Assert.DoesNotContain("failed", response.Headers.Location.OriginalString);
        return response;
    }
    private static string Html(string html) => WebUtility.HtmlDecode(html);
    private static string Token(string html)
    {
        var token = Html(Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value);
        Assert.NotEmpty(token);
        return token;
    }
    private static FormUrlEncodedContent Form(string token, params (string Key, string Value)[] fields) =>
        new(fields.Select(field => new KeyValuePair<string, string>(field.Key, field.Value)).Append(new("__RequestVerificationToken", token)));
}
