using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TaxManager.Data.Audit;
using TaxManager.Data.Profiles;
using TaxManager.Data.Protection;
using TaxManager.Data.Tests.Fixtures;
using Xunit;

namespace TaxManager.Data.Tests.Audit;

public class AuditRetryTests
{
    [Fact]
    public async Task UnknownCommitOutcomeIsReconciledWithStableOperationAndFreshContext()
    {
        using var db = new SqliteFileDatabase(); await SyntheticGraph.SeedAsync(db);
        var operation = Guid.NewGuid();
        await using (var c = db.Open(new AfterCommitFaultInterceptor()))
        {
            c.AuditContext = new("synthetic", "unknown-commit", OperationId: operation);
            (await c.MonthlyIncomes.SingleAsync()).GrossIncome = 5;
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => c.SaveChangesAsync());
            Assert.Contains("unknown", error.Message);
        }
        await using var verify = db.Open();
        verify.AuditContext = new("synthetic", "unknown-commit", OperationId: operation);
        Assert.Single(await verify.Set<AuditEvent>().Where(e => e.OperationId == operation).ToListAsync());
        var income = await verify.MonthlyIncomes.SingleAsync();
        Assert.Equal(5, income.GrossIncome);
        income.GrossIncome = 5; Assert.Equal(0, await verify.SaveChangesAsync());
        income.GrossIncome = 6;
        await Assert.ThrowsAsync<InvalidOperationException>(() => verify.SaveChangesAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CompetingWriterFailsBeforeCollectingStaleBeforeStateThenFreshRetrySucceeds(bool async)
    {
        using var db = new SqliteFileDatabase(); await SyntheticGraph.SeedAsync(db);
        var operation = Guid.NewGuid();
        await using var second = db.Open();
        ((SqliteConnection)second.Database.GetDbConnection()).DefaultTimeout = 1;
        var stale = await second.MonthlyIncomes.SingleAsync();
        stale.GrossIncome = 7;
        second.AuditContext = new("synthetic", "busy-retry", OperationId: operation);
        await using (var first = db.Open())
        {
            await using var lockOwner = await first.Database.BeginTransactionAsync();
            (await first.MonthlyIncomes.SingleAsync()).GrossIncome = 6;
            await first.SaveChangesAsync();
            if (async) await Assert.ThrowsAsync<SqliteException>(() => second.SaveChangesAsync());
            else Assert.Throws<SqliteException>(() => second.SaveChanges());
            await lockOwner.CommitAsync();
        }
        await using var fresh = db.Open();
        fresh.AuditContext = new("synthetic", "busy-retry", OperationId: operation);
        (await fresh.MonthlyIncomes.SingleAsync()).GrossIncome = 7;
        await fresh.SaveChangesAsync();
        var e = await fresh.Set<AuditEvent>().AsNoTracking().SingleAsync(e => e.OperationId == operation);
        Assert.Contains("\"6\"", fresh.ReadAudit(e).Before!.Value.GetRawText());
        Assert.Equal(6, await fresh.Set<AuditEvent>().CountAsync());
    }

    [Fact]
    public async Task CryptoFailurePartWayThroughBatchRollsBackAndRetainsCallerIntentForRetry()
    {
        using var db = new SqliteFileDatabase();
        await using var c = db.Open(); await c.Database.MigrateAsync();
        var first = new UserProfile { DisplayName = "Synthetic first" };
        var second = new UserProfile { DisplayName = new string('x', 4 * 1024 * 1024) };
        c.AddRange(first, second);
        await Assert.ThrowsAsync<DataProtectionException>(() => c.SaveChangesAsync());
        await using (var check = db.Open())
        {
            Assert.Empty(await check.UserProfiles.ToListAsync()); Assert.Empty(await check.Set<AuditEvent>().ToListAsync());
        }
        second.DisplayName = "Synthetic fixed";
        await c.SaveChangesAsync();
        await using var verify = db.Open();
        Assert.Equal(2, await verify.UserProfiles.CountAsync()); Assert.Equal(2, await verify.Set<AuditEvent>().CountAsync());
        Assert.Equal("Synthetic first", (await verify.UserProfiles.SingleAsync(p => p.Id == first.Id)).DisplayName);
    }
}
