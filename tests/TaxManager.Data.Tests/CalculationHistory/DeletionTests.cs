using Microsoft.EntityFrameworkCore;
using TaxManager.Data.Audit;
using TaxManager.Data.CalculationHistory;
using TaxManager.Data.Profiles;
using TaxManager.Data.Tests.Fixtures;
using Xunit;

namespace TaxManager.Data.Tests.CalculationHistory;

public class DeletionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OrdinaryDeletionRetainsUnloadedHistoryAndPurgeErasesAlreadyDeletedTargetOnly(bool service)
    {
        using var db = new SqliteFileDatabase();
        var id = await HistoryFixture.SeedAsync(db);
        var other = await SyntheticGraph.SeedAsync(db);
        var history = await HistoryFixture.CreateAsync(db, id);
        var otherHistory = await HistoryFixture.CreateAsync(db, other);
        await using (var c = db.Open())
        {
            if (service) await new ProfileDeletionService(c).DeleteAsync(id);
            else { c.Remove(await c.UserProfiles.SingleAsync(p => p.Id == id)); await c.SaveChangesAsync(); }
        }
        await using (var fresh = db.Open())
        {
            Assert.False(await fresh.UserProfiles.AnyAsync(p => p.Id == id));
            Assert.NotNull(await new CalculationHistoryService(fresh).ReadAsync(id, history.HistoryId));
            Assert.Null(await new CalculationHistoryService(fresh).ReadAsync(other, history.HistoryId));
            var audit = await fresh.Set<AuditEvent>().Where(e => e.ProfileId == id).ToListAsync();
            Assert.Equal(9, audit.Count);
            Assert.All(audit, e => Assert.NotNull(fresh.ReadAudit(e)));
        }
        await using (var purge = db.Open())
            await new ProfilePurgeService(purge).PurgeAsync(id, new("synthetic", "erase"));
        await using var verify = db.Open();
        Assert.False(await verify.CalculationHistory.AnyAsync(h => h.ProfileId == id));
        Assert.False(await verify.Set<AuditEvent>().AnyAsync(e => e.ProfileId == id));
        Assert.NotNull(await new CalculationHistoryService(verify).ReadAsync(other, otherHistory.HistoryId));
        Assert.Single(await verify.UserProfiles.ToListAsync());
        Assert.Equal(5, await verify.Set<AuditEvent>().CountAsync());
        verify.ValidateProtection();
    }

    [Theory]
    [InlineData("AuditEvents")]
    [InlineData("UserProfiles")]
    [InlineData("CalculationHistory")]
    public async Task FailedPurgeRestoresHistoryAuditAndBusiness(string table)
    {
        using var db = new SqliteFileDatabase();
        var id = await HistoryFixture.SeedAsync(db);
        var history = await HistoryFixture.CreateAsync(db, id);
        await using (var c = db.Open())
        {
            var sql = table switch
            {
                "AuditEvents" => "CREATE TRIGGER FailPurgeHistory BEFORE DELETE ON AuditEvents BEGIN SELECT RAISE(ABORT,'synthetic'); END",
                "UserProfiles" => "CREATE TRIGGER FailPurgeHistory BEFORE DELETE ON UserProfiles BEGIN SELECT RAISE(ABORT,'synthetic'); END",
                _ => "CREATE TRIGGER FailPurgeHistory BEFORE DELETE ON CalculationHistory BEGIN SELECT RAISE(ABORT,'synthetic'); END"
            };
            await c.Database.ExecuteSqlRawAsync(sql);
            await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(() => new ProfilePurgeService(c)
                .PurgeAsync(id, new("synthetic", "erase")));
        }
        await using var verify = db.Open();
        Assert.Single(await verify.UserProfiles.ToListAsync());
        Assert.NotNull(await new CalculationHistoryService(verify).ReadAsync(id, history.HistoryId));
        Assert.Equal(5, await verify.Set<AuditEvent>().CountAsync());
    }

    [Fact]
    public async Task ActiveTargetPurgeIncludesHistoryAndCallerRollbackRestoresEverything()
    {
        using var db = new SqliteFileDatabase();
        var id = await HistoryFixture.SeedAsync(db);
        await HistoryFixture.CreateAsync(db, id);
        await using (var c = db.Open())
        {
            await using var transaction = await c.Database.BeginTransactionAsync();
            await new ProfilePurgeService(c).PurgeAsync(id, new("synthetic", "erase"));
            Assert.Empty(await c.CalculationHistory.ToListAsync());
            Assert.Empty(await c.Set<AuditEvent>().ToListAsync());
            await transaction.RollbackAsync();
        }
        await using (var verify = db.Open())
        {
            Assert.Single(await verify.CalculationHistory.ToListAsync());
            Assert.Equal(5, await verify.Set<AuditEvent>().CountAsync());
        }
        await using (var c = db.Open()) await new ProfilePurgeService(c).PurgeAsync(id, new("synthetic", "erase"));
        await using var final = db.Open();
        Assert.Empty(await final.CalculationHistory.ToListAsync()); Assert.Empty(await final.Set<AuditEvent>().ToListAsync());
        Assert.Empty(await final.UserProfiles.ToListAsync()); final.ValidateProtection();
    }

    [Fact]
    public async Task CancellationAfterHistoryDeleteRestoresHistoryAndAudit()
    {
        using var db = new SqliteFileDatabase();
        var id = await HistoryFixture.SeedAsync(db);
        await HistoryFixture.CreateAsync(db, id);
        using var source = new CancellationTokenSource();
        await using (var c = db.Open(new CancelHistoryPurgeInterceptor(source)))
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new ProfilePurgeService(c)
                .PurgeAsync(id, new("synthetic", "erase"), source.Token));
        await using var verify = db.Open();
        Assert.Single(await verify.CalculationHistory.ToListAsync());
        Assert.Equal(5, await verify.Set<AuditEvent>().CountAsync());
        Assert.Single(await verify.UserProfiles.ToListAsync());
    }

    [Fact]
    public async Task DisabledForeignKeysRejectHistoryCreationAndPurgeWithoutChanges()
    {
        using var db = new SqliteFileDatabase();
        var id = await HistoryFixture.SeedAsync(db);
        await HistoryFixture.CreateAsync(db, id);
        await using (var c = db.Open())
        {
            await c.Database.OpenConnectionAsync();
            await c.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys=OFF");
            await Assert.ThrowsAsync<InvalidOperationException>(() => new CalculationHistoryService(c)
                .CreateMonthlyAsync(id, 2026, 1, CalculationSourceMode.References));
            await Assert.ThrowsAsync<InvalidOperationException>(() => new ProfilePurgeService(c)
                .PurgeAsync(id, new("synthetic", "erase")));
        }
        await using var verify = db.Open();
        Assert.Single(await verify.CalculationHistory.ToListAsync());
        Assert.Equal(5, await verify.Set<AuditEvent>().CountAsync());
    }
}
