using Microsoft.EntityFrameworkCore;
using TaxManager.Data.Audit;
using TaxManager.Data.CalculationHistory;
using TaxManager.Data.Tests.Fixtures;
using Xunit;

namespace TaxManager.Data.Tests.CalculationHistory;

public class AtomicityTests
{
    [Theory]
    [InlineData("CalculationHistory", false)]
    [InlineData("AuditEvents", false)]
    [InlineData("CalculationHistory", true)]
    [InlineData("AuditEvents", true)]
    public async Task SqlFaultRollsBackWholeOperationAndCallerSavepointPreservesEarlierWork(string table, bool caller)
    {
        using var db = new SqliteFileDatabase();
        var id = await HistoryFixture.SeedAsync(db);
        await using (var c = db.Open())
        {
            await c.Database.ExecuteSqlRawAsync(table == "AuditEvents"
                ? "CREATE TRIGGER FailHistory BEFORE INSERT ON AuditEvents BEGIN SELECT RAISE(ABORT,'synthetic'); END"
                : "CREATE TRIGGER FailHistory BEFORE INSERT ON CalculationHistory BEGIN SELECT RAISE(ABORT,'synthetic'); END");
            await using var transaction = caller ? await c.Database.BeginTransactionAsync() : null;
            if (caller)
            {
                await c.Database.ExecuteSqlRawAsync("CREATE TABLE EarlierHistoryWork (Value TEXT)");
                await c.Database.ExecuteSqlRawAsync("INSERT INTO EarlierHistoryWork VALUES ('kept')");
            }
            await Assert.ThrowsAsync<DbUpdateException>(() => new CalculationHistoryService(c)
                .CreateMonthlyAsync(id, 2026, 1, CalculationSourceMode.References));
            Assert.Empty(c.ChangeTracker.Entries<CalculationRecord>());
            Assert.Empty(c.ChangeTracker.Entries<AuditEvent>());
            if (caller)
            {
                Assert.Same(transaction, c.Database.CurrentTransaction);
                await transaction!.CommitAsync();
            }
        }
        await using var fresh = db.Open();
        Assert.Empty(await fresh.CalculationHistory.ToListAsync());
        Assert.Equal(4, await fresh.Set<AuditEvent>().CountAsync());
        Assert.Equal(1234567890.123456789m, (await fresh.MonthlyIncomes.SingleAsync()).GrossIncome);
        if (caller)
        {
            await fresh.Database.OpenConnectionAsync();
            await using var check = fresh.Database.GetDbConnection().CreateCommand();
            check.CommandText = "SELECT Value FROM EarlierHistoryWork";
            Assert.Equal("kept", await check.ExecuteScalarAsync());
        }
        await fresh.Database.ExecuteSqlRawAsync("DROP TRIGGER FailHistory");
        await new CalculationHistoryService(fresh).CreateMonthlyAsync(id, 2026, 1, CalculationSourceMode.References);
        Assert.Single(await fresh.CalculationHistory.ToListAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CallerOwnsCommitAndRollback(bool commit)
    {
        using var db = new SqliteFileDatabase();
        var id = await HistoryFixture.SeedAsync(db);
        await using (var c = db.Open())
        {
            await using var transaction = await c.Database.BeginTransactionAsync();
            await new CalculationHistoryService(c).CreateMonthlyAsync(id, 2026, 1, CalculationSourceMode.References);
            Assert.Same(transaction, c.Database.CurrentTransaction);
            if (commit) await transaction.CommitAsync(); else await transaction.RollbackAsync();
        }
        await using var fresh = db.Open();
        Assert.Equal(commit ? 1 : 0, await fresh.CalculationHistory.CountAsync());
        Assert.Equal(commit ? 5 : 4, await fresh.Set<AuditEvent>().CountAsync());
    }

    [Fact]
    public async Task CancellationAfterSqlRollsBackAndDetachesInternalAppend()
    {
        using var db = new SqliteFileDatabase();
        var id = await HistoryFixture.SeedAsync(db);
        using var cancellation = new CancellationTokenSource();
        await using (var c = db.Open(new CancelAfterSaveInterceptor(cancellation)))
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new CalculationHistoryService(c)
                .CreateMonthlyAsync(id, 2026, 1, CalculationSourceMode.References, cancellation.Token));
            Assert.Empty(c.ChangeTracker.Entries());
        }
        await using var fresh = db.Open();
        Assert.Empty(await fresh.CalculationHistory.ToListAsync());
        Assert.Equal(4, await fresh.Set<AuditEvent>().CountAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CommitFailureReportsUncertaintyAndReconcilesOperationIdWithoutAutomaticRetry(bool afterCommit)
    {
        using var db = new SqliteFileDatabase();
        var id = await HistoryFixture.SeedAsync(db);
        var before = new CommitFaultInterceptor { Armed = true };
        var after = new AfterCommitFaultInterceptor();
        var operation = Guid.NewGuid();
        await using (var c = afterCommit ? db.Open(after) : db.Open(before))
        {
            c.AuditContext = new("synthetic", "history", OperationId: operation);
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => new CalculationHistoryService(c)
                .CreateMonthlyAsync(id, 2026, 1, CalculationSourceMode.References));
            Assert.Contains("unknown", error.Message);
            Assert.Empty(c.ChangeTracker.Entries());
        }
        await using var fresh = db.Open();
        var events = await fresh.Set<AuditEvent>().Where(e => e.OperationId == operation).ToListAsync();
        Assert.Equal(afterCommit ? 1 : 0, events.Count);
        Assert.Equal(afterCommit ? 1 : 0, await fresh.CalculationHistory.CountAsync());
        if (afterCommit)
        {
            Assert.NotNull(fresh.ReadAudit(Assert.Single(events)).After);
            fresh.AuditContext = new("synthetic", "history", OperationId: operation);
            await Assert.ThrowsAsync<InvalidOperationException>(() => new CalculationHistoryService(fresh)
                .CreateMonthlyAsync(id, 2026, 1, CalculationSourceMode.References));
            Assert.Single(await fresh.CalculationHistory.ToListAsync());
        }
    }
}
