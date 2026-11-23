using Microsoft.EntityFrameworkCore;
using TaxManager.Data.CalculationHistory;
using TaxManager.Data.Tests.Fixtures;
using Xunit;

namespace TaxManager.Data.Tests.CalculationHistory;

public class ImmutabilityTests
{
    [Theory]
    [InlineData(false, "add")]
    [InlineData(true, "add")]
    [InlineData(false, "update")]
    [InlineData(true, "update")]
    [InlineData(false, "detached-update")]
    [InlineData(true, "detached-update")]
    [InlineData(false, "remove")]
    [InlineData(true, "remove")]
    [InlineData(false, "payload")]
    [InlineData(true, "payload")]
    [InlineData(false, "month")]
    [InlineData(true, "month")]
    public async Task CallerCannotForgeAppendOrModifyOrDeleteHistory(bool async, string mutation)
    {
        using var db = new SqliteFileDatabase();
        var id = await HistoryFixture.SeedAsync(db);
        var snapshot = await HistoryFixture.CreateAsync(db, id);
        await using var c = db.Open();
        var record = await c.CalculationHistory.SingleAsync();
        var bytes = record.PayloadEncrypted;
        switch (mutation)
        {
            case "add": c.Entry(record).State = EntityState.Detached; c.Add(record); break;
            case "update": c.Update(record); break;
            case "detached-update": c.Entry(record).State = EntityState.Detached; c.Update(record); break;
            case "remove": c.Remove(record); break;
            case "payload": c.Entry(record).Property(x => x.PayloadEncrypted).CurrentValue = [1, 2, 3]; break;
            case "month": c.Entry(record).Property(x => x.Month).CurrentValue = 2; break;
        }
        if (async) await Assert.ThrowsAsync<InvalidOperationException>(() => c.SaveChangesAsync(false));
        else Assert.Throws<InvalidOperationException>(() => c.SaveChanges(false));
        await using var fresh = db.Open();
        Assert.Equal(bytes, (await fresh.CalculationHistory.SingleAsync()).PayloadEncrypted);
        Assert.NotNull(await new CalculationHistoryService(fresh).ReadAsync(id, snapshot.HistoryId));
    }

    [Fact]
    public async Task FakeBlankRecordAndKeyMutationAreRejected()
    {
        using var db = new SqliteFileDatabase();
        var id = await HistoryFixture.SeedAsync(db);
        await HistoryFixture.CreateAsync(db, id);
        await using var c = db.Open();
        var fake = Activator.CreateInstance(typeof(CalculationRecord), nonPublic: true)!;
        c.Add(fake);
        await Assert.ThrowsAsync<InvalidOperationException>(() => c.SaveChangesAsync());
        c.ChangeTracker.Clear();
        var record = await c.CalculationHistory.SingleAsync();
        Assert.Throws<InvalidOperationException>(() => c.Entry(record).Property(h => h.Id).CurrentValue = Guid.NewGuid());
    }
}
