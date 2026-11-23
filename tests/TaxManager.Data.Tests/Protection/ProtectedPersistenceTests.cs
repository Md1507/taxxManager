using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TaxManager.Data.Audit;
using TaxManager.Data.Infrastructure;
using TaxManager.Data.Protection;
using TaxManager.Data.Tests.Fixtures;
using Xunit;

namespace TaxManager.Data.Tests.Protection;

public class ProtectedPersistenceTests
{
    [Fact]
    public async Task NamesNullEmptyUnicodeDirtyDetectionNoTrackingProjectionAndReload()
    {
        using var db = new SqliteFileDatabase();
        await SyntheticGraph.SeedAsync(db);
        await using (var c = db.Open())
        {
            var p = await c.UserProfiles.SingleAsync();
            p.DisplayName = null; await c.SaveChangesAsync();
            p.DisplayName = ""; await c.SaveChangesAsync();
            p.DisplayName = "Synthetic Trần 文 🧪"; await c.SaveChangesAsync();
            c.ChangeTracker.Clear();
            Assert.Equal("Synthetic Trần 文 🧪", await c.UserProfiles.AsNoTracking().Select(x => x.DisplayName).SingleAsync());
            var income = await c.MonthlyIncomes.SingleAsync();
            income.GrossIncome = 0.0000000000000000000000000001m;
            await c.SaveChangesAsync();
            income.GrossIncome = 99;
            await c.Entry(income).ReloadAsync();
            Assert.Equal(0.0000000000000000000000000001m, income.GrossIncome);
        }
        // Reopen the durable key file; do not rely on the fixture's in-memory protector.
        using var keys = FileKeyRing.Load(db.KeyPath, Path.GetFullPath("."), Path.GetDirectoryName(db.DatabasePath)!);
        await using var reopened = Open(db.DatabasePath, keys);
        reopened.ValidateProtection();
        Assert.Equal("Synthetic Trần 文 🧪", (await reopened.UserProfiles.SingleAsync()).DisplayName);
    }

