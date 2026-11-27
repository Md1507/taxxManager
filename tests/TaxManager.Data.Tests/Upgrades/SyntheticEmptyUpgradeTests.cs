using System.Text.Json;
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

public class SyntheticEmptyUpgradeTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EmptyAndRulesOnlySourcesInitializeSentinelAndPreserveAllPublicRules(bool rulesOnly)
    {
        using var db = new SqliteFileDatabase();
        await using (var c = db.Open())
        {
            await c.GetService<IMigrator>().MigrateAsync("20261122161649_AddMonthlyTaxRules");
            if (rulesOnly) await new TaxRuleStore(c).SeedReferencesAsync();
        }
        var sourceBytes = File.ReadAllBytes(db.DatabasePath);
        var rules = await ReadPublicRows(db.DatabasePath);
        var target = db.DatabasePath + ".encrypted";
        var result = await new SyntheticDatabaseUpgrader().UpgradeAsync(db.DatabasePath, target, db.Keys, true);
        Assert.Equal(0, result.Profiles); Assert.Equal(0, result.Incomes);
        Assert.Equal(0, result.Dependents); Assert.Equal(0, result.Periods);
        Assert.Equal(sourceBytes, File.ReadAllBytes(db.DatabasePath));
        Assert.Equal(rules, await ReadPublicRows(target));
        Assert.Equal(rulesOnly, rules.Length > 0);

        // A fresh ring/context must authenticate even though no protected entities exist.
        using var keys = FileKeyRing.Load(db.KeyPath, Path.GetFullPath("."), Path.GetDirectoryName(target)!);
        await using (var reopened = Open(target, keys))
        {
            reopened.ValidateProtection();
            Assert.Single(await reopened.Set<DataProtectionMetadata>().AsNoTracking().ToListAsync());
            Assert.Empty(await reopened.UserProfiles.ToListAsync());
            Assert.Empty(await reopened.Set<AuditEvent>().ToListAsync());
            Assert.Equal(rulesOnly ? 2 : 0, await reopened.Set<TaxRuleSnapshot>().CountAsync());
        }
        using var wrong = new SqliteFileDatabase();
        await using (var reopened = Open(target, wrong.Keys)) Assert.Throws<DataProtectionException>(reopened.ValidateProtection);
        await using (var reopened = Open(target, null)) Assert.Throws<DataProtectionException>(reopened.ValidateProtection);
        Assert.Equal(sourceBytes, File.ReadAllBytes(db.DatabasePath));
    }

    [Fact]
    public async Task RulesOnlyImportFailureAfterSaveKeepsSourceAndDoesNotPublishMetadataOrRules()
    {
        using var db = new SqliteFileDatabase();
        await using (var c = db.Open())
        {
            await c.GetService<IMigrator>().MigrateAsync("20261122161649_AddMonthlyTaxRules");
            await new TaxRuleStore(c).SeedReferencesAsync();
        }
        var sourceBytes = File.ReadAllBytes(db.DatabasePath);
        var target = db.DatabasePath + ".encrypted";
        var fault = new SaveActionInterceptor(c =>
        {
            Assert.NotNull(c.Database.CurrentTransaction);
            Assert.Single(c.Set<DataProtectionMetadata>().AsNoTracking());
            Assert.Equal(2, c.Set<TaxRuleSnapshot>().Count());
            throw new InvalidOperationException("Synthetic fault after public rules and sentinel SQL.");
        });
        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => new SyntheticDatabaseUpgrader(fault)
            .UpgradeAsync(db.DatabasePath, target, db.Keys, true));
        Assert.Contains("Synthetic fault", failure.Message);
        Assert.Equal(sourceBytes, File.ReadAllBytes(db.DatabasePath));
        Assert.False(File.Exists(target));
        Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(target)!, "*.pending-*"));
    }

    private static async Task<string[]> ReadPublicRows(string path)
    {
        await using var c = new SqliteConnection($"Data Source={path};Mode=ReadOnly;Pooling=False");
        await c.OpenAsync();
        var rows = new List<string>();
        foreach (var table in new[] { "TaxRuleLegalBasis", "TaxRuleSnapshot", "TaxRuleBracket", "TaxRulePeriod" })
        {
            await using var cmd = c.CreateCommand();
            cmd.CommandText = $"SELECT * FROM {table}";
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                // Compare every legacy column. The additive annual column must be
                // empty for copied monthly-only rules, rather than extending the oracle row.
                var values = new List<object>();
                for (var index = 0; index < reader.FieldCount; index++)
                    if (reader.GetName(index) == "AnnualGraph") Assert.True(reader.IsDBNull(index));
                    else values.Add(reader.GetValue(index));
                rows.Add(table + ":" + JsonSerializer.Serialize(values));
            }
        }
        return rows.Order(StringComparer.Ordinal).ToArray();
    }

    private static TaxManagerDbContext Open(string path, FileKeyRing? keys) => new(
        new DbContextOptionsBuilder<TaxManagerDbContext>().UseSqlite($"Data Source={path};Foreign Keys=True;Pooling=False").Options, keys);
}
