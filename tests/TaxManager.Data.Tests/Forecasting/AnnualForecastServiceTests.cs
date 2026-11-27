using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TaxManager.Core.Forecasting;
using TaxManager.Data.Audit;
using TaxManager.Data.Forecasting;
using TaxManager.Data.Infrastructure;
using TaxManager.Data.Protection;
using TaxManager.Data.TaxRules;
using TaxManager.Data.Tests.Fixtures;
using Xunit;

namespace TaxManager.Data.Tests.Forecasting;

public sealed class AnnualForecastServiceTests
{
    private static ForecastAnnualInput Input(decimal gross = 30_000_000m)
    {
        var entered = new DateTimeOffset(2026, 10, 20, 1, 2, 3, TimeSpan.Zero);
        ForecastValue<decimal> Money(decimal value) => new(value, ForecastStatus.Actual, "SYN_ANNUAL_PRIVATE_nguồn", "Ghi chú\nRiêng tư", entered);
        return new(2026, Enumerable.Range(1, 12).Select(month => new ForecastMonth(month, Money(gross), Money(0),
            new(0, ForecastStatus.Actual, "Tự khai", null, entered), null)).ToArray(), []);
    }
    private static async Task<Guid> Setup(SqliteFileDatabase database)
    {
        await using var context = database.Open();
        await context.Database.MigrateAsync();
        await new TaxRuleStore(context).SeedReferencesAsync();
        await new AnnualForecastRuleCatalog(context).SeedReferencesAsync();
        return await new ForecastStore(context).CreateAsync(ForecastDraftReader.Encode(Input()));
    }

    [Fact]
    public async Task Saved_revision_and_rules_remain_readable_after_recalculation_catalog_drift_and_profile_deletion()
    {
        using var database = new SqliteFileDatabase();
        var id = await Setup(database);
        AnnualCalculationSnapshot first;
        string firstJson;
        await using (var context = database.Open())
        {
            first = (await new AnnualForecastService(context).CalculateAsync(id, 1)).Snapshot!;
            firstJson = JsonSerializer.Serialize(first);
            Assert.Equal(11_400_000m, first.Result.Obligation); // 120m*5% + 54m*10%.
            Assert.Null(first.Result.Difference);
            Assert.Equal(1, first.InputRevision);
            Assert.NotEqual(Guid.Empty, first.InputSnapshotId);
            var second = (await new AnnualForecastService(context).CalculateAsync(id, 1)).Snapshot!;
            Assert.NotEqual(first.HistoryId, second.HistoryId);
        }
        await using (var context = database.Open())
        {
            await new ForecastStore(context).UpdateAsync(id, 1, ForecastDraftReader.Encode(Input(40_000_000m)));
        }
        await using (var context = database.Open())
        {
            await context.Database.ExecuteSqlRawAsync("DELETE FROM AnnualForecastRules WHERE PeriodFrom = '2026-01-01'");
            await Assert.ThrowsAsync<InvalidOperationException>(() => new AnnualForecastService(context).CalculateAsync(id, 2));
            Assert.Equal(firstJson, JsonSerializer.Serialize(await new AnnualForecastService(context).ReadAsync(id, first.HistoryId)));
        }
        await using (var context = database.Open()) await new ForecastStore(context).DeleteAsync(id, 2);
        await using (var context = database.Open())
        {
            var service = new AnnualForecastService(context);
            Assert.Equal(2, (await service.HistoryAsync(id)).Count);
            Assert.Equal(firstJson, JsonSerializer.Serialize(await service.ReadAsync(id, first.HistoryId)));
            Assert.Equal(2, await context.Set<AuditEvent>().CountAsync(x => x.Entity == nameof(AnnualCalculationRecord)));
            Assert.Empty(await context.MonthlyIncomes.ToListAsync());
            Assert.Empty(await context.CalculationHistory.ToListAsync());
        }
    }

    [Fact]
    public async Task Direct_append_update_and_delete_are_rejected_and_empty_history_reads_require_the_key()
    {
        using var database = new SqliteFileDatabase();
        var id = await Setup(database);
        await using (var context = database.Open()) await new AnnualForecastService(context).CalculateAsync(id, 1);
        await using (var context = database.Open())
        {
            var row = await context.Set<AnnualCalculationRecord>().SingleAsync();
            context.Entry(row).Property(x => x.InputRevision).CurrentValue = 2;
            await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        }
        await using (var context = database.Open())
        {
            var row = await context.Set<AnnualCalculationRecord>().SingleAsync();
            context.Remove(row);
            await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        }
        await using (var context = database.Open())
        {
            var row = await context.Set<AnnualCalculationRecord>().AsNoTracking().SingleAsync();
            context.Add(row);
            await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        }
        var options = new DbContextOptionsBuilder<TaxManagerDbContext>().UseSqlite($"Data Source={database.DatabasePath};Foreign Keys=True;Pooling=False").Options;
        await using var noKey = new TaxManagerDbContext(options);
        await Assert.ThrowsAsync<DataProtectionException>(() => new AnnualForecastService(noKey).HistoryAsync(Guid.NewGuid()));
        using var other = new SqliteFileDatabase();
        await using var wrongKey = new TaxManagerDbContext(options, other.Keys);
        await Assert.ThrowsAsync<DataProtectionException>(() => new AnnualForecastService(wrongKey).ReadAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task Orphaned_annual_history_still_requires_the_original_sentinel_when_listing_profiles()
    {
        using var database = new SqliteFileDatabase();
        var id = await Setup(database);
        await using (var context = database.Open())
        {
            await new AnnualForecastService(context).CalculateAsync(id, 1);
            // Simulate corruption leaving only annual evidence in an initialized store.
            await context.Database.ExecuteSqlRawAsync("""
                DELETE FROM ForecastDocuments;
                DELETE FROM ForecastSnapshots;
                DELETE FROM AuditEvents;
                DELETE FROM DataProtectionMetadata;
                """);
        }
        await using var reopened = database.Open();
        await Assert.ThrowsAsync<DataProtectionException>(() => new ForecastStore(reopened).ListAsync());
        await Assert.ThrowsAsync<DataProtectionException>(() => new AnnualForecastService(reopened).HistoryAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Caller_transaction_rollback_removes_history_and_audit_and_populated_history_blocks_downgrade()
    {
        using var database = new SqliteFileDatabase();
        var id = await Setup(database);
        await using (var context = database.Open())
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            await new AnnualForecastService(context).CalculateAsync(id, 1);
            await transaction.RollbackAsync();
        }
        await using (var context = database.Open())
        {
            Assert.Empty(await new AnnualForecastService(context).HistoryAsync(id));
            Assert.Empty(await context.Set<AuditEvent>().Where(x => x.Entity == nameof(AnnualCalculationRecord)).ToListAsync());
            await new AnnualForecastService(context).CalculateAsync(id, 1);
        }
        await using (var context = database.Open())
            await Assert.ThrowsAsync<SqliteException>(() => context.GetService<IMigrator>().MigrateAsync("20261127043040_AddAnnualRuleGraph"));
        await using (var context = database.Open()) Assert.Single(await new AnnualForecastService(context).HistoryAsync(id));
    }
}
