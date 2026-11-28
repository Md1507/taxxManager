using System.Net;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaxManager.Web.Accounts;
using TaxManager.Web.Demo;
using Xunit;

namespace TaxManager.Web.Tests.Security;

public sealed class LocalDataUseHttpTests
{
    private const string Password = "synthetic consent password 123";
    private const string RegisterPath = "/tai-khoan/dang-ky";
    private const string LoginPath = "/tai-khoan/dang-nhap";
    private const string ConsentPath = "/tai-khoan/du-lieu-thu";

    [Fact]
    public async Task Registration_requires_explicit_acceptance_and_uses_server_version_and_time()
    {
        await using var host = await DemoHost.StartAsync(accounts: true);
        using var client = host.AccountClient();
        var html = await client.GetStringAsync(RegisterPath);
        Assert.DoesNotContain("checked", Regex.Match(html, "<input[^>]*name=\"AcceptDataUse\"[^>]*>").Value);
        Assert.Contains(LocalDataUse.DisplayVersion, html);
        var before = DateTimeOffset.UtcNow;
        foreach (var accept in new[] { "", "false" })
        {
            using var rejected = await client.PostAsync(RegisterPath, Registration(Token(html), "alice", accept));
            Assert.Equal(HttpStatusCode.OK, rejected.StatusCode);
            var error = WebUtility.HtmlDecode(await rejected.Content.ReadAsStringAsync());
            Assert.Contains("Đọc và xác nhận", error);
            Assert.DoesNotContain(Password, error);
            Assert.Contains("value=\"alice\"", error);
            using var scope = host.Services.CreateScope();
            Assert.Empty(await scope.ServiceProvider.GetRequiredService<LocalIdentityDb>().Users.ToArrayAsync());
            Assert.Empty(await scope.ServiceProvider.GetRequiredService<LocalIdentityDb>().UserClaims.ToArrayAsync());
        }
        using var registered = await client.PostAsync(RegisterPath, Registration(Token(html), "alice", "true"));
        Assert.Equal(HttpStatusCode.Redirect, registered.StatusCode);
        var acceptance = await Acceptance(host, "alice");
        Assert.Equal(LocalDataUse.Version, acceptance!.Version);
        Assert.InRange(acceptance.AcceptedAt, before, DateTimeOffset.UtcNow);
        await Login(client, "alice");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/ho-so")).StatusCode);
        // A duplicate registration must not leave another acknowledgement from its failed transaction.
        using var anonymous = host.AccountClient();
        using var duplicate = await anonymous.PostAsync(RegisterPath, Registration(Token(await anonymous.GetStringAsync(RegisterPath)), "ALICE", "true"));
        Assert.Equal(HttpStatusCode.OK, duplicate.StatusCode);
        using var finalScope = host.Services.CreateScope();
        var db = finalScope.ServiceProvider.GetRequiredService<LocalIdentityDb>();
        Assert.Single(await db.Users.ToArrayAsync());
        Assert.Single(await db.UserClaims.Where(c => c.ClaimType == LocalDataUse.ClaimType).ToArrayAsync());
    }

    [Fact]
    public async Task Failed_acceptance_write_rolls_back_registration_instead_of_creating_an_unacknowledged_user()
    {
        await using var host = await DemoHost.StartAsync(accounts: true);
        using var client = host.AccountClient();
        using (var scope = host.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<LocalIdentityDb>().Database.ExecuteSqlRawAsync(
                "CREATE TRIGGER reject_acceptance BEFORE INSERT ON AspNetUserClaims BEGIN SELECT RAISE(ABORT, 'synthetic failure'); END");
        using var failed = await client.PostAsync(RegisterPath, Registration(Token(await client.GetStringAsync(RegisterPath)), "alice", "true"));
        Assert.Equal(HttpStatusCode.InternalServerError, failed.StatusCode);
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LocalIdentityDb>();
            Assert.Empty(await db.Users.ToArrayAsync());
            Assert.Empty(await db.UserClaims.ToArrayAsync());
            await db.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_acceptance");
        }
        using var retry = await client.PostAsync(RegisterPath, Registration(Token(await client.GetStringAsync(RegisterPath)), "alice", "true"));
        Assert.Equal(HttpStatusCode.Redirect, retry.StatusCode);
        Assert.NotNull(await Acceptance(host, "alice"));
    }

