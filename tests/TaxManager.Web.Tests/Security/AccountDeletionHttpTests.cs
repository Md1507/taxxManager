using System.Net;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaxManager.Web.Accounts;
using TaxManager.Web.Backup;
using TaxManager.Web.Demo;
using TaxManager.Data.Forecasting;
using Xunit;

namespace TaxManager.Web.Tests.Security;

public sealed class AccountDeletionHttpTests
{
    private const string Path = "/tai-khoan/xoa";
    private const string Password = "synthetic deletion password 123";

    [Fact]
    public async Task Get_never_deletes_and_post_requires_authentication_csrf_password_and_explicit_confirmation()
    {
        await using var host = await DemoHost.StartAsync(accounts: true);
        using var client = host.AccountClient();
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync(Path)).StatusCode);
        var user = await Account(host, "alice");
        await Login(client, "alice");
        var html = await client.GetStringAsync(Path);
        Assert.DoesNotContain("checked", Regex.Match(html, "<input[^>]*name=\"ConfirmDeletion\"[^>]*>").Value);
        Assert.True(Directory.Exists(user.StoreRoot));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync(Path, new FormUrlEncodedContent([]))).StatusCode);
        var token = Token(html);
        foreach (var fields in new[] {
            new[] { ("Password", Password) },
            new[] { ("ConfirmDeletion", "true") },
            new[] { ("Password", "wrong synthetic password"), ("ConfirmDeletion", "true") } })
        {
            using var rejected = await client.PostAsync(Path, Form(token, fields));
            Assert.Equal(HttpStatusCode.OK, rejected.StatusCode);
            var error = WebUtility.HtmlDecode(await rejected.Content.ReadAsStringAsync());
            Assert.DoesNotContain(Password, error);
            Assert.DoesNotContain("wrong synthetic password", error);
            Assert.DoesNotContain(user.StoreRoot, error);
            Assert.True(Directory.Exists(user.StoreRoot));
            Assert.NotNull(await User(host, "alice"));
        }
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/ho-so")).StatusCode);
    }

    [Fact]
    public async Task Deletes_only_current_account_and_its_versions_history_and_claims_invalidates_other_sessions_and_allows_fresh_name_reuse()
    {
        await using var host = await DemoHost.StartAsync(accounts: true);
        var alice = await Account(host, "alice");
        var bob = await Account(host, "bob");
        var shared = host.Services.GetRequiredService<SyntheticDatabase>().RootPath;
        var store = host.Services.GetRequiredService<LocalAccountStores>().Get(alice.Id, alice.StoreRoot);
        Guid saved;
        await using (var db = store.Open())
        {
            var draft = ForecastDraftReader.Encode(new(2026, Enumerable.Range(1, 12).Select(month => new TaxManager.Core.Forecasting.ForecastMonth(month,
                new(30000000, TaxManager.Core.Forecasting.ForecastStatus.Actual, "Synthetic", null, DateTimeOffset.UtcNow),
                new(0, TaxManager.Core.Forecasting.ForecastStatus.Actual, null, null, DateTimeOffset.UtcNow),
                new(0, TaxManager.Core.Forecasting.ForecastStatus.Actual, null, null, DateTimeOffset.UtcNow),
                new(0, TaxManager.Core.Forecasting.ForecastStatus.Actual, null, null, DateTimeOffset.UtcNow))).ToArray(), []));
            saved = await new ForecastStore(db).CreateAsync(draft);
        }
        await using (var db = store.Open())
        {
            await new AnnualForecastService(db).CalculateAsync(saved, 1);
            Assert.NotEmpty(await new AnnualForecastService(db).HistoryAsync(saved));
        }
        using var a = host.AccountClient(); using var otherSession = host.AccountClient(); using var b = host.AccountClient();
        await Login(a, "alice"); await Login(otherSession, "alice"); await Login(b, "bob");
        var stale = Token(await otherSession.GetStringAsync("/Forecast"));
        using var deleted = await a.PostAsync(Path, Form(Token(await a.GetStringAsync(Path)),
            ("Password", Password), ("ConfirmDeletion", "true"), ("UserId", bob.Id), ("StoreRoot", bob.StoreRoot)));
        Assert.Equal(HttpStatusCode.Redirect, deleted.StatusCode);
        Assert.Contains("deleted=True", deleted.Headers.Location!.OriginalString, StringComparison.OrdinalIgnoreCase);
        Assert.Null(await User(host, "alice"));
        Assert.NotNull(await User(host, "bob"));
        Assert.False(Directory.Exists(alice.StoreRoot));
        Assert.True(Directory.Exists(bob.StoreRoot)); Assert.True(Directory.Exists(shared));
        Assert.Equal(HttpStatusCode.OK, (await b.GetAsync("/ho-so")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await otherSession.GetAsync("/ho-so")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await a.PostAsync(Path, Form(stale, ("Password", Password), ("ConfirmDeletion", "true")))).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await otherSession.PostAsync("/Forecast?handler=Save", Form(stale))).StatusCode);
        using var scope = host.Services.CreateScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<LocalIdentityDb>().UserClaims.Where(c => c.UserId == alice.Id).ToArrayAsync());
        var replacement = await Account(host, "alice");
        Assert.NotEqual(alice.Id, replacement.Id); Assert.NotEqual(alice.StoreRoot, replacement.StoreRoot);
        using var fresh = host.AccountClient(); await Login(fresh, "alice");
        Assert.Contains("Chưa có hồ sơ", WebUtility.HtmlDecode(await fresh.GetStringAsync("/ho-so")));
    }

    [Fact]
    public async Task Legacy_unaccepted_account_can_delete_without_opening_or_decrypting_its_broken_store()
    {
        await using var host = await DemoHost.StartAsync(accounts: true);
        var user = await Account(host, "alice", accept: false);
        File.Delete(System.IO.Path.Combine(user.StoreRoot, "synthetic-key.json"));
        using var client = host.AccountClient(); await Login(client, "alice");
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/ho-so")).StatusCode);
        using var deleted = await client.PostAsync(Path, Form(Token(await client.GetStringAsync(Path)), ("Password", Password), ("ConfirmDeletion", "true")));
        Assert.Equal(HttpStatusCode.Redirect, deleted.StatusCode);
        Assert.Null(await User(host, "alice")); Assert.False(Directory.Exists(user.StoreRoot));
    }

    [Fact]
    public async Task Wrong_password_locks_out_deletion_without_touching_store()
    {
        await using var host = await DemoHost.StartAsync(accounts: true);
        var user = await Account(host, "alice");
        using var client = host.AccountClient(); await Login(client, "alice");
        var token = Token(await client.GetStringAsync(Path));
        for (var i = 0; i < 5; i++)
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsync(Path, Form(token, ("Password", "wrong synthetic password"), ("ConfirmDeletion", "true")))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync(Path, Form(token, ("Password", Password), ("ConfirmDeletion", "true")))).StatusCode);
        Assert.True(Directory.Exists(user.StoreRoot));
        Assert.NotNull((await User(host, "alice"))!.LockoutEnd);
    }

    [Fact]
    public async Task Foreign_or_shared_root_is_rejected_before_deletion_starts()
    {
        await using var host = await DemoHost.StartAsync(accounts: true);
        var alice = await Account(host, "alice"); var bob = await Account(host, "bob");
        using var client = host.AccountClient(); await Login(client, "alice");
        var token = Token(await client.GetStringAsync(Path));
        foreach (var root in new[] { bob.StoreRoot, host.Services.GetRequiredService<SyntheticDatabase>().RootPath, System.IO.Path.GetTempPath() })
        {
            using (var scope = host.Services.CreateScope())
            {
                var users = scope.ServiceProvider.GetRequiredService<UserManager<LocalUser>>();
                var user = (await users.FindByNameAsync("alice"))!; user.StoreRoot = root;
                Assert.True((await users.UpdateAsync(user)).Succeeded);
            }
            using var rejected = await client.PostAsync(Path, Form(token, ("Password", Password), ("ConfirmDeletion", "true")));
            Assert.Equal(HttpStatusCode.ServiceUnavailable, rejected.StatusCode);
            Assert.True(Directory.Exists(alice.StoreRoot)); Assert.True(Directory.Exists(bob.StoreRoot));
            using var check = host.Services.CreateScope();
            Assert.False(await check.ServiceProvider.GetRequiredService<AccountDeletion>().IsPendingAsync((await User(host, "alice"))!));
        }
    }

    [Fact]
    public async Task Failed_final_registry_delete_keeps_durable_intent_blocks_data_and_can_resume_after_new_host()
    {
        string shared; LocalUser user;
        await using (var first = await DemoHost.StartAsync(accounts: true))
        {
            shared = first.Services.GetRequiredService<SyntheticDatabase>().RootPath;
            user = await Account(first, "alice");
            using var client = first.AccountClient(); await Login(client, "alice");
            var stale = Token(await client.GetStringAsync("/Forecast"));
            using (var scope = first.Services.CreateScope())
                await scope.ServiceProvider.GetRequiredService<LocalIdentityDb>().Database.ExecuteSqlRawAsync(
                    "CREATE TRIGGER reject_delete BEFORE DELETE ON AspNetUsers BEGIN SELECT RAISE(ABORT, 'synthetic failure'); END");
            {
                using var failed = await client.PostAsync(Path, Form(Token(await client.GetStringAsync(Path)), ("Password", Password), ("ConfirmDeletion", "true")));
                Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
                Assert.DoesNotContain(user.StoreRoot, await failed.Content.ReadAsStringAsync());
                Assert.NotNull(await User(first, "alice"));
                var blocked = await client.GetAsync("/ho-so");
                Assert.Equal("/tai-khoan/xoa", blocked.Headers.Location!.OriginalString);
                Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync("/Forecast?handler=Save", Form(stale))).StatusCode);
                Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync("/tai-khoan/xuat-du-lieu", Form(stale))).StatusCode);
                Assert.False(Directory.Exists(user.StoreRoot));
            }
            using var cleanup = first.Services.CreateScope();
            await cleanup.ServiceProvider.GetRequiredService<LocalIdentityDb>().Database.ExecuteSqlRawAsync("DROP TRIGGER reject_delete");
        }
        await using var second = await DemoHost.StartAsync(accounts: true, syntheticRoot: shared);
        using var retry = second.AccountClient(); await Login(retry, "alice");
        var page = WebUtility.HtmlDecode(await retry.GetStringAsync(Path));
        Assert.Contains("Việc xóa chưa hoàn tất", page);
        using var completed = await retry.PostAsync(Path, Form(Token(page), ("Password", Password), ("ConfirmDeletion", "true")));
        Assert.Equal(HttpStatusCode.Redirect, completed.StatusCode);
        Assert.Null(await User(second, "alice")); Assert.False(Directory.Exists(user.StoreRoot));
    }

    [Fact]
    public async Task Failed_intent_write_keeps_user_files_and_access_unchanged()
    {
        await using var host = await DemoHost.StartAsync(accounts: true);
        var user = await Account(host, "alice");
        using var client = host.AccountClient(); await Login(client, "alice");
        using (var scope = host.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<LocalIdentityDb>().Database.ExecuteSqlRawAsync(
                "CREATE TRIGGER reject_intent BEFORE INSERT ON AspNetUserClaims BEGIN SELECT RAISE(ABORT, 'synthetic failure'); END");
        using var rejected = await client.PostAsync(Path, Form(Token(await client.GetStringAsync(Path)), ("Password", Password), ("ConfirmDeletion", "true")));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, rejected.StatusCode);
        Assert.True(File.Exists(System.IO.Path.Combine(user.StoreRoot, "synthetic-key.json")));
        Assert.NotNull(await User(host, "alice"));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/ho-so")).StatusCode);
    }

    [Fact]
    public async Task Deletes_own_active_restores_and_previews_across_browsers_but_leaves_other_accounts_restores()
    {
        await using var host = await DemoHost.StartAsync(accounts: true);
        var alice = await Account(host, "alice"); var bob = await Account(host, "bob");
        var source = host.Services.GetRequiredService<LocalAccountStores>().Get(alice.Id, alice.StoreRoot);
        var bytes = await host.Services.GetRequiredService<BackupStore>().ExportAsync(source, Password, DateTimeOffset.UtcNow, default);
        using var a = host.AccountClient(); using var previewBrowser = host.AccountClient(); using var b = host.AccountClient();
        await Login(a, "alice"); await Login(previewBrowser, "alice"); await Login(b, "bob");
        await Restore(a, bytes, activate: true);
        await Restore(previewBrowser, bytes, activate: false);
        await Restore(b, bytes, activate: true);
        var workspaces = host.Services.GetRequiredService<BackupWorkspaces>();
        var ownedRoots = workspaces.AccountRoots(alice.Id); var otherRoots = workspaces.AccountRoots(bob.Id);
        Assert.Equal(2, ownedRoots.Length); Assert.Single(otherRoots);
        using var deleted = await a.PostAsync(Path, Form(Token(await a.GetStringAsync(Path)), ("Password", Password), ("ConfirmDeletion", "true")));
        Assert.Equal(HttpStatusCode.Redirect, deleted.StatusCode);
        Assert.False(Directory.Exists(alice.StoreRoot));
        Assert.All(ownedRoots, root => Assert.False(Directory.Exists(root)));
        Assert.All(otherRoots, root => Assert.True(Directory.Exists(root)));
        Assert.True(Directory.Exists(bob.StoreRoot));
        Assert.Equal(HttpStatusCode.OK, (await b.GetAsync("/ho-so")).StatusCode);
        Assert.Empty(workspaces.AccountRoots(alice.Id));
    }

    private static async Task Restore(HttpClient client, byte[] bytes, bool activate)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(Token(await client.GetStringAsync("/khoi-phuc"))), "__RequestVerificationToken");
        form.Add(new StringContent(Password), "password");
        form.Add(new ByteArrayContent(bytes), "upload", "synthetic.taxbackup");
        Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/khoi-phuc", form)).StatusCode);
        if (!activate) return;
        var page = await client.GetStringAsync("/khoi-phuc/xem-truoc");
        var handle = WebUtility.HtmlDecode(Regex.Match(page, "name=\"handle\" value=\"([^\"]+)\"").Groups[1].Value);
        Assert.NotEmpty(handle);
        Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/khoi-phuc/xem-truoc", Form(Token(page), ("handle", handle)))).StatusCode);
    }

    [Fact]
    public async Task Account_deletion_waits_for_existing_account_request_gate_and_rechecks_user_after_waiting()
    {
        await using var host = await DemoHost.StartAsync(accounts: true);
        var user = await Account(host, "alice");
        using var client = host.AccountClient(); await Login(client, "alice");
        var gate = host.Services.GetRequiredService<LocalAccountStores>().Gate(user.Id);
        await gate.WaitAsync();
        var queued = client.GetAsync("/ho-so");
        try
        {
            Assert.NotSame(queued, await Task.WhenAny(queued, Task.Delay(100)));
            using var scope = host.Services.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<LocalUser>>();
            Assert.True((await users.DeleteAsync((await users.FindByIdAsync(user.Id))!)).Succeeded);
        }
        finally { gate.Release(); }
        Assert.Contains((await queued).StatusCode, new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Redirect });
    }

    [Fact]
    public async Task Deletion_form_from_previous_account_is_rejected_after_switching_account()
    {
        await using var host = await DemoHost.StartAsync(accounts: true);
        var alice = await Account(host, "alice"); var bob = await Account(host, "bob");
        using var client = host.AccountClient(); await Login(client, "alice");
        var stale = Token(await client.GetStringAsync(Path));
        Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/tai-khoan?handler=Logout", Form(Token(await client.GetStringAsync("/tai-khoan"))))).StatusCode);
        await Login(client, "bob");
        using var rejected = await client.PostAsync(Path, Form(stale, ("Password", Password), ("ConfirmDeletion", "true")));
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        Assert.True(Directory.Exists(alice.StoreRoot)); Assert.True(Directory.Exists(bob.StoreRoot));
    }

    [Fact]
    public async Task Concurrent_repeated_deletion_finishes_once_without_recreating_store()
    {
        await using var host = await DemoHost.StartAsync(accounts: true);
        var user = await Account(host, "alice");
        using var client = host.AccountClient(); await Login(client, "alice");
        var token = Token(await client.GetStringAsync(Path));
        var responses = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => client.PostAsync(Path,
            Form(token, ("Password", Password), ("ConfirmDeletion", "true")))));
        try
        {
            Assert.Contains(responses, response => response.Headers.Location?.OriginalString.Contains("deleted=", StringComparison.OrdinalIgnoreCase) == true);
            Assert.All(responses, response => Assert.Contains(response.StatusCode, new[] { HttpStatusCode.Redirect, HttpStatusCode.Unauthorized }));
            Assert.Null(await User(host, "alice")); Assert.False(Directory.Exists(user.StoreRoot));
        }
        finally { foreach (var response in responses) response.Dispose(); }
    }

    private static async Task<LocalUser> Account(DemoHost host, string name, bool accept = true)
    {
        using var scope = host.Services.CreateScope();
        using var store = new SyntheticDatabase(Directory.GetCurrentDirectory());
        var user = new LocalUser { UserName = name, StoreRoot = store.RootPath };
        var users = scope.ServiceProvider.GetRequiredService<UserManager<LocalUser>>();
        Assert.True((await users.CreateAsync(user, Password)).Succeeded);
        if (accept) Assert.True((await scope.ServiceProvider.GetRequiredService<LocalDataUse>().AcceptAsync(user)).Succeeded);
        return user;
    }
    private static async Task<LocalUser?> User(DemoHost host, string name)
    {
        using var scope = host.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<UserManager<LocalUser>>().FindByNameAsync(name);
    }
    private static async Task Login(HttpClient client, string name)
    {
        const string login = "/tai-khoan/dang-nhap";
        using var response = await client.PostAsync(login, Form(Token(await client.GetStringAsync(login)), ("Username", name), ("Password", Password)));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }
    private static string Token(string html) => WebUtility.HtmlDecode(Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value);
    private static FormUrlEncodedContent Form(string token, params (string Key, string Value)[] fields) =>
        new(fields.Select(field => new KeyValuePair<string, string>(field.Key, field.Value)).Append(new("__RequestVerificationToken", token)));
}
