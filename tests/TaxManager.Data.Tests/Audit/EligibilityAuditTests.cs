using Microsoft.EntityFrameworkCore;
using TaxManager.Data.Audit;
using TaxManager.Data.Dependents;
using TaxManager.Data.Tests.Fixtures;
using Xunit;

namespace TaxManager.Data.Tests.Audit;

public class EligibilityAuditTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DetachedUpdateAndKeyOnlyDeleteAuditActualDatabaseBeforeState(bool async)
    {
        using var db = new SqliteFileDatabase();
        await SyntheticGraph.SeedAsync(db);
        DependentEligibilityPeriod stored;
        await using (var c = db.Open()) stored = await c.DependentEligibilityPeriods.AsNoTracking().SingleAsync();
        await using (var c = db.Open())
        {
            c.Update(new DependentEligibilityPeriod
            {
                ProfileId = stored.ProfileId, DependentId = stored.DependentId,
                EligibleFromMonth = stored.EligibleFromMonth, EligibleToMonth = new(2026, 11, 1)
            });
            if (async) await c.SaveChangesAsync(); else c.SaveChanges();
        }
        await using (var c = db.Open())
        {
            Assert.Equal(new DateOnly(2026, 11, 1), (await c.DependentEligibilityPeriods.SingleAsync()).EligibleToMonth);
            var update = c.ReadAudit(await c.Set<AuditEvent>().AsNoTracking().SingleAsync(e => e.Action == "Update"));
            Assert.Equal("2026-12-01", update.Before!.Value.GetProperty("EligibleToMonth").GetString());
            Assert.Equal("2026-11-01", update.After!.Value.GetProperty("EligibleToMonth").GetString());
        }
        await using (var c = db.Open())
        {
            c.Remove(new DependentEligibilityPeriod
            {
                ProfileId = stored.ProfileId, DependentId = stored.DependentId, EligibleFromMonth = stored.EligibleFromMonth
            });
            if (async) await c.SaveChangesAsync(); else c.SaveChanges();
        }
        await using var verify = db.Open();
        Assert.Empty(await verify.DependentEligibilityPeriods.ToListAsync());
        var deleted = verify.ReadAudit(await verify.Set<AuditEvent>().AsNoTracking().SingleAsync(e => e.Action == "Delete"));
        Assert.Equal("2026-11-01", deleted.Before!.Value.GetProperty("EligibleToMonth").GetString());
        Assert.Null(deleted.After);
        Assert.Equal(6, await verify.Set<AuditEvent>().CountAsync());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task LoadedPeriodRejectsTrueStaleUpdateAndDelete(bool async, bool delete)
    {
        using var db = new SqliteFileDatabase();
        await SyntheticGraph.SeedAsync(db);
        await using var stale = db.Open();
        var period = await stale.DependentEligibilityPeriods.SingleAsync();
        await using (var writer = db.Open())
        {
            (await writer.DependentEligibilityPeriods.SingleAsync()).EligibleToMonth = new(2026, 11, 1);
            await writer.SaveChangesAsync();
        }
        if (delete) stale.Remove(period); else period.EligibleToMonth = new(2026, 10, 1);
        var failure = async
            ? await Assert.ThrowsAsync<InvalidOperationException>(() => stale.SaveChangesAsync())
            : Assert.Throws<InvalidOperationException>(() => stale.SaveChanges());
        Assert.Contains("changed since it was loaded", failure.Message);
        await using var verify = db.Open();
        Assert.Equal(new DateOnly(2026, 11, 1), (await verify.DependentEligibilityPeriods.SingleAsync()).EligibleToMonth);
        Assert.Equal(5, await verify.Set<AuditEvent>().CountAsync());
    }
}