    [Theory]
    [InlineData("field")]
    [InlineData("row")]
    [InlineData("profile")]
    [InlineData("database")]
    [InlineData("empty")]
    [InlineData("corrupt")]
    public async Task SwappedOrDamagedCiphertextFailsClosed(string mode)
    {
        using var db = new SqliteFileDatabase();
        var a = await SyntheticGraph.SeedAsync(db);
        var b = await SyntheticGraph.SeedAsync(db);
        await using (var c = db.Open())
        {
            c.Add(SyntheticGraph.Income(a, 2));
            await c.SaveChangesAsync();
            var sql = mode switch
            {
                "field" => "UPDATE MonthlyIncomes SET GrossIncomeEncrypted=InsuranceAmountEncrypted",
                "row" => "UPDATE MonthlyIncomes SET GrossIncomeEncrypted=(SELECT GrossIncomeEncrypted FROM MonthlyIncomes WHERE Month=2) WHERE Month=1",
                "profile" => $"UPDATE MonthlyIncomes SET GrossIncomeEncrypted=(SELECT GrossIncomeEncrypted FROM MonthlyIncomes WHERE ProfileId='{b.ToString().ToUpperInvariant()}') WHERE ProfileId='{a.ToString().ToUpperInvariant()}'",
                "database" => "UPDATE DataProtectionMetadata SET DatabaseId='11111111-1111-1111-1111-111111111111'",
                "empty" => "UPDATE MonthlyIncomes SET GrossIncomeEncrypted=X''",
                _ => "UPDATE MonthlyIncomes SET GrossIncomeEncrypted=X'540101AA'"
            };
            await c.Database.ExecuteSqlRawAsync(sql);
        }
        await using var reader = db.Open();
        await Assert.ThrowsAsync<DataProtectionException>(() => reader.MonthlyIncomes.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task MissingWrongKeysAndMissingSentinelFailWithoutFallbackIncludingEmptyDatabase()
    {
        using var db = new SqliteFileDatabase(); using var other = new SqliteFileDatabase();
        var id = await SyntheticGraph.SeedAsync(db);
        await using (var wrong = Open(db.DatabasePath, other.Keys))
            await Assert.ThrowsAsync<DataProtectionException>(() => wrong.UserProfiles.ToListAsync());
        await using (var missing = Open(db.DatabasePath, null))
            await Assert.ThrowsAsync<DataProtectionException>(() => missing.MonthlyIncomes.ToListAsync());
        await using (var c = db.Open()) await new TaxManager.Data.Profiles.ProfileDeletionService(c).DeleteAsync(id);
        await using (var wrong = Open(db.DatabasePath, other.Keys)) Assert.Throws<DataProtectionException>(wrong.ValidateProtection);
        await using (var c = db.Open()) await c.Database.ExecuteSqlRawAsync("DELETE FROM DataProtectionMetadata");
        await using (var reader = db.Open()) Assert.Throws<DataProtectionException>(reader.ValidateProtection);
    }

    [Theory]
    [InlineData("Action")]
    [InlineData("TimestampUtc")]
    [InlineData("ProfileId")]
    [InlineData("Payload")]
    public async Task AuditHeadersAndPayloadAreAuthenticated(string column)
    {
        using var db = new SqliteFileDatabase(); await SyntheticGraph.SeedAsync(db);
        await using (var c = db.Open())
        {
            var sql = column switch
            {
                "Action" => "UPDATE AuditEvents SET Action='Forged'",
                "TimestampUtc" => "UPDATE AuditEvents SET TimestampUtc='2001-01-01 00:00:00+00:00'",
                "ProfileId" => "UPDATE AuditEvents SET ProfileId='11111111-1111-1111-1111-111111111111'",
                _ => "UPDATE AuditEvents SET Payload=X'540101AA'"
            };
            await c.Database.ExecuteSqlRawAsync(sql);
        }
        await using var reader = db.Open();
        var events = await reader.Set<AuditEvent>().AsNoTracking().ToListAsync();
        Assert.All(events, e => Assert.Throws<DataProtectionException>(() => reader.ReadAudit(e)));
    }

    [Fact]
    public async Task ActiveDatabaseWalAuditAndCapturedLogsContainNoSensitiveMarkers()
    {
        using var db = new SqliteFileDatabase();
        var logs = new List<string>();
        var options = new DbContextOptionsBuilder<TaxManagerDbContext>()
            .UseSqlite($"Data Source={db.DatabasePath};Foreign Keys=True;Pooling=False")
            .LogTo(logs.Add).Options;
        Guid id;
        await using (var c = new TaxManagerDbContext(options, db.Keys))
        {
            await c.Database.MigrateAsync();
            await c.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL");
            c.AuditContext = new("ACTOR_SECRET_849372", "CONTEXT_SECRET_238721", "CORRELATION_SECRET_538261");
            var p = new TaxManager.Data.Profiles.UserProfile { DisplayName = "NAME_SECRET_538261" };
            id = p.Id; c.AddRange(p, SyntheticGraph.Income(p.Id));
            await c.SaveChangesAsync();
            (await c.MonthlyIncomes.SingleAsync()).GrossIncome = 987654321.123456789m;
            await c.SaveChangesAsync();
            c.Remove(p); await c.SaveChangesAsync();
            var events = await c.Set<AuditEvent>().AsNoTracking().ToListAsync();
            Assert.Contains(events, e => c.ReadAudit(e).Actor == "ACTOR_SECRET_849372");
            foreach (var file in Directory.GetFiles(Path.GetDirectoryName(db.DatabasePath)!))
            {
                var bytes = File.ReadAllBytes(file);
                foreach (var marker in Markers)
                {
                    Assert.True(bytes.AsSpan().IndexOf(Encoding.UTF8.GetBytes(marker)) < 0);
                    Assert.True(bytes.AsSpan().IndexOf(Encoding.Unicode.GetBytes(marker)) < 0);
                }
            }
        }
        var log = string.Join("\n", logs);
        foreach (var marker in Markers) Assert.DoesNotContain(marker, log);
        Assert.DoesNotContain(File.ReadAllText(db.KeyPath), log);
    }

    private static readonly string[] Markers = ["NAME_SECRET_538261", "ACTOR_SECRET_849372", "CONTEXT_SECRET_238721",
        "CORRELATION_SECRET_538261", "1234567890.123456789", "987654321.123456789"];

    private static TaxManagerDbContext Open(string path, FileKeyRing? keys) => new(
        new DbContextOptionsBuilder<TaxManagerDbContext>().UseSqlite($"Data Source={path};Foreign Keys=True;Pooling=False").Options, keys);
}
