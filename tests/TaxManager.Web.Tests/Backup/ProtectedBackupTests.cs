using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.DataProtection;
using TaxManager.Core.Forecasting;
using TaxManager.Data.Audit;
using TaxManager.Data.Forecasting;
using TaxManager.Data.Protection;
using TaxManager.Web.Backup;
using TaxManager.Web.Demo;
using TaxManager.Web.MonthlySalary;
using Xunit;

namespace TaxManager.Web.Tests.Backup;

public sealed class ProtectedBackupTests
{
    private const string Password = "Synthetic backup mật khẩu 2026!";
    private static readonly DateTimeOffset Now = new(2026, 11, 28, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Whole_store_roundtrip_preserves_all_rows_keys_histories_rules_and_reopens_in_new_host()
    {
        string restoredRoot;
        string expected;
        Guid id;
        await using (var host = await DemoHost.StartAsync())
        {
            var source = host.Services.GetRequiredService<SyntheticDatabase>();
            id = await Seed(source);
            expected = Dump(source);
            var backup = host.Services.GetRequiredService<BackupStore>();
            var bytes = await backup.ExportAsync(source, Password, Now, default);
            Assert.Equal(expected, Dump(source)); // Backup is read-only, including audit.
            Assert.DoesNotContain("Nguồn mẫu", Encoding.UTF8.GetString(bytes));
            Assert.DoesNotContain(Convert.ToBase64String(JsonDocument.Parse(File.ReadAllText(Path.Combine(source.RootPath, "synthetic-key.json")))
                .RootElement.GetProperty("keys").GetProperty("synthetic").GetBytesFromBase64()), Encoding.UTF8.GetString(bytes));
            var (restored, summary) = await backup.ImportAsync(bytes, Password, default);
            using (restored)
            {
                restoredRoot = restored.RootPath;
                Assert.NotEqual(source.RootPath, restoredRoot);
                Assert.Equal(Now, summary.CreatedAt);
                Assert.Equal(2, summary.Profiles); // One forecast and one monthly-calculation profile.
                Assert.Equal(3, summary.Versions); // Includes deleted forecast's retained snapshot.
                Assert.Equal(12, summary.MonthlyResults);
                Assert.Equal(3, summary.AnnualResults); // Includes result for an ordinary-deleted forecast.
                Assert.True(summary.AuditEvents > 12);
                Assert.Equal(expected, Dump(restored));
                await using var check = restored.Open();
                check.ValidateProtection();
                var annual = await new AnnualForecastService(check).HistoryAsync(id);
                Assert.Equal(2, annual.Count);
                Assert.All(annual, result => Assert.Equal("Nguồn mẫu", ForecastDraftReader.Read(result.DraftPayload).Months[0].Gross!.Source));
                Assert.Equal("Giữ nguyên\nGhi chú", ForecastDraftReader.Read((await new ForecastStore(check).ReadAsync(id))!.Payload!).Months[0].Gross!.Note);
                Assert.All(await check.Set<AuditEvent>().ToArrayAsync(), row => Assert.NotNull(check.ReadAudit(row)));
            }
        }
        await using var reopened = await DemoHost.StartAsync(syntheticRoot: restoredRoot);
        var storage = reopened.Services.GetRequiredService<SyntheticDatabase>();
        Assert.Equal(expected, Dump(storage));
        using var client = new HttpClient { BaseAddress = reopened.Address };
        Assert.Contains("Báo cáo", WebUtility.HtmlDecode(await client.GetStringAsync($"/ho-so/{id}/bao-cao")));
    }

    [Fact]
    public void Envelope_is_randomized_authenticated_bounded_and_versioned()
    {
        var first = BackupEnvelope.Seal([1, 2, 3], [4, 5], Password, Now);
        var second = BackupEnvelope.Seal([1, 2, 3], [4, 5], Password, Now);
        Assert.NotEqual(first, second);
        using (var opened = BackupEnvelope.Open(first, Password)) Assert.Equal(new byte[] { 1, 2, 3 }, opened.Database);
        Assert.ThrowsAny<CryptographicException>(() => BackupEnvelope.Open(first, Password + "wrong"));
        foreach (var offset in new[] { 8, 24, 40, first.Length - 1 })
        {
            var tampered = first.ToArray(); tampered[offset] ^= 1;
            Assert.ThrowsAny<CryptographicException>(() => BackupEnvelope.Open(tampered, Password));
        }
        var version = first.ToArray(); version[7] = (byte)'2';
        Assert.Throws<InvalidDataException>(() => BackupEnvelope.Open(version, Password));
        Assert.Throws<InvalidDataException>(() => BackupEnvelope.Open(first[..20], Password));
        Assert.ThrowsAny<CryptographicException>(() => BackupEnvelope.Open(first[..^1], Password));
        Assert.Throws<InvalidDataException>(() => BackupEnvelope.Open(new byte[BackupEnvelope.MaxFileBytes + 1], Password));
        Assert.Throws<InvalidDataException>(() => BackupEnvelope.Seal([1], [2], "short", Now));
    }

    [Fact]
    public async Task Preview_is_read_only_owner_bound_one_use_and_switch_blocks_stale_forms()
    {
        await using var host = await DemoHost.StartAsync();
        var source = host.Services.GetRequiredService<SyntheticDatabase>();
        var id = await Seed(source);
        using var browser = new HttpClient { BaseAddress = host.Address };
        using var other = new HttpClient { BaseAddress = host.Address };
        var bytes = await Download(browser);
        // Simulate an edit after backup. Restoring the older same-ID store must not overwrite this.
        await using (var context = source.Open()) await new ForecastStore(context).UpdateAsync(id, 2, Payload(7));
        var original = Dump(source);
        var staleForm = await browser.GetStringAsync("/sao-luu");
        using (var upload = await Upload(browser, bytes, Password)) upload.EnsureSuccessStatusCode();
        var preview = await browser.GetStringAsync("/khoi-phuc/xem-truoc");
        Assert.Contains("2 hồ sơ có cùng mã", WebUtility.HtmlDecode(preview));
        Assert.Equal(original, Dump(source));
        using (var stolen = await Post(other, "/khoi-phuc/xem-truoc", preview)) Assert.Equal(HttpStatusCode.BadRequest, stolen.StatusCode);
        var otherForm = await other.GetStringAsync("/sao-luu");
        using (var wrongOwner = await Post(other, "/khoi-phuc/xem-truoc", otherForm, ("handle", Field(preview, "handle"))))
            Assert.Equal(HttpStatusCode.Gone, wrongOwner.StatusCode);
        using (var confirm = await Post(browser, "/khoi-phuc/xem-truoc", preview)) confirm.EnsureSuccessStatusCode();
        Assert.Equal(original, Dump(source));
        Assert.Contains("2 phiên bản", WebUtility.HtmlDecode(await browser.GetStringAsync("/ho-so")));
        Assert.Contains("3 phiên bản", WebUtility.HtmlDecode(await other.GetStringAsync("/ho-so")));
        using (var stale = await Post(browser, "/sao-luu", staleForm, ("password", Password), ("confirmation", Password)))
            Assert.Equal(HttpStatusCode.BadRequest, stale.StatusCode);
        using (var replay = await Post(browser, "/khoi-phuc/xem-truoc", preview)) Assert.Equal(HttpStatusCode.BadRequest, replay.StatusCode);
        using (var consumed = await browser.GetAsync("/khoi-phuc/xem-truoc")) Assert.Equal(HttpStatusCode.Gone, consumed.StatusCode);
        var restoredBytes = await Download(browser);
        using (var restored = (await host.Services.GetRequiredService<BackupStore>().ImportAsync(restoredBytes, Password, default)).Store)
        {
            await using var context = restored.Open();
            Assert.Equal(2, (await new ForecastStore(context).ReadAsync(id))!.Revision);
        }
        var returnForm = await browser.GetStringAsync("/sao-luu");
        using (var returned = await Post(browser, "/sao-luu?handler=Return", returnForm)) returned.EnsureSuccessStatusCode();
        Assert.Contains("3 phiên bản", WebUtility.HtmlDecode(await browser.GetStringAsync("/ho-so")));
        Assert.Equal(original, Dump(source));
    }

    [Fact]
    public async Task Cancel_expiry_bad_password_and_unprotected_posts_never_activate_a_store()
    {
        var clock = new TestClock();
        await using var host = await DemoHost.StartAsync(clock: clock);
        var source = host.Services.GetRequiredService<SyntheticDatabase>();
        await Seed(source);
        using var handler = new HttpClientHandler();
        using var browser = new HttpClient(handler) { BaseAddress = host.Address };
        var bytes = await Download(browser);
        var before = Dump(source);
        using (var wrong = await Upload(browser, bytes, Password + "wrong"))
        {
            Assert.Equal(HttpStatusCode.UnprocessableEntity, wrong.StatusCode);
            Assert.DoesNotContain(Password, await wrong.Content.ReadAsStringAsync());
        }
        using (var noCsrf = await browser.PostAsync("/sao-luu", new FormUrlEncodedContent(new Dictionary<string, string> { ["password"] = Password, ["confirmation"] = Password })))
            Assert.Equal(HttpStatusCode.BadRequest, noCsrf.StatusCode);
        using (var response = await Upload(browser, bytes, Password)) response.EnsureSuccessStatusCode();
        var preview = await browser.GetStringAsync("/khoi-phuc/xem-truoc");
        var state = Workspace(host, handler);
        var canceledRoot = state.Preview!.Store.RootPath;
        using (var canceled = await Post(browser, "/khoi-phuc/xem-truoc?handler=Cancel", preview)) canceled.EnsureSuccessStatusCode();
        Assert.False(Directory.Exists(canceledRoot));
        using (var gone = await browser.GetAsync("/khoi-phuc/xem-truoc")) Assert.Equal(HttpStatusCode.Gone, gone.StatusCode);
        using (var response = await Upload(browser, bytes, Password)) response.EnsureSuccessStatusCode();
        preview = await browser.GetStringAsync("/khoi-phuc/xem-truoc");
        var expiredRoot = state.Preview!.Store.RootPath;
        clock.Now = clock.Now.AddMinutes(11);
        using (var expired = await Post(browser, "/khoi-phuc/xem-truoc", preview)) Assert.Equal(HttpStatusCode.Gone, expired.StatusCode);
        Assert.False(Directory.Exists(expiredRoot));
        Assert.Equal(before, Dump(source));
        Assert.DoesNotContain("Đang dùng kho khôi phục", WebUtility.HtmlDecode(await browser.GetStringAsync("/ho-so")));
    }

    [Fact]
    public async Task Changed_staging_files_are_discarded_before_confirmation()
    {
        await using var host = await DemoHost.StartAsync();
        var source = host.Services.GetRequiredService<SyntheticDatabase>();
        var id = await Seed(source);
        using var handler = new HttpClientHandler();
        using var browser = new HttpClient(handler) { BaseAddress = host.Address };
        var bytes = await Download(browser);
        using (var response = await Upload(browser, bytes, Password)) response.EnsureSuccessStatusCode();
        var form = await browser.GetStringAsync("/khoi-phuc/xem-truoc");
        var state = Workspace(host, handler);
        var root = state.Preview!.Store.RootPath;
        var original = Dump(source);
        await using (var context = state.Preview.Store.Open()) await new ForecastStore(context).UpdateAsync(id, 2, Payload(99));
        using (var response = await Post(browser, "/khoi-phuc/xem-truoc", form)) Assert.Equal(HttpStatusCode.Gone, response.StatusCode);
        Assert.Null(state.Active);
        Assert.Null(state.Preview);
        Assert.False(Directory.Exists(root));
        Assert.Equal(original, Dump(source));
    }

    private static WorkspaceState Workspace(DemoHost host, HttpClientHandler handler)
    {
        var cookie = handler.CookieContainer.GetCookies(host.Address)["taxmanager.workspace"]!.Value;
        var owner = host.Services.GetRequiredService<IDataProtectionProvider>().CreateProtector("TaxManager.BackupWorkspace.v1").Unprotect(cookie);
        return host.Services.GetRequiredService<BackupWorkspaces>().Get(owner);
    }

    [Theory]
    [InlineData("sentinel")]
    [InlineData("payload")]
    [InlineData("audit")]
    [InlineData("schema")]
    [InlineData("migration")]
    [InlineData("catalog")]
    [InlineData("key")]
    public async Task Import_rejects_authenticated_but_invalid_database_without_repairing_it(string fault)
    {
        await using var host = await DemoHost.StartAsync();
        var source = host.Services.GetRequiredService<SyntheticDatabase>();
        await Seed(source);
        var backups = host.Services.GetRequiredService<BackupStore>();
        using var original = BackupEnvelope.Open(await backups.ExportAsync(source, Password, Now, default), Password);
        var file = Path.Combine(Path.GetTempPath(), "taxmanager-invalid-" + Guid.NewGuid().ToString("N") + ".db");
        byte[] bad;
        try
        {
            await File.WriteAllBytesAsync(file, original.Database);
            using (var db = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = file, Pooling = false }.ToString()))
            {
                await db.OpenAsync();
                using var command = db.CreateCommand();
                command.CommandText = fault switch
                {
                    "sentinel" => "DELETE FROM DataProtectionMetadata",
                    "payload" => "UPDATE ForecastDocuments SET PayloadEncrypted=randomblob(64)",
                    "audit" => "UPDATE AuditEvents SET Payload=randomblob(64)",
                    "schema" => "CREATE TABLE Unexpected(Value TEXT)",
                    "migration" => "DELETE FROM __EFMigrationsHistory WHERE MigrationId LIKE '%AddAnnualForecastResults'",
                    "catalog" => "UPDATE AnnualForecastRules SET Graph='{}'",
                    _ => "SELECT 1"
                };
                await command.ExecuteNonQueryAsync();
            }
            var keys = fault == "key" ? Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { activeKeyId = "synthetic", keys = new { synthetic = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) } })) : original.Keys;
            bad = BackupEnvelope.Seal(await File.ReadAllBytesAsync(file), keys, Password, Now);
        }
        finally { File.Delete(file); }
        var unchanged = Dump(source);
        await Assert.ThrowsAnyAsync<Exception>(() => backups.ImportAsync(bad, Password, default));
        Assert.Equal(unchanged, Dump(source));
    }

    [Fact]
    public async Task Empty_and_deleted_stores_remain_readable_and_export_refuses_missing_sentinel()
    {
        await using var host = await DemoHost.StartAsync();
        var source = host.Services.GetRequiredService<SyntheticDatabase>();
        var backups = host.Services.GetRequiredService<BackupStore>();
        var empty = await backups.ExportAsync(source, Password, Now, default);
        using (var restored = (await backups.ImportAsync(empty, Password, default)).Store)
            Assert.Equal(Dump(source), Dump(restored));
        Guid id;
        await using (var context = source.Open()) id = await new ForecastStore(context).CreateAsync(Payload(0));
        await using (var context = source.Open()) await new ForecastStore(context).DeleteAsync(id, 1);
        var deleted = await backups.ExportAsync(source, Password, Now, default);
        using (var restored = (await backups.ImportAsync(deleted, Password, default)).Store)
            Assert.Equal(Dump(source), Dump(restored));
        await using (var context = source.Open()) await context.Database.ExecuteSqlRawAsync("DELETE FROM DataProtectionMetadata");
        await Assert.ThrowsAsync<DataProtectionException>(() => backups.ExportAsync(source, Password, Now, default));
    }

    private static async Task<Guid> Seed(SyntheticDatabase store)
    {
        await new SalaryCalculator(store).CalculateAsync(2026, Enumerable.Range(1, 12).Select(_ => new SalaryInput(30_000_000, 0, 0)).ToArray());
        Guid id, deleted;
        await using (var context = store.Open()) id = await new ForecastStore(context).CreateAsync(Payload(0));
        await using (var context = store.Open()) await new ForecastStore(context).UpdateAsync(id, 1, Payload(1));
        await using (var context = store.Open()) await new AnnualForecastService(context).CalculateAsync(id, 2);
        await using (var context = store.Open()) await new AnnualForecastService(context).CalculateAsync(id, 2);
        await using (var context = store.Open()) deleted = await new ForecastStore(context).CreateAsync(Payload(0));
        await using (var context = store.Open()) await new AnnualForecastService(context).CalculateAsync(deleted, 1);
        await using (var context = store.Open()) await new ForecastStore(context).DeleteAsync(deleted, 1);
        return id;
    }
    private static string Payload(decimal gross) => ForecastDraftReader.Encode(new(2026, Enumerable.Range(1, 12).Select(m => new ForecastMonth(m,
        new(gross, ForecastStatus.Actual, "Nguồn mẫu", "Giữ nguyên\nGhi chú", Now),
        new(0, ForecastStatus.Planned, null, null, Now), new(1, ForecastStatus.Assumed, null, "Thử", Now),
        new(0, ForecastStatus.Actual, null, null, Now))).ToArray(), []));

    private static string Dump(SyntheticDatabase store)
    {
        using var context = store.Open();
        context.Database.OpenConnection();
        var db = context.Database.GetDbConnection();
        using var tables = db.CreateCommand();
        tables.CommandText = "SELECT name FROM sqlite_schema WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
        var names = new List<string>();
        using (var reader = tables.ExecuteReader()) while (reader.Read()) names.Add(reader.GetString(0));
        var dump = new StringBuilder();
        foreach (var name in names)
        {
            using var command = db.CreateCommand();
            command.CommandText = "SELECT * FROM \"" + name.Replace("\"", "\"\"") + "\"";
            using var reader = command.ExecuteReader();
            var rows = new List<string>();
            while (reader.Read()) rows.Add(JsonSerializer.Serialize(Enumerable.Range(0, reader.FieldCount).Select(reader.GetValue).ToArray()));
            dump.Append(name).Append(':').AppendJoin('|', rows.Order(StringComparer.Ordinal)).AppendLine();
        }
        return dump.ToString();
    }

    private static async Task<byte[]> Download(HttpClient client)
    {
        var form = await client.GetStringAsync("/sao-luu");
        using var response = await Post(client, "/sao-luu", form, ("password", Password), ("confirmation", Password));
        response.EnsureSuccessStatusCode();
        Assert.True(response.Headers.CacheControl!.NoStore);
        Assert.Equal("attachment", response.Content.Headers.ContentDisposition!.DispositionType);
        return await response.Content.ReadAsByteArrayAsync();
    }
    private static async Task<HttpResponseMessage> Upload(HttpClient client, byte[] file, string password)
    {
        var form = await client.GetStringAsync("/khoi-phuc");
        using var multipart = new MultipartFormDataContent();
        multipart.Add(new StringContent(Field(form, "__RequestVerificationToken")), "__RequestVerificationToken");
        multipart.Add(new StringContent(password), "password");
        multipart.Add(new ByteArrayContent(file), "upload", "ignored.taxbackup");
        return await client.PostAsync("/khoi-phuc", multipart);
    }
    private static Task<HttpResponseMessage> Post(HttpClient client, string path, string html, params (string Key, string Value)[] additions)
    {
        var fields = new Dictionary<string, string> { ["__RequestVerificationToken"] = Field(html, "__RequestVerificationToken") };
        var handle = Field(html, "handle");
        if (handle.Length > 0) fields["handle"] = handle;
        foreach (var field in additions) fields[field.Key] = field.Value;
        return client.PostAsync(path, new FormUrlEncodedContent(fields));
    }
    private static string Field(string html, string name) => WebUtility.HtmlDecode(Regex.Match(html, "<input\\b[^>]*name=\"" + Regex.Escape(name) + "\"[^>]*value=\"([^\"]*)\"").Groups[1].Value);
    private sealed class TestClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = ProtectedBackupTests.Now;
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
