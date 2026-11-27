using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TaxManager.Core.Forecasting;
using TaxManager.Data.Audit;
using TaxManager.Data.Forecasting;
using TaxManager.Data.Infrastructure;
using TaxManager.Data.Protection;
using TaxManager.Data.TaxRules;
using TaxManager.Data.Tests.Fixtures;
using Xunit;

namespace TaxManager.Data.Tests.Forecasting;

public sealed class ForecastMonthServiceTests
{
    private static readonly DateTimeOffset Entered = new(2026, 10, 20, 1, 2, 3, TimeSpan.Zero);
    private static ForecastValue<T> Value<T>(T value, ForecastStatus status = ForecastStatus.Actual) => new(value, status, "Phiếu lương synthetic", "Ghi chú\nhai dòng", Entered);
    private static ForecastAnnualInput Input(int month = 6) => new(2026,
        Enumerable.Range(1, 12).Select(number => number == month
            ? new ForecastMonth(number, Value(30_000_000m), Value(3_000_000m), Value(1), null)
            : new ForecastMonth(number, null, null, null, null)).ToArray(),
        [new("included", month, Value(2_000_000m), true),
         new("added", month, Value(3_000_000m, ForecastStatus.Planned), false),
         new("other-month", month == 6 ? 7 : 6, Value(100_000_000m), false)]);

    private static async Task<Guid> Setup(SqliteFileDatabase database, ForecastAnnualInput input)
    {
        await using var context = database.Open();
        await context.Database.MigrateAsync();
        await new TaxRuleStore(context).SeedReferencesAsync();
        await new AnnualForecastRuleCatalog(context).SeedReferencesAsync();
        return await new ForecastStore(context).CreateAsync(ForecastDraftReader.Encode(input));
    }

    [Theory]
    [InlineData(6, 1, 6)]
    [InlineData(7, 7, 12)]
    public async Task Saved_month_uses_its_own_inputs_bonuses_and_registered_period_without_writing(int month, int from, int to)
    {
        using var database = new SqliteFileDatabase();
        var input = Input(month);
        var id = await Setup(database, input);
        await using var context = database.Open();
        var before = await context.Set<ForecastDocument>().AsNoTracking().SingleAsync();
        var auditCount = await context.Set<AuditEvent>().CountAsync();
        var service = new ForecastMonthService(context);
        var view = (await service.ReadAsync(id, 1, month))!;
        // 30m gross already includes 2m bonus; only the additional 3m is added.
        // (33m - 3m - 15.5m - 6.2m) * 5% = 415,000.
        Assert.Equal(33_000_000m, view.Result!.GrossIncome.Value);
        Assert.Equal(415_000m, view.Result.Pit.TotalTax.Value);
        Assert.Equal(input.Months[month - 1], view.Input);
        Assert.Equal(2, view.Bonuses.Count);
        Assert.Equal(ForecastStatus.Planned, view.Status);
        Assert.Null(view.Input.Withheld);
        Assert.Null(view.Difference);
        Assert.Equal(new DateOnly(2026, from, 1), view.Registration!.PeriodFrom);
        Assert.Equal(new DateOnly(2026, to, DateTime.DaysInMonth(2026, to)), view.Registration.PeriodTo);
        Assert.All(view.Result.Pit.Portions, p => Assert.Equal(view.Registration.RuleSet.TaxBasis, p.Basis));
        Assert.Equal(JsonSerializer.Serialize(view.Result), JsonSerializer.Serialize((await service.ReadAsync(id, 1, month))!.Result));
        var after = await context.Set<ForecastDocument>().AsNoTracking().SingleAsync();
        Assert.Equal(before.PayloadEncrypted, after.PayloadEncrypted);
        Assert.Equal(before.Payload, after.Payload);
        Assert.Equal(1, after.Revision);
        Assert.Equal(auditCount, await context.Set<AuditEvent>().CountAsync());
        Assert.Single(await new ForecastStore(context).HistoryAsync(id));
        Assert.Empty(await context.CalculationHistory.ToListAsync());
        Assert.Empty(await context.Set<AnnualCalculationRecord>().ToListAsync());
        Assert.Empty(await context.UserProfiles.ToListAsync());
        Assert.Empty(await context.MonthlyIncomes.ToListAsync());
    }

