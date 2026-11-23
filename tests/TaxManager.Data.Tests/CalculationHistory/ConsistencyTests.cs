using Microsoft.EntityFrameworkCore;
using TaxManager.Data.CalculationHistory;
using TaxManager.Data.TaxRules;
using TaxManager.Data.Tests.Fixtures;
using Xunit;

namespace TaxManager.Data.Tests.CalculationHistory;

public class ConsistencyTests
{
    [Fact]
    public async Task WriterLockPrecedesSourceReadsAndAllSourcesUseOneTransaction()
    {
        using var db = new SqliteFileDatabase();
        var id = await HistoryFixture.SeedAsync(db);
        var probe = new ConsistencyProbeInterceptor(db.DatabasePath);
        await using (var c = db.Open(probe))
            await new CalculationHistoryService(c).CreateMonthlyAsync(id, 2026, 1, CalculationSourceMode.References);
        Assert.True(probe.Attempted); Assert.True(probe.SourceReads > 5);
        await using var fresh = db.Open();
        Assert.Equal(TaxManager.Core.TaxRuleSet.Vietnam2026.PersonalDeduction,
            (await fresh.Set<TaxRuleSnapshot>().SingleAsync(r => r.FromYear == 2026)).PersonalDeduction);
    }

    [Fact]
    public async Task UnchangedTrackedSourcesCannotOverrideFreshPersistedReads()
    {
        using var db = new SqliteFileDatabase();
        var id = await HistoryFixture.SeedAsync(db);
        await using var reader = db.Open();
        var stale = await reader.MonthlyIncomes.SingleAsync();
        await using (var writer = db.Open())
        {
            (await writer.MonthlyIncomes.SingleAsync()).GrossIncome = 777m;
            await writer.SaveChangesAsync();
        }
        var snapshot = await new CalculationHistoryService(reader).CreateMonthlyAsync(id, 2026, 1, CalculationSourceMode.References);
        Assert.Equal(777m, snapshot.Input.GrossIncome);
        Assert.Equal(1234567890.123456789m, stale.GrossIncome);
        Assert.Equal(0, await reader.SaveChangesAsync());
        await using var fresh = db.Open();
        Assert.Equal(777m, (await fresh.MonthlyIncomes.SingleAsync()).GrossIncome);
    }
}
