using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TaxManager.Data.Audit;
using TaxManager.Data.Infrastructure;
using TaxManager.Data.Protection;
using TaxManager.Data.TaxRules;
using TaxManager.Data.Tests.Fixtures;
using TaxManager.Data.Upgrades;
using Xunit;

namespace TaxManager.Data.Tests.Upgrades;

public class SyntheticUpgradeTests
{
    [Fact]
    public async Task CopyForwardPreservesSensitiveFieldsKeysRulesRelationsAndTriggersAndSourceBytes()
    {
        using var db = new SqliteFileDatabase();
        var (p, d) = await LegacySyntheticDatabase.SeedAsync(db);
        var original = SHA256.HashData(File.ReadAllBytes(db.DatabasePath));
        var target = db.DatabasePath + ".encrypted";
        var result = await new SyntheticDatabaseUpgrader().UpgradeAsync(db.DatabasePath, target, db.Keys, true);
        Assert.Equal(1, result.Profiles); Assert.Equal(1, result.Incomes); Assert.Equal(1, result.Dependents); Assert.Equal(1, result.Periods);
        Assert.Equal(original, SHA256.HashData(File.ReadAllBytes(db.DatabasePath)));
        await using var c = Open(target, db.Keys);
        Assert.Equal(p, (await c.UserProfiles.SingleAsync()).Id);
        Assert.Equal("Synthetic Nguyễn LEGACY_SECRET_734981", (await c.UserProfiles.SingleAsync()).DisplayName);
        Assert.Equal("", (await c.Dependents.SingleAsync()).DisplayName);
        Assert.Equal(d, (await c.Dependents.SingleAsync()).Id);
        var income = await c.MonthlyIncomes.SingleAsync();
        Assert.Equal(1234567890.123456789m, income.GrossIncome);
        Assert.Equal(0.0000000000000000000000000001m, income.InsuranceAmount);
        Assert.Equal(decimal.MaxValue, income.ActualWithheldTax);
        Assert.Equal(new DateOnly(2026, 12, 1), (await c.DependentEligibilityPeriods.SingleAsync()).EligibleToMonth);
        var provider = await new TaxRuleStore(c).LoadReferencesAsync();
        Assert.NotNull(provider);
        await Assert.ThrowsAsync<InvalidOperationException>(() => new TaxRuleStore(c).LoadOfficialAsync());
        Assert.Equal(4, await c.Set<AuditEvent>().CountAsync());
        Assert.All(await c.Set<AuditEvent>().AsNoTracking().ToListAsync(), e => Assert.Equal("synthetic-upgrader", c.ReadAudit(e).Actor));
        await Assert.ThrowsAsync<SqliteException>(() => c.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO DependentEligibilityPeriods VALUES ({p},{d},'2026-02-01','2026-03-01')"));
        await Assert.ThrowsAsync<NotSupportedException>(() => c.GetService<IMigrator>().MigrateAsync("20261122161649_AddMonthlyTaxRules"));
        Assert.Equal(4, (await c.Database.GetAppliedMigrationsAsync()).Count());
        await Assert.ThrowsAsync<ArgumentException>(() => new SyntheticDatabaseUpgrader().UpgradeAsync(db.DatabasePath, target, db.Keys, true));
        Assert.True(File.ReadAllBytes(target).AsSpan().IndexOf(System.Text.Encoding.UTF8.GetBytes("LEGACY_SECRET_734981")) < 0);
    }

    [Theory]
    [InlineData("sql")]
    [InlineData("readback")]
    [InlineData("cancel")]
    [InlineData("publish")]
    [InlineData("key")]
    public async Task FailureNeverChangesSourceOrPublishesIncompleteTarget(string failure)
    {
        using var db = new SqliteFileDatabase(); await LegacySyntheticDatabase.SeedAsync(db);
        var original = File.ReadAllBytes(db.DatabasePath);
        var target = db.DatabasePath + ".encrypted";
        using var cancel = new CancellationTokenSource();
        var callback = new SaveActionInterceptor(c =>
        {
            if (failure == "sql") throw new InvalidOperationException("Synthetic copy failure after SQL.");
            if (failure == "readback") c.Database.ExecuteSqlRaw("UPDATE MonthlyIncomes SET GrossIncomeEncrypted=X'00'");
            if (failure == "cancel") cancel.Cancel();
            if (failure == "publish") File.WriteAllText(target, "existing synthetic target");
            if (failure == "key") db.Keys.Dispose();
        });
        await Assert.ThrowsAnyAsync<Exception>(() => new SyntheticDatabaseUpgrader(callback)
            .UpgradeAsync(db.DatabasePath, target, db.Keys, true, cancel.Token));
        Assert.Equal(original, File.ReadAllBytes(db.DatabasePath));
        if (failure == "publish") Assert.Equal("existing synthetic target", File.ReadAllText(target));
        else Assert.False(File.Exists(target));
        Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(target)!, "*.pending-*"));
    }

    [Theory]
    [InlineData("0.123456789012345678901234567891")]
    [InlineData("-1")]
    [InlineData("garbage")]
    public async Task InvalidLegacyMoneyIsRejectedWithoutRoundingOrLosingSource(string invalid)
    {
        using var db = new SqliteFileDatabase(); await LegacySyntheticDatabase.SeedAsync(db);
        await using (var c = db.Open()) await c.Database.ExecuteSqlInterpolatedAsync($"UPDATE MonthlyIncomes SET GrossIncome={invalid}");
        var original = File.ReadAllBytes(db.DatabasePath);
        var target = db.DatabasePath + ".encrypted";
        await Assert.ThrowsAnyAsync<Exception>(() => new SyntheticDatabaseUpgrader().UpgradeAsync(db.DatabasePath, target, db.Keys, true));
        Assert.Equal(original, File.ReadAllBytes(db.DatabasePath)); Assert.False(File.Exists(target));
    }

    [Fact]
    public async Task PathsIntentAndCancellationAreValidatedBeforeCreatingTarget()
    {
        using var db = new SqliteFileDatabase(); await LegacySyntheticDatabase.SeedAsync(db);
        var upgrader = new SyntheticDatabaseUpgrader(); var target = db.DatabasePath + ".encrypted";
        await Assert.ThrowsAsync<ArgumentException>(() => upgrader.UpgradeAsync(db.DatabasePath, target, db.Keys, false));
        await Assert.ThrowsAsync<ArgumentException>(() => upgrader.UpgradeAsync(db.DatabasePath, db.DatabasePath, db.Keys, true));
        await Assert.ThrowsAsync<ArgumentException>(() => upgrader.UpgradeAsync("taxmanager.local.db", target, db.Keys, true));
        await Assert.ThrowsAsync<ArgumentException>(() => upgrader.UpgradeAsync(db.DatabasePath,
            Path.Combine(Path.GetDirectoryName(target)!, "taxmanager.local.db"), db.Keys, true));
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => upgrader.UpgradeAsync(db.DatabasePath, target, db.Keys, true, cancel.Token));
        Assert.False(File.Exists(target));
        File.WriteAllText(target + "-wal", "synthetic existing sidecar");
        await Assert.ThrowsAsync<ArgumentException>(() => upgrader.UpgradeAsync(db.DatabasePath, target, db.Keys, true));
        Assert.Equal("synthetic existing sidecar", File.ReadAllText(target + "-wal"));
    }

    private static TaxManagerDbContext Open(string path, FileKeyRing keys) => new(
        new DbContextOptionsBuilder<TaxManagerDbContext>().UseSqlite($"Data Source={path};Foreign Keys=True;Pooling=False").Options, keys);
}
