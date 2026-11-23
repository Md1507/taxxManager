using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using TaxManager.Data.CalculationHistory;
using TaxManager.Data.Protection;
using TaxManager.Data.Tests.Fixtures;
using Xunit;

namespace TaxManager.Data.Tests.CalculationHistory;

public class ProtectionTests
{
    [Theory]
    [InlineData("Year")]
    [InlineData("Month")]
    [InlineData("CreatedAtUtc")]
    [InlineData("ProfileId")]
    [InlineData("Id")]
    [InlineData("PayloadEncrypted")]
    [InlineData("field")]
    [InlineData("row")]
    [InlineData("database")]
    public async Task EveryHeaderAndCiphertextContextIsAuthenticated(string mutation)
    {
        using var db = new SqliteFileDatabase();
        var id = await HistoryFixture.SeedAsync(db);
        await HistoryFixture.CreateAsync(db, id);
        await using (var c = db.Open())
        {
            switch (mutation)
            {
                case "Year": await c.Database.ExecuteSqlRawAsync("UPDATE CalculationHistory SET Year=2025"); break;
                case "Month": await c.Database.ExecuteSqlRawAsync("UPDATE CalculationHistory SET Month=2"); break;
                case "CreatedAtUtc": await c.Database.ExecuteSqlRawAsync("UPDATE CalculationHistory SET CreatedAtUtc='2026-01-01 00:00:00+00:00'"); break;
                case "ProfileId": await c.Database.ExecuteSqlRawAsync("UPDATE CalculationHistory SET ProfileId='11111111-1111-1111-1111-111111111111'"); break;
                case "Id": await c.Database.ExecuteSqlRawAsync("UPDATE CalculationHistory SET Id='11111111-1111-1111-1111-111111111111'"); break;
                case "PayloadEncrypted": await c.Database.ExecuteSqlRawAsync("UPDATE CalculationHistory SET PayloadEncrypted=X'540101AA'"); break;
                case "field": await c.Database.ExecuteSqlRawAsync("UPDATE CalculationHistory SET PayloadEncrypted=(SELECT GrossIncomeEncrypted FROM MonthlyIncomes)"); break;
                case "row":
                    var next = await new CalculationHistoryService(c).CreateMonthlyAsync(id, 2026, 1, CalculationSourceMode.References);
                    await c.Database.ExecuteSqlInterpolatedAsync($"UPDATE CalculationHistory SET PayloadEncrypted=(SELECT PayloadEncrypted FROM CalculationHistory WHERE Id={next.HistoryId}) WHERE Id<>{next.HistoryId}");
                    break;
                case "database":
                    var newId = Guid.NewGuid();
                    var check = db.Keys.Protect("TaxManager key check v1"u8, new(newId, newId, "Metadata", "1", "KeyCheck"));
                    await c.Database.ExecuteSqlInterpolatedAsync($"UPDATE DataProtectionMetadata SET DatabaseId={newId}, KeyCheck={check}");
                    break;
            }
        }
        await using var fresh = db.Open();
        await Assert.ThrowsAsync<DataProtectionException>(() => fresh.CalculationHistory.AsNoTracking().ToListAsync());
    }

    [Theory]
    [InlineData("schema")]
    [InlineData("header-schema")]
    [InlineData("missing")]
    [InlineData("null")]
    [InlineData("unknown")]
    [InlineData("decimal")]
    [InlineData("legal-status")]
    [InlineData("missing-legal-null")]
    [InlineData("mode")]
    [InlineData("duplicate")]
    public async Task AuthenticatedButUnsupportedDocumentFailsWithoutLeakingParserText(string mutation)
    {
        using var db = new SqliteFileDatabase();
        var id = await HistoryFixture.SeedAsync(db);
        var snapshot = await HistoryFixture.CreateAsync(db, id);
        await using (var c = db.Open())
        {
            var record = await c.CalculationHistory.SingleAsync();
            var databaseId = (await c.Set<DataProtectionMetadata>().SingleAsync()).DatabaseId;
            var node = JsonSerializer.SerializeToNode(snapshot)!;
            switch (mutation)
            {
                case "schema": node["SchemaVersion"] = 99; break;
                case "header-schema":
                    await c.Database.OpenConnectionAsync();
                    await c.Database.ExecuteSqlRawAsync("PRAGMA ignore_check_constraints=ON");
                    await c.Database.ExecuteSqlRawAsync("UPDATE CalculationHistory SET SchemaVersion=99");
                    break;
                case "missing": node.AsObject().Remove("Input"); break;
                case "null": node["Result"] = null; break;
                case "unknown": node["OptionalGraph"] = "SECRET_INVALID_DOCUMENT"; break;
                case "legal-status": node["Rules"]!["TaxBasis"]!["Status"] = 99; break;
                case "missing-legal-null": node["Rules"]!["TaxBasis"]!.AsObject().Remove("SourceUrl"); break;
                case "mode": node["SourceMode"] = 99; break;
            }
            var json = node.ToJsonString();
            if (mutation == "decimal") json = json.Replace("1234567890.123456789", "0.123456789012345678901234567891", StringComparison.Ordinal);
            if (mutation == "duplicate") json = json.Insert(1, "\"SchemaVersion\":1,");
            var field = JsonSerializer.Serialize(new { Field = "Snapshot", record.Year, record.Month,
                record.SchemaVersion, record.CreatedAtUtc });
            var bytes = db.Keys.Protect(Encoding.UTF8.GetBytes(json),
                new(databaseId, id, nameof(CalculationRecord), record.Id.ToString("D"), field));
            await c.Database.ExecuteSqlInterpolatedAsync($"UPDATE CalculationHistory SET PayloadEncrypted={bytes}");
        }
        await using var reader = db.Open();
        var failure = await Assert.ThrowsAsync<DataProtectionException>(() => reader.CalculationHistory.ToListAsync());
        Assert.Null(failure.InnerException);
        Assert.DoesNotContain("SECRET_INVALID_DOCUMENT", failure.ToString());
        Assert.DoesNotContain("1234567890", failure.ToString());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WrongOrMissingKeysFailEvenForEmptyLookup(bool missing)
    {
        using var db = new SqliteFileDatabase(); using var other = new SqliteFileDatabase();
        var id = await HistoryFixture.SeedAsync(db);
        await HistoryFixture.CreateAsync(db, id);
        await using var c = HistoryFixture.Open(db, missing ? null : other.Keys);
        await Assert.ThrowsAsync<DataProtectionException>(() => c.CalculationHistory.ToListAsync());
        await Assert.ThrowsAsync<DataProtectionException>(() => new CalculationHistoryService(c).ReadAsync(id, Guid.NewGuid()));
    }
}