    [Fact]
    public async Task Legacy_account_cannot_read_or_post_any_data_until_confirmed_and_other_account_stays_blocked()
    {
        await using var host = await DemoHost.StartAsync(accounts: true);
        await LegacyUser(host, "alice"); await LegacyUser(host, "bob");
        using var a = host.AccountClient(); using var b = host.AccountClient();
        await Login(a, "alice"); await Login(b, "bob");
        var id = Guid.NewGuid();
        foreach (var path in new[] { "/", "/ho-so", "/Forecast", $"/Forecast?id={id}", $"/ho-so/{id}/thang/1?revision=1",
            $"/ForecastAnnualHistory?id={id}", $"/ho-so/{id}/bao-cao?handler=Csv&revision=1", $"/ho-so/{id}/kich-ban",
            $"/ho-so/{id}/doi-chieu", "/sao-luu", "/khoi-phuc", "/khoi-phuc/xem-truoc" })
        {
            using var denied = await a.GetAsync(path);
            Assert.Equal(HttpStatusCode.Redirect, denied.StatusCode);
            Assert.StartsWith(ConsentPath + "?", denied.Headers.Location!.OriginalString);
        }
        foreach (var path in new[] { "/Forecast?handler=Save", "/Forecast?handler=Calculate", "/sao-luu", "/khoi-phuc", $"/ho-so/{id}/doi-chieu" })
            Assert.Equal(HttpStatusCode.Forbidden, (await a.PostAsync(path, Form(Token(await a.GetStringAsync(ConsentPath))))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await a.GetAsync("/tai-khoan")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await a.GetAsync("/site.css")).StatusCode);
        using var head = await a.SendAsync(new HttpRequestMessage(HttpMethod.Head, "/ho-so"));
        Assert.Equal(HttpStatusCode.Redirect, head.StatusCode);
        var token = Token(await a.GetStringAsync(ConsentPath));
        Assert.Equal(HttpStatusCode.BadRequest, (await a.PostAsync(ConsentPath, Form("", ("AcceptDataUse", "true")))).StatusCode);
        using var uncheckedPost = await a.PostAsync(ConsentPath, Form(token));
        Assert.Equal(HttpStatusCode.OK, uncheckedPost.StatusCode);
        Assert.Null(await Acceptance(host, "alice"));
        using var accepted = await a.PostAsync(ConsentPath, Form(token, ("AcceptDataUse", "true"), ("UserId", "bob"), ("Version", "forged"), ("AcceptedAt", "2000-01-01")));
        Assert.Equal(HttpStatusCode.Redirect, accepted.StatusCode);
        Assert.Equal("/ho-so", accepted.Headers.Location!.OriginalString);
        Assert.Equal(LocalDataUse.Version, (await Acceptance(host, "alice"))!.Version);
        Assert.Null(await Acceptance(host, "bob"));
        Assert.Equal(HttpStatusCode.OK, (await a.GetAsync("/ho-so")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await b.GetAsync("/ho-so")).StatusCode);
    }

