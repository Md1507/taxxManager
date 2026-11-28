using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TaxManager.Core.Forecasting;
using TaxManager.Data.Audit;
using TaxManager.Data.CalculationHistory;
using TaxManager.Data.Exports;
using TaxManager.Data.Forecasting;
using TaxManager.Data.Profiles;
using TaxManager.Data.Protection;
using TaxManager.Data.TaxRules;
using TaxManager.Data.Tests.CalculationHistory;
using TaxManager.Data.Tests.Fixtures;
using Xunit;

namespace TaxManager.Data.Tests.Exports;

public sealed class PersonalTaxDataReaderTests
{
    [Fact]
    public async Task Annual_results_are_original_saved_snapshots_even_after_catalog_and_profile_are_removed()
    {
        using var db = new SqliteFileDatabase();
        Guid id;
        await using (var write = db.Open())
        {
            await write.Database.MigrateAsync();
            await new TaxRuleStore(write).SeedReferencesAsync();
            await new AnnualForecastRuleCatalog(write).SeedReferencesAsync();
            var entered = DateTimeOffset.UtcNow;
            var input = new ForecastAnnualInput(2026, Enumerable.Range(1, 12).Select(m => new ForecastMonth(m,
                new(30_000_000, ForecastStatus.Actual, "Synthetic", null, entered),
                new(0, ForecastStatus.Actual, null, null, entered),
                new(0, ForecastStatus.Actual, null, null, entered), null)).ToArray(), []);
            id = await new ForecastStore(write).CreateAsync(ForecastDraftReader.Encode(input));
        }
        AnnualCalculationSnapshot snapshot;
        await using (var calculate = db.Open()) snapshot = (await new AnnualForecastService(calculate).CalculateAsync(id, 1)).Snapshot!;
        await using (var remove = db.Open())
        {
            await new ForecastStore(remove).DeleteAsync(id, 1);
            await remove.Database.ExecuteSqlRawAsync("DELETE FROM AnnualForecastRules");
        }
        await using var reader = db.Open();
        var export = await new PersonalTaxDataReader(reader).ReadAsync();
        Assert.Equal(JsonSerializer.Serialize(snapshot), JsonSerializer.Serialize(Assert.Single(export.AnnualResults)));
        Assert.Empty(export.Forecasts);
        Assert.Null(export.AnnualResults[0].Result.Difference);
        Assert.Empty(reader.ChangeTracker.Entries());
    }

    [Fact]
    public async Task Read_preserves_plain_values_snapshots_and_deleted_profile_history_without_writing()
    {
        using var db = new SqliteFileDatabase();
        var id = await HistoryFixture.SeedAsync(db);
        var history = await HistoryFixture.CreateAsync(db, id);
        Guid forecastId;
        const string payload = "{\"amount\":null,\"zero\":0,\"source\":\"Synthetic 文\"}";
        await using (var write = db.Open())
        {
            forecastId = await new ForecastStore(write).CreateAsync(payload);
            await new ForecastStore(write).UpdateAsync(forecastId, 1, payload + " ");
        }
        int count;
        await using (var before = db.Open())
        {
            before.ValidateProtection();
            count = await before.Set<AuditEvent>().CountAsync();
        }
        await using (var reader = db.Open())
        {
            var export = await new PersonalTaxDataReader(reader).ReadAsync();
            Assert.Equal(id, Assert.Single(export.Profiles).Id);
            Assert.Equal(history.Input.GrossIncome, Assert.Single(export.MonthlyIncome).GrossIncome);
            Assert.Equal(history.HistoryId, Assert.Single(export.MonthlyResults).HistoryId);
            Assert.Equal(JsonSerializer.Serialize(history), JsonSerializer.Serialize(export.MonthlyResults[0]));
            Assert.Single(export.Dependents); Assert.Single(export.EligibilityPeriods);
            Assert.Equal(payload + " ", Assert.Single(export.Forecasts).Payload);
            Assert.Equal(new[] { payload, payload + " " }, export.ForecastVersions.OrderBy(v => v.Revision).Select(v => v.Payload));
            Assert.Equal(count, export.Audit.Length);
            Assert.All(export.Audit, a => Assert.NotNull(a.Content));
            Assert.Empty(reader.ChangeTracker.Entries());
        }
        await using (var remove = db.Open())
        {
            await new ProfileDeletionService(remove).DeleteAsync(id);
            await new ForecastStore(remove).DeleteAsync(forecastId, 2);
        }
        await using var final = db.Open();
        var retained = await new PersonalTaxDataReader(final).ReadAsync();
        Assert.Empty(retained.Profiles); Assert.Empty(retained.Forecasts);
        Assert.Equal(2, retained.ForecastVersions.Length);
        Assert.Equal(history.HistoryId, Assert.Single(retained.MonthlyResults).HistoryId);
        Assert.Contains(retained.Audit, a => a.ProfileId == id && a.Action == "Delete");
        Assert.Equal(retained.Audit.Length, await final.Set<AuditEvent>().CountAsync());
    }

    [Fact]
    public async Task Untouched_store_stays_without_sentinel_and_initialized_empty_store_authenticates()
    {
        using var db = new SqliteFileDatabase();
        await using (var fresh = db.Open())
        {
            await fresh.Database.MigrateAsync();
            var export = await new PersonalTaxDataReader(fresh).ReadAsync();
            Assert.Empty(export.Profiles); Assert.Empty(export.Audit);
            Assert.Empty(await fresh.Set<DataProtectionMetadata>().ToArrayAsync());
        }
        Guid id;
        await using (var write = db.Open())
        {
            var profile = new UserProfile { DisplayName = "Synthetic" };
            write.Add(profile); await write.SaveChangesAsync(); id = profile.Id;
        }
        await using (var purge = db.Open()) await new ProfilePurgeService(purge).PurgeAsync(id, new("synthetic", "test-export"));
        await using (var missingKeys = HistoryFixture.Open(db, null))
            await Assert.ThrowsAsync<DataProtectionException>(() => new PersonalTaxDataReader(missingKeys).ReadAsync());
        await using var valid = db.Open();
        Assert.Empty((await new PersonalTaxDataReader(valid).ReadAsync()).Profiles);
        Assert.Single(await valid.Set<DataProtectionMetadata>().ToArrayAsync());
    }

    [Fact]
    public async Task Export_rejects_corrupt_content_instead_of_returning_partial_data()
    {
        using var db = new SqliteFileDatabase();
        await HistoryFixture.SeedAsync(db);
        await using (var corrupt = db.Open())
            await corrupt.Database.ExecuteSqlRawAsync("UPDATE UserProfiles SET DisplayNameEncrypted=X'00'");
        await using var reader = db.Open();
        await Assert.ThrowsAsync<DataProtectionException>(() => new PersonalTaxDataReader(reader).ReadAsync());
    }
}
