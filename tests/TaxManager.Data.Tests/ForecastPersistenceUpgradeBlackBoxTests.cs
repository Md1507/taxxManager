// Loại đầu vào: DB synthetic 3.1b có dữ liệu; nâng cấp additive; snapshot và ciphertext cũ; mở lại khóa đúng/sai/thiếu
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TaxManager.Data.Audit;
using TaxManager.Data.CalculationHistory;
using TaxManager.Data.Infrastructure;
using TaxManager.Data.Profiles;
using TaxManager.Data.Protection;
using TaxManager.Data.TaxRules;
using TaxManager.Data.Tests.Fixtures;
using Xunit;

namespace TaxManager.Data.Tests;

public sealed class ForecastPersistenceUpgradeBlackBoxTests
{
    [Fact]
    public async Task Populated_31b_database_upgrades_additively_and_reopens_without_rewriting_old_evidence()
    {
        const string previous = "20261123045552_AddCalculationHistory";
        using var database = new SqliteFileDatabase();
        var profile = new UserProfile { DisplayName = "SYN_BGOAL_LEGACY Nguyễn & 2026" };
        string before;
        Guid history;
        await using (var context = database.Open())
        {
            await context.GetService<IMigrator>().MigrateAsync(previous);
            context.AddRange(profile, SyntheticGraph.Income(profile.Id));
            await context.SaveChangesAsync();
            await new TaxRuleStore(context).SeedReferencesAsync();
            history = (await new CalculationHistoryService(context)
                .CreateMonthlyAsync(profile.Id, 2026, 1, CalculationSourceMode.References)).HistoryId;
            before = await OldEvidence(context);
        }

        await using (var upgrading = database.Open())
        {
            await upgrading.Database.MigrateAsync();
            await upgrading.Database.MigrateAsync();
            var applied = (await upgrading.Database.GetAppliedMigrationsAsync()).ToArray();
            Assert.Contains(previous, applied);
            Assert.True(applied.Any(m => string.CompareOrdinal(m, previous) > 0),
                "Stage 2 requires an additive persistence migration after the 3.1b schema.");
            Assert.Empty(await upgrading.Database.GetPendingMigrationsAsync());
        }

        // Re-read the key file and create a separate context, rather than retaining EF tracking or a key session.
        using var keys = FileKeyRing.Load(database.KeyPath, Path.GetFullPath("."), Path.GetDirectoryName(database.DatabasePath)!);
        await using (var reopened = Open(database.DatabasePath, keys))
        {
            reopened.ValidateProtection();
            Assert.Equal(before, await OldEvidence(reopened));
            var snapshot = await new CalculationHistoryService(reopened).ReadAsync(profile.Id, history);
            Assert.NotNull(snapshot);
            Assert.Equal(1234567890.123456789m, snapshot.Input.GrossIncome);
            Assert.Equal("SYN_BGOAL_LEGACY Nguyễn & 2026", (await reopened.UserProfiles.SingleAsync()).DisplayName);
            Assert.False(reopened.Database.HasPendingModelChanges());
        }

        using var unrelated = new SqliteFileDatabase();
        foreach (var badKeys in new FileKeyRing?[] { null, unrelated.Keys })
        {
            await using var rejected = Open(database.DatabasePath, badKeys);
            // The lookup deliberately has no result. Protection must fail before returning an empty result.
            await Assert.ThrowsAsync<DataProtectionException>(() => new CalculationHistoryService(rejected)
                .ReadAsync(Guid.NewGuid(), Guid.NewGuid()));
        }
        await using var final = Open(database.DatabasePath, keys);
        final.ValidateProtection();
        Assert.Equal(before, await OldEvidence(final));
    }

    private static TaxManagerDbContext Open(string path, FileKeyRing? keys) => new(
        new DbContextOptionsBuilder<TaxManagerDbContext>()
            .UseSqlite($"Data Source={path};Foreign Keys=True;Pooling=False").Options, keys);

    private static async Task<string> OldEvidence(TaxManagerDbContext context)
    {
        context.ValidateProtection();
        var profiles = (await context.UserProfiles.AsNoTracking().OrderBy(x => x.Id).ToListAsync())
            .Select(x => new { x.Id, x.DisplayName, x.DisplayNameEncrypted }).ToArray();
        var incomes = (await context.MonthlyIncomes.AsNoTracking().OrderBy(x => x.ProfileId).ThenBy(x => x.Year).ThenBy(x => x.Month).ToListAsync())
            .Select(x => new { x.ProfileId, x.Year, x.Month, x.GrossIncome, x.InsuranceAmount,
                x.ActualWithheldTax, x.GrossIncomeEncrypted, x.InsuranceAmountEncrypted, x.ActualWithheldTaxEncrypted }).ToArray();
        var snapshots = (await context.CalculationHistory.AsNoTracking().OrderBy(x => x.Id).ToListAsync())
            .Select(x => new { x.Id, x.ProfileId, x.Year, x.Month, x.SchemaVersion, x.CreatedAtUtc,
                x.PayloadEncrypted, x.Snapshot }).ToArray();
        var audit = (await context.Set<AuditEvent>().AsNoTracking().OrderBy(x => x.Id).ToListAsync())
            .Select(x => new { x.Id, x.OperationId, x.Ordinal, x.ProfileId, x.Entity, x.EntityKey,
                x.Action, x.TimestampUtc, x.Payload, Content = context.ReadAudit(x) }).ToArray();
        var metadata = await context.Set<DataProtectionMetadata>().AsNoTracking().SingleAsync();
        var rules = (await new TaxRuleStore(context).LoadReferencesAsync()).GetForYear(2026);
        return JsonSerializer.Serialize(new { profiles, incomes, snapshots, audit, metadata, rules });
    }
}