    [Fact]
    public async Task Missing_fields_block_only_the_selected_month_and_zero_is_known_withholding_is_independent()
    {
        using var database = new SqliteFileDatabase();
        var id = await Setup(database, Input());
        await using var context = database.Open();
        var service = new ForecastMonthService(context);
        var missing = (await service.ReadAsync(id, 1, 1))!;
        Assert.Equal(new[] { ForecastInputKind.Gross, ForecastInputKind.Insurance, ForecastInputKind.Dependents }, missing.Missing);
        Assert.Null(missing.Result);
        Assert.Null(missing.Status);
        var input = Input() with { Bonuses = [] };
        input.Months[0] = new(1, Value(0m), Value(0m), Value(0), Value(0m, ForecastStatus.Assumed));
        await new ForecastStore(context).UpdateAsync(id, 1, ForecastDraftReader.Encode(input));
        var knownZero = (await service.ReadAsync(id, 2, 1))!;
        Assert.Empty(knownZero.Missing);
        Assert.Equal(0m, knownZero.Result!.Pit.TotalTax.Value);
        Assert.Equal(0m, knownZero.Difference);
        Assert.Equal(ForecastStatus.Actual, knownZero.Status); // Withholding status does not change obligation status.
        Assert.Equal(ForecastStatus.Assumed, knownZero.Input.Withheld!.Status);
    }

    [Fact]
    public async Task Stale_deleted_and_missing_documents_never_fall_back_to_another_revision()
    {
        using var database = new SqliteFileDatabase();
        var id = await Setup(database, Input());
        ForecastMonthView first;
        await using (var context = database.Open()) first = (await new ForecastMonthService(context).ReadAsync(id, 1, 6))!;
        var original = JsonSerializer.Serialize(first);
        await using (var context = database.Open())
        {
            var changed = Input();
            changed.Months[5] = changed.Months[5] with { Insurance = Value(4_000_000m) };
            await new ForecastStore(context).UpdateAsync(id, 1, ForecastDraftReader.Encode(changed));
        }
        await using (var context = database.Open())
        {
            var service = new ForecastMonthService(context);
            await Assert.ThrowsAsync<ForecastRevisionConflictException>(() => service.ReadAsync(id, 1, 6));
            var current = (await service.ReadAsync(id, 2, 6))!;
            Assert.Equal(365_000m, current.Result!.Pit.TotalTax.Value);
            Assert.Equal(original, JsonSerializer.Serialize(first));
            Assert.Null(await service.ReadAsync(Guid.NewGuid(), 1, 6));
            await new ForecastStore(context).DeleteAsync(id, 2);
        }
        await using (var context = database.Open()) Assert.Null(await new ForecastMonthService(context).ReadAsync(id, 2, 6));
    }

    [Theory]
    [InlineData("DELETE FROM AnnualForecastRules WHERE PeriodFrom = '2026-01-01'")]
    [InlineData("UPDATE AnnualForecastRules SET Graph = 'invalid' WHERE PeriodFrom = '2026-01-01'")]
    [InlineData("DELETE FROM TaxRuleSnapshot")]
    public async Task Missing_or_changed_stored_catalog_is_not_replaced_by_core_defaults(string sql)
    {
        using var database = new SqliteFileDatabase();
        var id = await Setup(database, Input());
        await using var context = database.Open();
#pragma warning disable EF1002 // Only constant InlineData SQL in this synthetic test.
        await context.Database.ExecuteSqlRawAsync(sql);
#pragma warning restore EF1002
        await Assert.ThrowsAsync<InvalidOperationException>(() => new ForecastMonthService(context).ReadAsync(id, 1, 6));
    }

    [Fact]
    public async Task Empty_lookup_still_requires_the_original_key_and_sentinel()
    {
        using var database = new SqliteFileDatabase();
        await Setup(database, Input());
        var options = new DbContextOptionsBuilder<TaxManagerDbContext>()
            .UseSqlite($"Data Source={database.DatabasePath};Foreign Keys=True;Pooling=False").Options;
        await using (var noKey = new TaxManagerDbContext(options))
            await Assert.ThrowsAsync<DataProtectionException>(() => new ForecastMonthService(noKey).ReadAsync(Guid.NewGuid(), 1, 6));
        using var other = new SqliteFileDatabase();
        await using (var wrongKey = new TaxManagerDbContext(options, other.Keys))
            await Assert.ThrowsAsync<DataProtectionException>(() => new ForecastMonthService(wrongKey).ReadAsync(Guid.NewGuid(), 1, 6));
        await using (var context = database.Open()) await context.Database.ExecuteSqlRawAsync("DELETE FROM DataProtectionMetadata");
        await using (var context = database.Open())
            await Assert.ThrowsAsync<DataProtectionException>(() => new ForecastMonthService(context).ReadAsync(Guid.NewGuid(), 1, 6));
    }

    [Fact]
    public async Task Included_bonus_cannot_exceed_gross_and_pending_changes_cannot_be_used_as_saved_input()
    {
        using var database = new SqliteFileDatabase();
        var input = Input();
        input.Months[5] = input.Months[5] with { Gross = Value(1_000_000m) };
        var id = await Setup(database, input);
        await using var context = database.Open();
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => new ForecastMonthService(context).ReadAsync(id, 1, 6));
        var document = await context.Set<ForecastDocument>().SingleAsync();
        document.Payload = ForecastDraftReader.Encode(Input());
        await Assert.ThrowsAsync<InvalidOperationException>(() => new ForecastMonthService(context).ReadAsync(id, 1, 6));
    }
}
