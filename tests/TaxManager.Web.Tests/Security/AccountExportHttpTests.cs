using System.Net;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaxManager.Core.Forecasting;
using TaxManager.Data.Audit;
using TaxManager.Data.Forecasting;
using TaxManager.Data.Protection;
using TaxManager.Web.Accounts;
using TaxManager.Web.Backup;
using TaxManager.Web.Demo;
using Xunit;

namespace TaxManager.Web.Tests.Security;

public sealed class AccountExportHttpTests
{
    private const string Password = "synthetic export password 123";
    private const string Path = "/tai-khoan/xuat-du-lieu";

    [Fact]
    public async Task Export_is_opt_in_authenticated_post_only_and_requires_csrf_and_current_password()
    {
        await using (var demo = await DemoHost.StartAsync())
        {
            using var publicClient = new HttpClient { BaseAddress = demo.Address };
            Assert.Equal(HttpStatusCode.NotFound, (await publicClient.GetAsync(Path)).StatusCode);
        }
        await using var host = await DemoHost.StartAsync(accounts: true);
        using var client = host.AccountClient();
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync(Path)).StatusCode);
        await Account(host, "alice"); await Login(client, "alice");
        using var get = await client.GetAsync(Path + "?handler=Download");
        Assert.Equal("text/html", get.Content.Headers.ContentType!.MediaType);
        Assert.Null(get.Content.Headers.ContentDisposition);
        var token = Token(await get.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync(Path, Form("", ("Password", Password)))).StatusCode);
        foreach (var password in new[] { "", "incorrect synthetic password" })
        {
            using var denied = await client.PostAsync(Path, Form(token, ("Password", password)));
            Assert.Equal(HttpStatusCode.OK, denied.StatusCode);
            Assert.Null(denied.Content.Headers.ContentDisposition);
            var html = await denied.Content.ReadAsStringAsync();
            Assert.DoesNotContain("incorrect synthetic password", html);
            Assert.Contains("error-summary", html);
        }
        using var success = await client.PostAsync(Path, Form(token, ("Password", Password)));
        Assert.Equal(HttpStatusCode.OK, success.StatusCode);
        Assert.Equal("application/json", success.Content.Headers.ContentType!.MediaType);
        Assert.Equal("attachment", success.Content.Headers.ContentDisposition!.DispositionType);
        Assert.True(success.Headers.CacheControl!.NoStore);
        Assert.Equal("nosniff", Assert.Single(success.Headers.GetValues("X-Content-Type-Options")));
        using var json = JsonDocument.Parse(await success.Content.ReadAsStringAsync());
        Assert.Empty(json.RootElement.GetProperty("taxData").GetProperty("forecasts").EnumerateArray());
        await using var context = (await Store(host, "alice")).Open();
        Assert.Empty(await context.Set<DataProtectionMetadata>().ToArrayAsync());
    }

    [Fact]
    public async Task File_contains_only_owner_original_data_and_retained_history_without_security_material_or_writes()
    {
        await using var host = await DemoHost.StartAsync(accounts: true);
        await Account(host, "alice"); await Account(host, "bob");
        var a = await Store(host, "alice"); var b = await Store(host, "bob");
        const string payload = "{\"source\":\"owner only\",\"missing\":null,\"zero\":0}";
        Guid id;
        await using (var write = a.Open()) id = await new ForecastStore(write).CreateAsync(payload);
        await using (var write = a.Open()) await new ForecastStore(write).UpdateAsync(id, 1, payload + " ");
        await using (var write = a.Open()) await new ForecastStore(write).DeleteAsync(id, 2);
        await using (var write = b.Open()) await new ForecastStore(write).CreateAsync("other account private");
        using var client = host.AccountClient(); await Login(client, "alice");
        var before = await AuditCount(a);
        using var response = await Download(client, ("UserId", "bob"), ("StoreRoot", b.RootPath));
        response.EnsureSuccessStatusCode();
        var text = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(text);
        var root = json.RootElement;
        Assert.Equal(1, root.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("original-account-store", root.GetProperty("scope").GetString());
        Assert.Equal("alice", root.GetProperty("account").GetProperty("username").GetString());
        Assert.Single(root.GetProperty("account").GetProperty("dataUseAcknowledgements").EnumerateArray());
        var tax = root.GetProperty("taxData");
        Assert.Empty(tax.GetProperty("forecasts").EnumerateArray());
        var versions = tax.GetProperty("forecastVersions").EnumerateArray().OrderBy(v => v.GetProperty("revision").GetInt32()).ToArray();
        Assert.Equal(new[] { payload, payload + " " }, versions.Select(v => v.GetProperty("payload").GetString()));
        Assert.All(versions, v => Assert.Equal(id, v.GetProperty("documentId").GetGuid()));
        Assert.Equal(before, tax.GetProperty("audit").GetArrayLength());
        foreach (var forbidden in new[] { "other account private", Password, "passwordHash", "securityStamp", "concurrencyStamp",
            "storeRoot", "synthetic-key", "ciphertext", "identity.db", a.RootPath, b.RootPath })
            Assert.DoesNotContain(forbidden, text, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(before, await AuditCount(a));
    }

    [Fact]
    public async Task Original_export_is_independent_of_an_accepted_restore()
    {
        await using var host = await DemoHost.StartAsync(accounts: true);
        await Account(host, "alice");
        var store = await Store(host, "alice");
        Guid id;
        var older = Payload(30_000_000); var newer = Payload(40_000_000);
        await using (var write = store.Open()) id = await new ForecastStore(write).CreateAsync(older);
        var backup = await host.Services.GetRequiredService<BackupStore>().ExportAsync(store, Password, DateTimeOffset.UtcNow, default);
        await using (var write = store.Open()) await new ForecastStore(write).UpdateAsync(id, 1, newer);
        using var client = host.AccountClient(); await Login(client, "alice");
        using var upload = new MultipartFormDataContent();
        upload.Add(new StringContent(Token(await client.GetStringAsync("/khoi-phuc"))), "__RequestVerificationToken");
        upload.Add(new StringContent(Password), "password");
        upload.Add(new ByteArrayContent(backup), "upload", "own.taxbackup");
        using var preview = await client.PostAsync("/khoi-phuc", upload);
        Assert.Equal(HttpStatusCode.Redirect, preview.StatusCode);
        var location = preview.Headers.Location!;
        var html = await client.GetStringAsync(location);
        using var accept = await client.PostAsync(location, Form(Token(html), ("handle", Value(html, "handle"))));
        Assert.Equal(HttpStatusCode.Redirect, accept.StatusCode);
        using var response = await Download(client);
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(newer, Assert.Single(json.RootElement.GetProperty("taxData").GetProperty("forecasts").EnumerateArray()).GetProperty("payload").GetString());
    }

    [Fact]
    public async Task Corrupt_protected_data_returns_no_file_and_does_not_expose_paths()
    {
        await using var host = await DemoHost.StartAsync(accounts: true);
        await Account(host, "alice"); var store = await Store(host, "alice");
        await using (var write = store.Open())
        {
            await new ForecastStore(write).CreateAsync("private content");
            await write.Database.ExecuteSqlRawAsync("UPDATE ForecastDocuments SET PayloadEncrypted=X'00'");
        }
        using var client = host.AccountClient(); await Login(client, "alice");
        using var response = await Download(client);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Null(response.Content.Headers.ContentDisposition);
        var html = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(store.RootPath, html); Assert.DoesNotContain(Password, html);
        Assert.DoesNotContain("private content", html);
    }

    [Fact]
    public async Task Legacy_account_can_export_without_accepting_the_notice_and_account_switch_invalidates_token()
    {
        await using var host = await DemoHost.StartAsync(accounts: true);
        await Account(host, "alice", accept: false); await Account(host, "bob");
        using var client = host.AccountClient(); await Login(client, "alice");
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/ho-so")).StatusCode);
        var staleToken = Token(await client.GetStringAsync(Path));
        using var own = await client.PostAsync(Path, Form(staleToken, ("Password", Password)));
        Assert.Equal("application/json", own.Content.Headers.ContentType!.MediaType);
        using var logout = await client.PostAsync("/tai-khoan?handler=Logout", Form(Token(await client.GetStringAsync("/tai-khoan"))));
        Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);
        await Login(client, "bob");
        using var stale = await client.PostAsync(Path, Form(staleToken, ("Password", Password)));
        Assert.Equal(HttpStatusCode.BadRequest, stale.StatusCode);
    }

    [Fact]
    public async Task Repeated_wrong_export_passwords_lock_out_even_when_login_cookie_remains_valid()
    {
        await using var host = await DemoHost.StartAsync(accounts: true);
        await Account(host, "alice");
        using var client = host.AccountClient(); await Login(client, "alice");
        var token = Token(await client.GetStringAsync(Path));
        for (var i = 0; i < 5; i++)
        {
            using var denied = await client.PostAsync(Path, Form(token, ("Password", "incorrect synthetic password")));
            Assert.Null(denied.Content.Headers.ContentDisposition);
        }
        using var locked = await client.PostAsync(Path, Form(token, ("Password", Password)));
        Assert.Null(locked.Content.Headers.ContentDisposition);
        using var scope = host.Services.CreateScope();
        var user = (await scope.ServiceProvider.GetRequiredService<UserManager<LocalUser>>().FindByNameAsync("alice"))!;
        Assert.NotNull(user.LockoutEnd);
    }

    private static string Payload(decimal gross) => ForecastDraftReader.Encode(new(2026,
        Enumerable.Range(1, 12).Select(m => new ForecastMonth(m,
            new(gross, ForecastStatus.Actual, "Synthetic export", null, DateTimeOffset.UtcNow),
            new(0, ForecastStatus.Actual, null, null, DateTimeOffset.UtcNow),
            new(0, ForecastStatus.Actual, null, null, DateTimeOffset.UtcNow),
            new(0, ForecastStatus.Actual, null, null, DateTimeOffset.UtcNow))).ToArray(), []));
    private static async Task Account(DemoHost host, string name, bool accept = true)
    {
        using var scope = host.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<LocalUser>>();
        var store = new SyntheticDatabase(Directory.GetCurrentDirectory());
        var user = new LocalUser { UserName = name, StoreRoot = store.RootPath };
        store.Dispose();
        Assert.True((await users.CreateAsync(user, Password)).Succeeded);
        if (accept) Assert.True((await users.AddClaimAsync(user, new Claim(LocalDataUse.ClaimType,
            LocalDataUse.Version + "|" + DateTimeOffset.UtcNow.ToString("O")))).Succeeded);
    }
    private static async Task Login(HttpClient client, string name)
    {
        const string path = "/tai-khoan/dang-nhap";
        using var response = await client.PostAsync(path, Form(Token(await client.GetStringAsync(path)), ("Username", name), ("Password", Password)));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }
    private static async Task<HttpResponseMessage> Download(HttpClient client, params (string Key, string Value)[] extras) =>
        await client.PostAsync(Path, Form(Token(await client.GetStringAsync(Path)), extras.Append(("Password", Password)).ToArray()));
    private static async Task<SyntheticDatabase> Store(DemoHost host, string name)
    {
        using var scope = host.Services.CreateScope();
        var user = (await scope.ServiceProvider.GetRequiredService<UserManager<LocalUser>>().FindByNameAsync(name))!;
        return host.Services.GetRequiredService<LocalAccountStores>().Get(user.Id, user.StoreRoot);
    }
    private static async Task<int> AuditCount(SyntheticDatabase store)
    {
        await using var db = store.Open(); db.ValidateProtection();
        return await db.Set<AuditEvent>().CountAsync();
    }
    private static string Token(string html) => Value(html, "__RequestVerificationToken");
    private static string Value(string html, string name) => WebUtility.HtmlDecode(Regex.Match(
        Regex.Match(html, $"<input\\b[^>]*name=\"{Regex.Escape(name)}\"[^>]*>").Value, "value=\"([^\"]*)\"").Groups[1].Value);
    private static FormUrlEncodedContent Form(string token, params (string Key, string Value)[] fields) =>
        new(fields.Select(f => new KeyValuePair<string, string>(f.Key, f.Value)).Append(new("__RequestVerificationToken", token)));
}
