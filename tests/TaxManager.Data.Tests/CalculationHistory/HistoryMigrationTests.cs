using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TaxManager.Data.Audit;
using TaxManager.Data.CalculationHistory;
using TaxManager.Data.Dependents;
using TaxManager.Data.Profiles;
using TaxManager.Data.Protection;
using TaxManager.Data.TaxRules;
using TaxManager.Data.Tests.Fixtures;
using Xunit;

namespace TaxManager.Data.Tests.CalculationHistory;

public class HistoryMigrationTests
{
    [Fact]
    public async Task ActualPopulatedStep23UpgradesAdditivelyAndReopensWithOriginalKeysAndAudit()
    {
        using var db = new SqliteFileDatabase();
        var profile = new UserProfile { DisplayName = "SYNTHETIC_23_UPGRADE_NAME" };
        var dependent = new Dependent { ProfileId = profile.Id, DisplayName = "SYNTHETIC_23_UPGRADE_DEPENDENT" };
        string auditBefore;
        byte[] incomeBefore;
        byte[] sentinelBefore;
        string rulesBefore;
        await using (var c = db.Open())
        {
            await c.GetService<IMigrator>().MigrateAsync("20261123034403_AddDataProtectionAndAudit");
            // Initialize exactly the 2.3 sentinel without using the new history table.
            var databaseId = Guid.NewGuid();
            var check = db.Keys.Protect("TaxManager key check v1"u8, new(databaseId, databaseId, "Metadata", "1", "KeyCheck"));
            await c.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO DataProtectionMetadata VALUES(1,{databaseId},{check})");
            c.AddRange(profile, dependent, SyntheticGraph.Income(profile.Id), new DependentEligibilityPeriod
            {
                ProfileId = profile.Id, DependentId = dependent.Id,
                EligibleFromMonth = new(2026, 1, 1), EligibleToMonth = new(2026, 12, 1)
            });
            await c.SaveChangesAsync();
            await new TaxRuleStore(c).SeedReferencesAsync();
            auditBefore = JsonSerializer.Serialize((await c.Set<AuditEvent>().AsNoTracking().OrderBy(e => e.Id).ToListAsync())
                .Select(e => new { e.Id, e.ProfileId, e.Payload, Content = c.ReadAudit(e) }));
            incomeBefore = (await c.MonthlyIncomes.SingleAsync()).GrossIncomeEncrypted.ToArray();
            sentinelBefore = (await c.Set<DataProtectionMetadata>().SingleAsync()).KeyCheck;
            rulesBefore = JsonSerializer.Serialize((await new TaxRuleStore(c).LoadReferencesAsync()).GetForYear(2026));
            Assert.Equal(3, (await c.Database.GetAppliedMigrationsAsync()).Count());
            await c.Database.MigrateAsync();
            await c.Database.MigrateAsync();
            Assert.Equal(8, (await c.Database.GetAppliedMigrationsAsync()).Count());
            await Assert.ThrowsAsync<NotSupportedException>(() => c.GetService<IMigrator>()
                .MigrateAsync("20261123034403_AddDataProtectionAndAudit"));
            Assert.Equal(4, (await c.Database.GetAppliedMigrationsAsync()).Count());
        }
        using var keys = FileKeyRing.Load(db.KeyPath, Path.GetFullPath("."), Path.GetDirectoryName(db.DatabasePath)!);
        await using var fresh = HistoryFixture.Open(db, keys);
        fresh.ValidateProtection();
        Assert.Equal("SYNTHETIC_23_UPGRADE_NAME", (await fresh.UserProfiles.SingleAsync()).DisplayName);
        Assert.Equal("SYNTHETIC_23_UPGRADE_DEPENDENT", (await fresh.Dependents.SingleAsync()).DisplayName);
        var income = await fresh.MonthlyIncomes.SingleAsync();
        Assert.Equal(1234567890.123456789m, income.GrossIncome); Assert.Equal(.1m, income.InsuranceAmount); Assert.Equal(2m, income.ActualWithheldTax);
        Assert.Equal(incomeBefore, income.GrossIncomeEncrypted);
        Assert.Equal(sentinelBefore, (await fresh.Set<DataProtectionMetadata>().SingleAsync()).KeyCheck);
        Assert.Equal(auditBefore, JsonSerializer.Serialize((await fresh.Set<AuditEvent>().AsNoTracking().OrderBy(e => e.Id).ToListAsync())
            .Select(e => new { e.Id, e.ProfileId, e.Payload, Content = fresh.ReadAudit(e) })));
        Assert.Equal(rulesBefore, JsonSerializer.Serialize((await new TaxRuleStore(fresh).LoadReferencesAsync()).GetForYear(2026)));
        Assert.Empty(await fresh.CalculationHistory.ToListAsync());
        await fresh.Database.OpenConnectionAsync();
        await using var command = fresh.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT count(*) FROM sqlite_master WHERE type='trigger' AND name LIKE '%NoOverlap_%'";
        Assert.Equal(4L, await command.ExecuteScalarAsync());
        command.CommandText = "PRAGMA foreign_key_list(CalculationHistory)";
        await using (var reader = await command.ExecuteReaderAsync()) Assert.False(await reader.ReadAsync());
        fresh.AuditContext = new("synthetic", "history after upgrade");
        await new CalculationHistoryService(fresh).CreateMonthlyAsync(profile.Id, 2026, 1, CalculationSourceMode.References);
        Assert.Single(await fresh.CalculationHistory.ToListAsync());
    }

    [Fact]
    public void AllSixPreviousMigrationFilesRemainByteIdentical()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "TaxManager.sln"))) directory = directory.Parent;
        Assert.NotNull(directory);
        var expected = new Dictionary<string, string>
        {
            ["20261122100144_InitialPersistence.cs"] = "5E5A3D4A0E2879787CEDDEAF714873B0BC1613B43AA3EEB93A4506B4984B6695",
            ["20261122100144_InitialPersistence.Designer.cs"] = "B984B241E0EEC2178265F7E8FF8543277D4D1AD364B21C0A395DA5911DF40B06",
            ["20261122161649_AddMonthlyTaxRules.cs"] = "7379164B5479C1C46BBD9D34B738F5235B4033A96AE3FB55EC85C329E197D207",
            ["20261122161649_AddMonthlyTaxRules.Designer.cs"] = "307898C6F2F913E8A49868C7FCBD5E69DC3A233F1B889F3EA379C12414F6937D",
            ["20261123034403_AddDataProtectionAndAudit.cs"] = "E47461031B210F9F2906717ADEBCE15D4F271B29067335C743728123843EEE99",
            ["20261123034403_AddDataProtectionAndAudit.Designer.cs"] = "CCEE4114B62B54E41E732DFE75D5B49F2FFEE363E67A1A9A2D5D55516F979C08"
        };
        foreach (var (file, hash) in expected)
            Assert.Equal(hash, Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(directory.FullName, "src", "TaxManager.Data", "Migrations", file)))));
    }
}
