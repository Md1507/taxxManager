using Microsoft.EntityFrameworkCore;
using TaxManager.Data.Audit;
using TaxManager.Data.Infrastructure;
using TaxManager.Data.Profiles;
using TaxManager.Data.Tests.Fixtures;
using Xunit;

namespace TaxManager.Data.Tests.Audit;

public class AuditAtomicityTests
{
    [Theory]
    [InlineData(false, "AuditEvents")]
    [InlineData(true, "AuditEvents")]
    [InlineData(false, "MonthlyIncomes")]
    [InlineData(true, "MonthlyIncomes")]
    public async Task SqlFailureRollsBackBusinessAndAuditAndSameContextCanRetry(bool async, string table)
    {
        using var db = new SqliteFileDatabase();
        await using var c = db.Open();
        await c.Database.MigrateAsync();
        var faultSql = table == "AuditEvents"
            ? "CREATE TRIGGER SyntheticFailure BEFORE INSERT ON AuditEvents BEGIN SELECT RAISE(ABORT, 'synthetic failure'); END"
            : "CREATE TRIGGER SyntheticFailure BEFORE INSERT ON MonthlyIncomes BEGIN SELECT RAISE(ABORT, 'synthetic failure'); END";
        await c.Database.ExecuteSqlRawAsync(faultSql);
        var p = new UserProfile { DisplayName = "synthetic retry" };
        c.AddRange(p, SyntheticGraph.Income(p.Id));
        c.Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;
        c.Database.AutoSavepointsEnabled = false;
        if (async) await Assert.ThrowsAsync<DbUpdateException>(() => c.SaveChangesAsync());
        else Assert.Throws<DbUpdateException>(() => c.SaveChanges());
        await AssertEmpty(db);
        await c.Database.ExecuteSqlRawAsync("DROP TRIGGER SyntheticFailure");
        if (async) await c.SaveChangesAsync(); else c.SaveChanges();
        await using var verify = db.Open();
        Assert.Equal("synthetic retry", (await verify.UserProfiles.SingleAsync()).DisplayName);
        Assert.Equal(1234567890.123456789m, (await verify.MonthlyIncomes.SingleAsync()).GrossIncome);
        Assert.Equal(2, await verify.Set<AuditEvent>().CountAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CommitFaultAfterSqlRollsBackWithoutAcceptingTracker(bool async)
    {
        using var db = new SqliteFileDatabase();
        var fault = new CommitFaultInterceptor();
        await using var c = db.Open(fault);
        await c.Database.MigrateAsync();
        var p = new UserProfile();
        c.AddRange(p, SyntheticGraph.Income(p.Id));
        fault.Armed = true;
        if (async) await Assert.ThrowsAsync<InvalidOperationException>(() => c.SaveChangesAsync());
        else Assert.Throws<InvalidOperationException>(() => c.SaveChanges());
        Assert.Equal(EntityState.Added, c.Entry(p).State);
        await AssertEmpty(db);
        fault.Armed = false;
        await c.SaveChangesAsync();
        await using var verify = db.Open();
        Assert.Equal(2, await verify.Set<AuditEvent>().CountAsync());
    }

    [Fact]
    public async Task CancellationAfterSqlBeforeCommitRollsBackEveryTable()
    {
        using var db = new SqliteFileDatabase();
        using var source = new CancellationTokenSource();
        await using var c = db.Open(new CancelAfterSaveInterceptor(source));
        await c.Database.MigrateAsync();
        var p = new UserProfile();
        c.AddRange(p, SyntheticGraph.Income(p.Id));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => c.SaveChangesAsync(source.Token));
        await AssertEmpty(db);
        using var alreadyCanceled = new CancellationTokenSource();
        alreadyCanceled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => c.SaveChangesAsync(alreadyCanceled.Token));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task CallerCommitAndRollbackOwnBothBusinessAndAudit(bool commit, bool async)
    {
        using var db = new SqliteFileDatabase();
        var id = await SyntheticGraph.SeedAsync(db);
        await using (var c = db.Open())
        {
            await using var transaction = await c.Database.BeginTransactionAsync();
            (await c.MonthlyIncomes.SingleAsync()).GrossIncome = 8;
            if (async) await c.SaveChangesAsync(); else c.SaveChanges();
            Assert.Same(transaction, c.Database.CurrentTransaction);
            if (commit) await transaction.CommitAsync(); else await transaction.RollbackAsync();
        }
        await using var verify = db.Open();
        Assert.Equal(commit ? 8 : 1234567890.123456789m, (await verify.MonthlyIncomes.SingleAsync()).GrossIncome);
        Assert.Equal(commit ? 5 : 4, await verify.Set<AuditEvent>().CountAsync());
    }

    [Fact]
    public async Task FailedSaveRollsBackOnlyItsSavepointPreservingEarlierCallerWork()
    {
        using var db = new SqliteFileDatabase();
        await SyntheticGraph.SeedAsync(db);
        await using (var c = db.Open())
        {
            await c.Database.ExecuteSqlRawAsync("CREATE TRIGGER FailAudit BEFORE INSERT ON AuditEvents BEGIN SELECT RAISE(ABORT, 'synthetic'); END");
            await using var transaction = await c.Database.BeginTransactionAsync();
            await c.Database.ExecuteSqlRawAsync("CREATE TABLE EarlierCallerWork (Value TEXT)");
            await c.Database.ExecuteSqlRawAsync("INSERT INTO EarlierCallerWork VALUES ('kept')");
            (await c.MonthlyIncomes.SingleAsync()).GrossIncome = 8;
            await Assert.ThrowsAsync<DbUpdateException>(() => c.SaveChangesAsync());
            await transaction.CommitAsync();
        }
        await using var verify = db.Open();
        Assert.Equal(1234567890.123456789m, (await verify.MonthlyIncomes.SingleAsync()).GrossIncome);
        Assert.Equal(4, await verify.Set<AuditEvent>().CountAsync());
        await verify.Database.OpenConnectionAsync();
        await using var check = verify.Database.GetDbConnection().CreateCommand();
        check.CommandText = "SELECT Value FROM EarlierCallerWork";
        Assert.Equal("kept", await check.ExecuteScalarAsync());
    }

    [Fact]
    public async Task PurgeFailureAfterAuditDeleteRestoresAllPayloadsAndGraph()
    {
        using var db = new SqliteFileDatabase();
        var id = await SyntheticGraph.SeedAsync(db);
        await using (var c = db.Open())
        {
            await c.Database.ExecuteSqlRawAsync("CREATE TRIGGER FailPurge BEFORE DELETE ON UserProfiles BEGIN SELECT RAISE(ABORT, 'synthetic'); END");
            await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(() =>
                new ProfilePurgeService(c).PurgeAsync(id, new("synthetic", "test")));
        }
        await using var verify = db.Open();
        Assert.Single(await verify.UserProfiles.ToListAsync());
        Assert.Single(await verify.MonthlyIncomes.ToListAsync());
        Assert.Single(await verify.Dependents.ToListAsync());
        Assert.Single(await verify.DependentEligibilityPeriods.ToListAsync());
        var events = await verify.Set<AuditEvent>().AsNoTracking().ToListAsync();
        Assert.Equal(4, events.Count);
        Assert.All(events, e => Assert.NotNull(verify.ReadAudit(e).After));
    }

    private static async Task AssertEmpty(SqliteFileDatabase db)
    {
        await using var verify = db.Open();
        Assert.Empty(await verify.UserProfiles.ToListAsync()); Assert.Empty(await verify.MonthlyIncomes.ToListAsync());
        Assert.Empty(await verify.Dependents.ToListAsync()); Assert.Empty(await verify.DependentEligibilityPeriods.ToListAsync());
        Assert.Empty(await verify.Set<AuditEvent>().ToListAsync());
    }
}
