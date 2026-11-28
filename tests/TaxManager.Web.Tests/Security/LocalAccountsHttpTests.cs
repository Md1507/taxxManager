using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaxManager.Data.Forecasting;
using TaxManager.Web.Accounts;
using TaxManager.Web.Backup;
using TaxManager.Web.Demo;
using Xunit;

namespace TaxManager.Web.Tests.Security;

public sealed class LocalAccountsHttpTests
{
    private const string Password = "synthetic test password 123";
    private const string LoginPath = "/tai-khoan/dang-nhap";
    private const string RegisterPath = "/tai-khoan/dang-ky";

    [Fact]
    public async Task Anonymous_cannot_open_or_post_data_and_account_routes_are_opt_in()
    {
        await using (var demo = await DemoHost.StartAsync())
        {
            using var client = new HttpClient { BaseAddress = demo.Address };
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(LoginPath)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/ho-so")).StatusCode);
        }
        await using var host = await DemoHost.StartAsync(accounts: true);
        using var anonymous = host.AccountClient();
        var id = Guid.NewGuid();
        foreach (var path in Routes(id).Concat(["/ho-so", "/", "/sao-luu", "/khoi-phuc", "/khoi-phuc/xem-truoc", "/tai-khoan"]))
        {
            using var response = await anonymous.GetAsync(path);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains(LoginPath, response.Headers.Location!.OriginalString);
        }
        using var post = await anonymous.PostAsync("/Forecast?handler=Save", new FormUrlEncodedContent([]));
        Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync("/site.css")).StatusCode);
        Assert.Contains("autocomplete=\"current-password\"", await anonymous.GetStringAsync(LoginPath));
    }

    [Fact]
    public async Task Register_login_cookie_local_redirect_and_post_only_logout_use_framework_security()
    {
        await using var host = await DemoHost.StartAsync(accounts: true);
        using var client = host.AccountClient();
        await Register(client, "alice");
        using var login = await Login(client, "alice", returnUrl: "https://outside.invalid/");
        Assert.Equal("/ho-so", login.Headers.Location!.OriginalString);
        var cookie = Assert.Single(login.Headers.GetValues("Set-Cookie"), c => c.StartsWith("__Host-taxmanager.identity="));
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("expires=", cookie, StringComparison.OrdinalIgnoreCase);
        var account = await client.GetStringAsync("/tai-khoan?handler=Logout");
        Assert.Contains("alice", account);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/ho-so")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/tai-khoan?handler=Logout", new FormUrlEncodedContent([]))).StatusCode);
        await Logout(client);
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/ho-so")).StatusCode);
        Assert.DoesNotContain("asp-", account);
    }

    [Fact]
    public async Task Invalid_credentials_do_not_echo_passwords_and_failed_logins_lock_out()
    {
        await using var host = await DemoHost.StartAsync(accounts: true);
        using var client = host.AccountClient();
        await Register(client, "alice");
        var csrf = Token(await client.GetStringAsync(LoginPath));
        for (var i = 0; i < 5; i++)
        {
            using var result = await client.PostAsync(LoginPath, Form(csrf, ("Username", "alice"), ("Password", "wrong synthetic password")));
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            var html = await result.Content.ReadAsStringAsync();
            Assert.DoesNotContain("wrong synthetic password", html);
            Assert.Contains("error-summary", html);
        }
        using var locked = await Login(client, "alice");
        Assert.Equal(HttpStatusCode.OK, locked.StatusCode);
        Assert.DoesNotContain("__Host-taxmanager.identity=", string.Join(";", locked.Headers.TryGetValues("Set-Cookie", out var values) ? values : []));
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/ho-so")).StatusCode);
        using var scope = host.Services.CreateScope();
        var user = await scope.ServiceProvider.GetRequiredService<UserManager<LocalUser>>().FindByNameAsync("alice");
        Assert.NotNull(user!.LockoutEnd);
    }

    [Fact]
    public async Task Registration_validation_csrf_and_duplicate_do_not_create_an_account_or_echo_secrets()
    {
        await using var host = await DemoHost.StartAsync(accounts: true);
        using var client = host.AccountClient();
        using var csrf = await client.PostAsync(RegisterPath, Form("", ("Username", "alice"), ("Password", Password), ("ConfirmPassword", Password)));
        Assert.Equal(HttpStatusCode.BadRequest, csrf.StatusCode);
        using var invalid = await client.PostAsync(RegisterPath, Form(Token(await client.GetStringAsync(RegisterPath)),
            ("Username", "<bad>"), ("Password", "secret-short"), ("ConfirmPassword", "different-secret")));
        var html = await invalid.Content.ReadAsStringAsync();
        Assert.DoesNotContain("secret-short", html);
        Assert.DoesNotContain("different-secret", html);
        Assert.Contains("error-summary", html);
        await Register(client, "alice");
        using var duplicate = await client.PostAsync(RegisterPath, Form(Token(await client.GetStringAsync(RegisterPath)),
            ("Username", "ALICE"), ("Password", Password), ("ConfirmPassword", Password), ("AcceptDataUse", "true")));
        Assert.Equal(HttpStatusCode.OK, duplicate.StatusCode);
        Assert.DoesNotContain(Password, await duplicate.Content.ReadAsStringAsync());
        using var scope = host.Services.CreateScope();
        var user = Assert.Single(await scope.ServiceProvider.GetRequiredService<LocalIdentityDb>().Users.ToArrayAsync());
        Assert.NotEqual(Password, user.PasswordHash);
    }

    [Fact]
    public async Task Two_accounts_cannot_read_or_mutate_each_others_profiles_history_reports_or_comparisons()
    {
        await using var host = await DemoHost.StartAsync(accounts: true);
        using var a = host.AccountClient();
        using var b = host.AccountClient();
        await Register(a, "alice"); await Register(b, "bob");
        Assert.Equal(HttpStatusCode.Redirect, (await Login(a, "alice")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await Login(b, "bob")).StatusCode);
        var idA = await NewProfile(a, "31000000", "Nguồn bí mật A");
        var idB = await NewProfile(b, "42000000", "Nguồn bí mật B");
        var storeA = await Store(host, "alice");
        await using (var context = storeA.Open()) await new AnnualForecastService(context).CalculateAsync(idA, 1);
        var before = await State(storeA, idA);
        foreach (var path in Routes(idA))
        {
            using var response = await b.GetAsync(path);
            var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
            Assert.DoesNotContain("Nguồn bí mật A", html);
            Assert.DoesNotContain("31.000.000", html);
            Assert.DoesNotContain("text/csv", response.Content.Headers.ContentType?.ToString() ?? "");
        }
        Assert.DoesNotContain(idA.ToString(), await b.GetStringAsync("/ho-so"));
        Assert.DoesNotContain(idB.ToString(), await a.GetStringAsync("/ho-so"));
        // B's valid antiforgery token cannot turn A's submitted ID into an owned resource.
        var token = Token(await b.GetStringAsync("/Forecast"));
        foreach (var handler in new[] { "Save", "Calculate" })
        {
            using var response = await b.PostAsync("/Forecast?handler=" + handler,
                ProfileForm(token, "99999999", "Nguồn B giả ID A", idA));
            Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        }
        using (var deleted = await b.PostAsync("/ForecastDelete", Form(token, ("Id", idA.ToString()), ("Revision", "1"))))
            Assert.NotEqual(HttpStatusCode.InternalServerError, deleted.StatusCode);
        using (var scenario = await b.PostAsync($"/ho-so/{idA}/kich-ban", Form(token, ("Revision", "1"), ("BonusAmount", "100"), ("BonusMonth", "6"))))
            Assert.Equal(HttpStatusCode.NotFound, scenario.StatusCode);
        using (var reconcile = await b.PostAsync($"/ho-so/{idA}/doi-chieu", Form(token, ("Revision", "1"), ("Source", "Company"),
            ("SourceName", "B"), ("SourceYear", "2026"), ("FromMonth", "1"), ("ToMonth", "12"), ("Metric", "Gross"), ("Amount", "0"), ("Status", "Actual"))))
            Assert.Equal(HttpStatusCode.NotFound, reconcile.StatusCode);
        Assert.Equal(before, await State(storeA, idA));
        await using var checkB = (await Store(host, "bob")).Open();
        Assert.Null(await new ForecastStore(checkB).ReadAsync(idA));
        Assert.Single(await new ForecastStore(checkB).ListAsync());
    }

    [Fact]
    public async Task Switching_account_in_same_browser_rejects_old_forms_and_backup_contains_only_current_store()
    {
        await using var host = await DemoHost.StartAsync(accounts: true);
        using var client = host.AccountClient();
        await Register(client, "alice"); await Register(client, "bob");
        await Login(client, "alice");
        var idA = await NewProfile(client, "31000000", "A");
        var oldToken = Token(await client.GetStringAsync("/Forecast"));
        await Logout(client); await Login(client, "bob");
        using var stale = await client.PostAsync("/Forecast?handler=Save", ProfileForm(oldToken, "0", "Stale"));
        Assert.Equal(HttpStatusCode.BadRequest, stale.StatusCode);
        var idB = await NewProfile(client, "42000000", "B");
        using var backup = await client.PostAsync("/sao-luu", Form(Token(await client.GetStringAsync("/sao-luu")), ("password", Password), ("confirmation", Password)));
        backup.EnsureSuccessStatusCode();
        var imported = await host.Services.GetRequiredService<BackupStore>().ImportAsync(await backup.Content.ReadAsByteArrayAsync(), Password, default);
        using (imported.Store)
        {
            Assert.Equal([idB], imported.Summary.ProfileIds);
            Assert.DoesNotContain(idA, imported.Summary.ProfileIds);
        }
    }

    [Fact]
    public async Task Restore_selection_and_preview_are_bound_to_account_even_when_browser_cookie_is_reused()
    {
        await using var host = await DemoHost.StartAsync(accounts: true);
        using var client = host.AccountClient();
        await Register(client, "alice"); await Register(client, "bob"); await Login(client, "alice");
        var id = await NewProfile(client, "31000000", "A");
        var bytes = await host.Services.GetRequiredService<BackupStore>().ExportAsync(await Store(host, "alice"), Password, DateTimeOffset.UtcNow, default);
        using var upload = new MultipartFormDataContent();
        upload.Add(new StringContent(Token(await client.GetStringAsync("/khoi-phuc"))), "__RequestVerificationToken");
        upload.Add(new StringContent(Password), "password");
        upload.Add(new ByteArrayContent(bytes), "upload", "own.taxbackup");
        var response = await client.PostAsync("/khoi-phuc", upload);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var previewPath = response.Headers.Location!.OriginalString;
        var preview = await client.GetStringAsync(previewPath);
        var oldToken = Token(preview);
        var handle = Value(preview, "handle");
        Assert.NotEmpty(handle);
        await Logout(client); await Login(client, "bob");
        Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync(previewPath)).StatusCode);
        using var stolen = await client.PostAsync(previewPath, Form(Token(await client.GetStringAsync("/Forecast")), ("handle", handle)));
        Assert.Equal(HttpStatusCode.Gone, stolen.StatusCode);
        await Logout(client); await Login(client, "alice");
        var ownPreview = await client.GetStringAsync(previewPath);
        using var accepted = await client.PostAsync(previewPath, Form(Token(ownPreview), ("handle", handle)));
        Assert.Equal(HttpStatusCode.Redirect, accepted.StatusCode);
        Assert.Contains(id.ToString(), await client.GetStringAsync("/ho-so"));
        await Logout(client); await Login(client, "bob");
        Assert.DoesNotContain(id.ToString(), await client.GetStringAsync("/ho-so"));
        Assert.DoesNotContain("Đang dùng kho khôi phục", WebUtility.HtmlDecode(await client.GetStringAsync("/sao-luu")));
    }

    [Fact]
    public async Task New_host_reopens_only_own_store_and_missing_key_never_creates_replacement()
    {
        string root, userRoot; Guid id;
        await using (var first = await DemoHost.StartAsync(accounts: true))
        {
            root = first.Services.GetRequiredService<SyntheticDatabase>().RootPath;
            using var client = first.AccountClient();
            await Register(client, "alice"); await Login(client, "alice");
            id = await NewProfile(client, "31000000", "Persistent A");
            userRoot = (await Store(first, "alice")).RootPath;
        }
        await using (var second = await DemoHost.StartAsync(syntheticRoot: root, accounts: true))
        {
            using var client = second.AccountClient();
            Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/ho-so")).StatusCode);
            await Login(client, "alice");
            Assert.Contains(id.ToString(), await client.GetStringAsync("/ho-so"));
            Assert.Equal(userRoot, (await Store(second, "alice")).RootPath);
        }
        File.Delete(System.IO.Path.Combine(userRoot, "synthetic-key.json"));
        await using var broken = await DemoHost.StartAsync(syntheticRoot: root, accounts: true);
        using var reader = broken.AccountClient();
        await Login(reader, "alice");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await reader.GetAsync("/ho-so")).StatusCode);
        Assert.False(File.Exists(System.IO.Path.Combine(userRoot, "synthetic-key.json")));
    }

    [Fact]
    public async Task Changed_security_stamp_and_tampered_identity_cookie_are_rejected()
    {
        await using var host = await DemoHost.StartAsync(accounts: true);
        var cookies = new CookieContainer();
        using var client = host.AccountClient(cookies);
        await Register(client, "alice"); await Login(client, "alice");
        using (var scope = host.Services.CreateScope())
        {
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<LocalUser>>();
            Assert.True((await manager.UpdateSecurityStampAsync((await manager.FindByNameAsync("alice"))!)).Succeeded);
        }
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/ho-so")).StatusCode);
        await Login(client, "alice");
        var cookie = cookies.GetCookies(host.Address)["__Host-taxmanager.identity"]!;
        cookies.Add(host.Address, new Cookie(cookie.Name, "tampered", "/") { Secure = true, HttpOnly = true });
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/ho-so")).StatusCode);
    }

    [Fact]
    public async Task Account_entry_is_rate_limited_and_plain_http_cannot_process_credentials()
    {
        await using var host = await DemoHost.StartAsync(accounts: true);
        using var client = host.AccountClient();
        using var http = new HttpClient { BaseAddress = new UriBuilder(host.Address) { Scheme = "http" }.Uri };
        // A TLS listener may close the connection before it can produce an HTTP 400.
        try
        {
            using var plain = await http.PostAsync(LoginPath, Form("", ("Username", "alice"), ("Password", Password)));
            Assert.Equal(HttpStatusCode.BadRequest, plain.StatusCode);
        }
        catch (HttpRequestException ex) { Assert.IsAssignableFrom<IOException>(ex.InnerException); }
        for (var i = 0; i < 20; i++) Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(LoginPath)).StatusCode);
        using var limited = await client.GetAsync(LoginPath);
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
    }

    [Fact]
    public async Task Missing_registry_never_silently_reinitializes_accounts()
    {
        string root;
        await using (var first = await DemoHost.StartAsync(accounts: true))
        {
            root = first.Services.GetRequiredService<SyntheticDatabase>().RootPath;
            using var client = first.AccountClient();
            await Register(client, "alice");
        }
        var registry = System.IO.Path.Combine(root, "accounts", "identity.db");
        File.Delete(registry);
        await Assert.ThrowsAsync<InvalidOperationException>(() => DemoHost.StartAsync(syntheticRoot: root, accounts: true));
        Assert.False(File.Exists(registry));
    }

    private static IEnumerable<string> Routes(Guid id) => [ $"/Forecast?id={id}", $"/ho-so/{id}/thang/1?revision=1",
        $"/ForecastAnnualHistory?id={id}", $"/ForecastAnnualHistory?id={id}&record={Guid.NewGuid()}",
        $"/ho-so/{id}/bao-cao?revision=1", $"/ho-so/{id}/bao-cao?handler=Csv&revision=1", $"/ho-so/{id}/kich-ban", $"/ho-so/{id}/doi-chieu" ];
    private static async Task Register(HttpClient client, string name)
    {
        using var response = await client.PostAsync(RegisterPath, Form(Token(await client.GetStringAsync(RegisterPath)),
            ("Username", name), ("Password", Password), ("ConfirmPassword", Password), ("AcceptDataUse", "true")));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }
    private static async Task<HttpResponseMessage> Login(HttpClient client, string name, string? returnUrl = null) =>
        await client.PostAsync(LoginPath, Form(Token(await client.GetStringAsync(LoginPath)),
            ("Username", name), ("Password", Password), ("ReturnUrl", returnUrl ?? "")));
    private static async Task Logout(HttpClient client)
    {
        using var response = await client.PostAsync("/tai-khoan?handler=Logout", Form(Token(await client.GetStringAsync("/tai-khoan"))));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }
    private static string Token(string html) => Value(html, "__RequestVerificationToken");
    private static string Value(string html, string name) => WebUtility.HtmlDecode(Regex.Match(
        Regex.Match(html, $"<input\\b[^>]*name=\"{Regex.Escape(name)}\"[^>]*>").Value, "value=\"([^\"]*)\"").Groups[1].Value);
    private static FormUrlEncodedContent Form(string token, params (string Key, string Value)[] fields) =>
        new(fields.Select(f => new KeyValuePair<string, string>(f.Key, f.Value)).Append(new("__RequestVerificationToken", token)));
    private static FormUrlEncodedContent ProfileForm(string token, string gross, string source, Guid? id = null)
    {
        var fields = new List<(string, string)> { ("FromMonth", "1"), ("ToMonth", "12"), ("Gross", gross), ("Status", "Actual"), ("Source", source) };
        if (id is not null) { fields.Add(("ProfileId", id.ToString()!)); fields.Add(("Revision", "1")); }
        for (var i = 0; i < 12; i++)
        {
            fields.Add(($"Exceptions[{i}].Month", (i + 1).ToString()));
            foreach (var field in new[] { "Insurance", "Dependents", "Withheld" }) fields.Add(($"Exceptions[{i}].{field}", "0"));
        }
        return Form(token, fields.ToArray());
    }
    private static async Task<Guid> NewProfile(HttpClient client, string gross, string source)
    {
        using var response = await client.PostAsync("/Forecast?handler=Save", ProfileForm(Token(await client.GetStringAsync("/Forecast")), gross, source));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var saved = await client.GetStringAsync(response.Headers.Location!);
        return Guid.Parse(Value(saved, "ProfileId"));
    }
    private static async Task<SyntheticDatabase> Store(DemoHost host, string name)
    {
        using var scope = host.Services.CreateScope();
        var user = await scope.ServiceProvider.GetRequiredService<UserManager<LocalUser>>().FindByNameAsync(name);
        return host.Services.GetRequiredService<LocalAccountStores>().Get(user!.Id, user.StoreRoot);
    }
    private static async Task<string> State(SyntheticDatabase store, Guid id)
    {
        await using var context = store.Open();
        var document = (await new ForecastStore(context).ReadAsync(id))!;
        return document.Payload + ":" + document.Revision + ":" + (await new ForecastStore(context).HistoryAsync(id)).Count + ":" +
            (await new AnnualForecastService(context).HistoryAsync(id)).Count + ":" + await context.Set<TaxManager.Data.Audit.AuditEvent>().CountAsync();
    }
}
