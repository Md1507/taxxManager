using Microsoft.EntityFrameworkCore;
using TaxManager.Data.Audit;
using TaxManager.Data.CalculationHistory;
using TaxManager.Data.Protection;
using TaxManager.Data.TaxRules;
using TaxManager.Data.Tests.Fixtures;
using Xunit;

namespace TaxManager.Data.Tests.CalculationHistory;

public class CalculationFailureTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OversizePayloadOrArithmeticOverflowDoesNotLeavePartialHistory(bool overflow)
    {
        using var db = new SqliteFileDatabase();
        var id = await HistoryFixture.SeedAsync(db, true);
        await using (var c = db.Open())
        {
            if (overflow)
            {
                var income = await c.MonthlyIncomes.SingleAsync();
                income.GrossIncome = 0; income.InsuranceAmount = decimal.MaxValue;
            }
            else (await c.Set<TaxRuleLegalBasis>().FirstAsync()).Summary = new string('s', 4 * 1024 * 1024);
            await c.SaveChangesAsync();
        }
        await using (var c = db.Open())
        {
            var count = await c.Set<AuditEvent>().CountAsync();
            var call = new Func<Task>(() => new CalculationHistoryService(c).CreateMonthlyAsync(id, 2026, 1, CalculationSourceMode.Official));
            if (overflow) await Assert.ThrowsAsync<OverflowException>(call);
            else await Assert.ThrowsAsync<DataProtectionException>(call);
            Assert.Empty(c.ChangeTracker.Entries());
            Assert.Equal(count, await c.Set<AuditEvent>().CountAsync());
            Assert.Empty(await c.CalculationHistory.ToListAsync());
        }
    }
}