    [Fact]
    public async Task Acceptance_is_durable_idempotent_and_registry_revocation_overrides_existing_cookie()
    {
        string root;
        DataUseAcceptance accepted;
        await using (var host = await DemoHost.StartAsync(accounts: true))
        {
            root = host.Services.GetRequiredService<SyntheticDatabase>().RootPath;
            await LegacyUser(host, "alice");
            using var client = host.AccountClient(); await Login(client, "alice");
            var token = Token(await client.GetStringAsync(ConsentPath));
            for (var i = 0; i < 2; i++)
                Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync(ConsentPath, Form(token, ("AcceptDataUse", "true")))).StatusCode);
            accepted = (await Acceptance(host, "alice"))!;
            using var scope = host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<LocalIdentityDb>();
            Assert.Single(await db.UserClaims.Where(c => c.ClaimType == LocalDataUse.ClaimType).ToArrayAsync());
        }
        await using var reopened = await DemoHost.StartAsync(syntheticRoot: root, accounts: true);
        using var reader = reopened.AccountClient(); await Login(reader, "alice");
        Assert.Equal(accepted, await Acceptance(reopened, "alice"));
        Assert.Equal(HttpStatusCode.OK, (await reader.GetAsync("/ho-so")).StatusCode);
        using (var scope = reopened.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<LocalUser>>();
            var user = (await users.FindByNameAsync("alice"))!;
            Assert.True((await users.RemoveClaimsAsync(user, (await users.GetClaimsAsync(user)).Where(c => c.Type == LocalDataUse.ClaimType))).Succeeded);
            // The current cookie still contains the old claim, but must no longer open data.
        }
        Assert.Equal(HttpStatusCode.Redirect, (await reader.GetAsync("/ho-so")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await reader.PostAsync("/Forecast?handler=Save", Form(""))).StatusCode);
    }

    [Theory]
    [InlineData("old-version|2026-01-01T00:00:00.0000000+00:00")]
    [InlineData("local-demo-2026-11-28-v2|invalid")]
    [InlineData("local-demo-2026-11-29-v3|invalid")]
    [InlineData("local-demo-2026-11-29-v3|2099-01-01T00:00:00.0000000+00:00")]
    [InlineData("local-demo-2026-11-28-v2|2099-01-01T00:00:00.0000000+00:00")]
    public async Task Malformed_old_or_future_claim_is_not_acceptance(string value)
    {
        await using var host = await DemoHost.StartAsync(accounts: true);
        await LegacyUser(host, "alice");
        using (var scope = host.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<LocalUser>>();
            Assert.True((await users.AddClaimAsync((await users.FindByNameAsync("alice"))!, new Claim(LocalDataUse.ClaimType, value))).Succeeded);
        }
        using var client = host.AccountClient(); await Login(client, "alice");
        Assert.Null(await Acceptance(host, "alice"));
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/ho-so")).StatusCode);
    }

    [Theory]
    [InlineData("https://outside.invalid/", "/ho-so")]
    [InlineData("/tai-khoan/du-lieu-thu", "/ho-so")]
    [InlineData("~/tai-khoan/du-lieu-thu", "/ho-so")]
    [InlineData("/%74ai-khoan/du-lieu-thu", "/ho-so")]
    [InlineData("/Forecast?month=3#sua-thang", "/Forecast?month=3#sua-thang")]
    public async Task Return_url_is_local_and_cannot_loop_to_account_page(string destination, string expected)
    {
        await using var host = await DemoHost.StartAsync(accounts: true);
        using var anonymous = host.AccountClient();
        Assert.Equal(HttpStatusCode.Redirect, (await anonymous.GetAsync(ConsentPath)).StatusCode);
        await LegacyUser(host, "alice");
        using var client = host.AccountClient(); await Login(client, "alice");
        using var response = await client.PostAsync(ConsentPath, Form(Token(await client.GetStringAsync(ConsentPath)), ("AcceptDataUse", "true"), ("ReturnUrl", destination)));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(expected, response.Headers.Location!.OriginalString);
    }

    private static async Task LegacyUser(DemoHost host, string name)
    {
        using var store = new SyntheticDatabase(Directory.GetCurrentDirectory());
        using var scope = host.Services.CreateScope();
        Assert.True((await scope.ServiceProvider.GetRequiredService<UserManager<LocalUser>>().CreateAsync(
            new LocalUser { UserName = name, StoreRoot = store.RootPath }, Password)).Succeeded);
    }
    private static async Task<DataUseAcceptance?> Acceptance(DemoHost host, string name)
    {
        using var scope = host.Services.CreateScope();
        var user = (await scope.ServiceProvider.GetRequiredService<UserManager<LocalUser>>().FindByNameAsync(name))!;
        return await scope.ServiceProvider.GetRequiredService<LocalDataUse>().ReadAsync(user);
    }
    private static async Task Login(HttpClient client, string name)
    {
        using var response = await client.PostAsync(LoginPath, Form(Token(await client.GetStringAsync(LoginPath)), ("Username", name), ("Password", Password)));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }
    private static FormUrlEncodedContent Registration(string token, string name, string accepted) => Form(token,
        ("Username", name), ("Password", Password), ("ConfirmPassword", Password), ("AcceptDataUse", accepted));
    private static FormUrlEncodedContent Form(string token, params (string Key, string Value)[] fields) =>
        new(fields.Select(f => new KeyValuePair<string, string>(f.Key, f.Value)).Append(new("__RequestVerificationToken", token)));
    private static string Token(string html) => WebUtility.HtmlDecode(Regex.Match(html,
        "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value);
}
