// Loại đầu vào: hồ sơ và snapshot chặng 2 có metadata, nâng cấp SQLite additive lặp lại, ciphertext và audit sentinel cũ, migration và designer cũ
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TaxManager.Core.Forecasting;
using TaxManager.Data.Audit;
using TaxManager.Data.Forecasting;
using TaxManager.Data.Protection;
using TaxManager.Data.Tests.Fixtures;
using Xunit;

namespace TaxManager.Data.Tests;

/// <summary>Observes the stage-2 public storage contract across the stage-3 additive upgrade.</summary>
public sealed class AnnualStageThreeUpgradeBlackBoxTests
{
    [Fact]
    public async Task Populated_stage_two_profile_snapshots_metadata_and_protection_survive_additive_upgrade()
    {
        const string previous = "20261126101326_AddForecastSnapshots";
        using var database = new SqliteFileDatabase();
        var entered = new DateTimeOffset(2026, 10, 15, 6, 7, 8, TimeSpan.Zero);
        var amount = new ForecastValue<decimal>(31_847_529.1234567890123456789m,
            ForecastStatus.Planned, "SYN_ANNUAL_UPGRADE nguồn dự kiến", "Ghi chú cũ\nGiữ nguyên ngày nhập", entered);
        var profile = new ForecastProfile(2026, [new ForecastSalaryPeriod(1, 12, amount)],
            [new ForecastMonth(3, new(0m, ForecastStatus.Actual, "Nghỉ không lương", null, entered),
                new(0m, ForecastStatus.Assumed, "SYN_ANNUAL_UPGRADE", "Giả định bảo hiểm bằng zero", entered),
                null, null)]);
        var payload = JsonSerializer.Serialize(profile);
        Guid id;
        string before;
        await using (var old = database.Open())
        {
            await old.GetService<IMigrator>().MigrateAsync(previous);
            id = await new ForecastStore(old).CreateAsync(payload);
            await new ForecastStore(old).UpdateAsync(id, 1, payload);
            before = await Evidence(old);
            Assert.Equal(6, (await old.Database.GetAppliedMigrationsAsync()).Count());
        }

        await using (var upgrade = database.Open())
        {
            await upgrade.Database.MigrateAsync();
            await upgrade.Database.MigrateAsync();
            var applied = (await upgrade.Database.GetAppliedMigrationsAsync()).ToArray();
            Assert.Contains(previous, applied);
            Assert.True(applied.Any(migration => string.CompareOrdinal(migration, previous) > 0),
                "Stage 3 requires an additive migration after the existing stage-2 snapshot schema.");
            Assert.Empty(await upgrade.Database.GetPendingMigrationsAsync());
            Assert.False(upgrade.Database.HasPendingModelChanges());
        }
        // A fresh context and fresh key object prove reopen, rather than EF tracked state.
        using var keys = FileKeyRing.Load(database.KeyPath, Path.GetFullPath("."), Path.GetDirectoryName(database.DatabasePath)!);
        var options = new DbContextOptionsBuilder<TaxManager.Data.Infrastructure.TaxManagerDbContext>()
            .UseSqlite($"Data Source={database.DatabasePath};Foreign Keys=True;Pooling=False").Options;
        await using var reopened = new TaxManager.Data.Infrastructure.TaxManagerDbContext(options, keys);
        reopened.ValidateProtection();
        Assert.Equal(before, await Evidence(reopened));
        var document = await new ForecastStore(reopened).ReadAsync(id);
        Assert.NotNull(document);
        Assert.Equal(2, document.Revision);
        Assert.Equal(payload, document.Payload);
        var history = await new ForecastStore(reopened).HistoryAsync(id);
        Assert.Equal(2, history.Count);
        Assert.All(history, snapshot => Assert.Equal(payload, snapshot.Payload));
        Assert.Contains("Planned", document.Payload);
        Assert.Contains("Assumed", document.Payload);
        using var originalInput = JsonDocument.Parse(document.Payload!);
        Assert.Equal(entered, originalInput.RootElement.GetProperty("Periods")[0].GetProperty("Gross").GetProperty("EnteredAt").GetDateTimeOffset());
    }

    [Theory]
    [InlineData("20261123045552_AddCalculationHistory.cs", "574A065FCCA0D805F0D6BE6FF3C476B24BBEC8C36D0512190364C1E5445D8E60")]
    [InlineData("20261123045552_AddCalculationHistory.Designer.cs", "CC18BFF703F9119DE8251491CBD17A53256716718BD307F462F384DF95B869BE")]
    [InlineData("20261126101030_AddForecastDocuments.cs", "14828DB888ECE3DACC9D64BAD46DA38B73B6B48F0F61BAB236A8AD7598E89815")]
    [InlineData("20261126101030_AddForecastDocuments.Designer.cs", "61CDE0AA0DD9FDEA99D032841D2FCA06E7BBC70D4A107C62AA5A1BDFA715F976")]
    [InlineData("20261126101326_AddForecastSnapshots.cs", "1017FF35F436FB523EC255DDFBAAA57A0EF957688BD08F994FFF2A266313BA0F")]
    [InlineData("20261126101326_AddForecastSnapshots.Designer.cs", "A06ABD6FEF180022A68A0D1F5D9E358EBEFE60DC7FA41CC5AC6DE3D949878E5D")]
    public void Existing_history_and_stage_two_migration_bytes_are_unchanged(string file, string sha256)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "TaxManager.sln"))) root = root.Parent;
        Assert.NotNull(root);
        // These are baseline file-byte hashes, not hashes recomputed from the changed checkout.
        var path = Path.Combine(root.FullName, "src", "TaxManager.Data", "Migrations", file);
        Assert.Equal(sha256, Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))));
    }

    private static async Task<string> Evidence(TaxManager.Data.Infrastructure.TaxManagerDbContext context)
    {
        context.ValidateProtection();
        var documents = (await context.Set<ForecastDocument>().AsNoTracking().OrderBy(x => x.Id).ToListAsync())
            .Select(x => new { x.Id, x.Revision, x.Payload });
        var snapshots = (await context.Set<ForecastSnapshot>().AsNoTracking().OrderBy(x => x.Id).ToListAsync())
            .Select(x => new { x.Id, x.DocumentId, x.Revision, x.Payload });
        var audit = (await context.Set<AuditEvent>().AsNoTracking().OrderBy(x => x.Id).ToListAsync())
            .Select(x => new { x.Id, x.OperationId, x.Ordinal, x.ProfileId, x.Entity, x.EntityKey,
                x.Action, x.TimestampUtc, x.Payload, Content = context.ReadAudit(x) });
        var metadata = await context.Set<DataProtectionMetadata>().AsNoTracking().SingleAsync();
        // Observe ciphertext too, without assuming the column layout of the new result contract.
        await context.Database.OpenConnectionAsync();
        var stored = new SortedDictionary<string, string[]>();
        foreach (var table in new[] { "ForecastDocuments", "ForecastSnapshots" })
        {
            await using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT * FROM \"" + table + "\"";
            await using var reader = await command.ExecuteReaderAsync();
            var rows = new List<string>();
            while (await reader.ReadAsync())
            {
                var values = new object[reader.FieldCount];
                reader.GetValues(values);
                rows.Add(JsonSerializer.Serialize(values.Select(value => value is byte[] bytes ? Convert.ToHexString(bytes) : value)));
            }
            stored[table] = rows.Order(StringComparer.Ordinal).ToArray();
        }
        return JsonSerializer.Serialize(new { documents, snapshots, audit, metadata, stored });
    }
}
