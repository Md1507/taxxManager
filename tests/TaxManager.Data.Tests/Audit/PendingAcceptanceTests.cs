using Microsoft.EntityFrameworkCore;
using TaxManager.Data.Audit;
using TaxManager.Data.Profiles;
using TaxManager.Data.Tests.Fixtures;
using Xunit;

namespace TaxManager.Data.Tests.Audit;

public class PendingAcceptanceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AcceptAllChangesAllowsImmediateEditWithoutInterveningSave(bool async)
    {
        using var db = new SqliteFileDatabase();
        await using var c = db.Open();
        await c.Database.MigrateAsync();
        var profile = new UserProfile { DisplayName = "Synthetic first" };
        c.Add(profile);
        if (async) await c.SaveChangesAsync(false); else c.SaveChanges(false);
        Assert.Equal(EntityState.Added, c.Entry(profile).State);
        c.ChangeTracker.AcceptAllChanges();
        profile.DisplayName = "Synthetic changed";
        if (async) await c.SaveChangesAsync(); else c.SaveChanges();

        await using var verify = db.Open();
        Assert.Equal("Synthetic changed", (await verify.UserProfiles.SingleAsync()).DisplayName);
        var events = await verify.Set<AuditEvent>().AsNoTracking().ToListAsync();
        Assert.Equal(2, events.Count);
        var update = verify.ReadAudit(Assert.Single(events, e => e.Action == "Update"));
        Assert.Equal("Synthetic first", update.Before!.Value.GetProperty("DisplayName").GetString());
        Assert.Equal("Synthetic changed", update.After!.Value.GetProperty("DisplayName").GetString());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ClearAllowsImmediateNewAddWithoutInterveningSave(bool async)
    {
        using var db = new SqliteFileDatabase();
        await using var c = db.Open();
        await c.Database.MigrateAsync();
        c.Add(new UserProfile { DisplayName = "Synthetic first" });
        if (async) await c.SaveChangesAsync(false); else c.SaveChanges(false);
        c.ChangeTracker.Clear();
        c.Add(new UserProfile { DisplayName = "Synthetic second" });
        if (async) await c.SaveChangesAsync(); else c.SaveChanges();

        await using var verify = db.Open();
        Assert.Equal(2, await verify.UserProfiles.CountAsync());
        Assert.Equal(2, await verify.Set<AuditEvent>().CountAsync());
        Assert.Equal(new[] { "Synthetic first", "Synthetic second" },
            (await verify.UserProfiles.AsNoTracking().ToListAsync()).Select(p => p.DisplayName).Order());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task SavedModifiedEntryCanBeEditedImmediatelyAfterAcceptOrClear(bool async, bool clear)
    {
        using var db = new SqliteFileDatabase();
        await SyntheticGraph.SeedAsync(db);
        await using var c = db.Open();
        var income = await c.MonthlyIncomes.SingleAsync();
        income.GrossIncome = 5;
        if (async) await c.SaveChangesAsync(false); else c.SaveChanges(false);
        if (clear) c.ChangeTracker.Clear(); else c.ChangeTracker.AcceptAllChanges();
        income.GrossIncome = 6;
        if (clear) c.Update(income); // Reuse the same CLR object in a new tracking lifetime.
        if (async) await c.SaveChangesAsync(); else c.SaveChanges();

        await using var verify = db.Open();
        Assert.Equal(6, (await verify.MonthlyIncomes.SingleAsync()).GrossIncome);
        Assert.Equal(6, await verify.Set<AuditEvent>().CountAsync());
        var updates = await verify.Set<AuditEvent>().AsNoTracking().Where(e => e.Action == "Update").ToListAsync();
        Assert.Equal(2, updates.Count);
        Assert.Contains(updates, e => verify.ReadAudit(e).Before!.Value.GetProperty("GrossIncome").GetString() == "5" &&
            verify.ReadAudit(e).After!.Value.GetProperty("GrossIncome").GetString() == "6");
    }

    [Theory]
    [InlineData(false, "Add")]
    [InlineData(true, "Add")]
    [InlineData(false, "Update")]
    [InlineData(true, "Update")]
    [InlineData(false, "Delete")]
    [InlineData(true, "Delete")]
    public async Task UnacceptedEntriesStillRejectAnotherSaveWithoutDuplicateAudit(bool async, string action)
    {
        using var db = new SqliteFileDatabase();
        var id = await SyntheticGraph.SeedAsync(db);
        await using var c = db.Open();
        var income = action == "Add" ? SyntheticGraph.Income(id, 2) : await c.MonthlyIncomes.SingleAsync();
        if (action == "Add") c.Add(income);
        else if (action == "Delete") c.Remove(income);
        else income.GrossIncome = 5;
        if (async) await c.SaveChangesAsync(false); else c.SaveChanges(false);
        var failure = async
            ? await Assert.ThrowsAsync<InvalidOperationException>(() => c.SaveChangesAsync())
            : Assert.Throws<InvalidOperationException>(() => c.SaveChanges());
        Assert.Contains("AcceptAllChanges", failure.Message);
        await using var verify = db.Open();
        Assert.Equal(5, await verify.Set<AuditEvent>().CountAsync());
        Assert.Equal(action == "Add" ? 2 : action == "Delete" ? 0 : 1, await verify.MonthlyIncomes.CountAsync());
        if (action == "Update") Assert.Equal(5, (await verify.MonthlyIncomes.SingleAsync()).GrossIncome);
    }
}
