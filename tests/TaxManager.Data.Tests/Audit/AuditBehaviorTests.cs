using Microsoft.EntityFrameworkCore;
using TaxManager.Data.Audit;
using TaxManager.Data.Dependents;
using TaxManager.Data.Income;
using TaxManager.Data.Profiles;
using TaxManager.Data.Tests.Fixtures;
using Xunit;

namespace TaxManager.Data.Tests.Audit;

public class AuditBehaviorTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ChangesHaveEncryptedBeforeAfterCallerAndUtcButNoOpsDoNotAudit(bool async)
    {
        using var db = new SqliteFileDatabase();
        var id = await SyntheticGraph.SeedAsync(db);
        await using var c = db.Open();
        var operation = Guid.NewGuid();
        c.AuditContext = new("caller-marker", "purpose-marker", "correlation-marker", operation);
        var income = await c.MonthlyIncomes.SingleAsync();
        var dependent = await c.Dependents.SingleAsync();
        var period = await c.DependentEligibilityPeriods.SingleAsync();
        income.GrossIncome = 9.25m;
        dependent.DisplayName = "Synthetic changed";
        period.EligibleToMonth = new(2026, 11, 1);
        if (async) await c.SaveChangesAsync(); else c.SaveChanges();
        var events = await c.Set<AuditEvent>().AsNoTracking().Where(e => e.OperationId == operation).ToListAsync();
        Assert.Equal(3, events.Count);
        foreach (var e in events)
        {
            Assert.Equal(id, e.ProfileId); Assert.Equal("Update", e.Action);
            Assert.Equal(TimeSpan.Zero, e.TimestampUtc.Offset);
            var payload = c.ReadAudit(e);
            Assert.Equal("caller-marker", payload.Actor); Assert.Equal("purpose-marker", payload.Context);
            Assert.Equal("correlation-marker", payload.Correlation);
            Assert.NotEqual(payload.Before!.Value.GetRawText(), payload.After!.Value.GetRawText());
        }
        c.AuditContext = null; // No-op needs no actor.
        c.Entry(income).State = EntityState.Modified;
        income.GrossIncome = 10; income.GrossIncome = 9.25m;
        if (async) Assert.Equal(0, await c.SaveChangesAsync()); else Assert.Equal(0, c.SaveChanges());
        Assert.Equal(7, await c.Set<AuditEvent>().CountAsync());
        var transient = SyntheticGraph.Income(id, 2);
        c.Add(transient); c.Remove(transient);
        Assert.Equal(0, await c.SaveChangesAsync());
    }

    [Fact]
    public async Task CallerMetadataIsRequiredAndAuditSetCannotAppendUpdateOrDelete()
    {
        using var db = new SqliteFileDatabase();
        var id = await SyntheticGraph.SeedAsync(db);
        await using (var c = db.Open())
        {
            c.AuditContext = null;
            (await c.MonthlyIncomes.SingleAsync()).ActualWithheldTax = 99;
            await Assert.ThrowsAsync<InvalidOperationException>(() => c.SaveChangesAsync());
        }
        foreach (var action in new[] { "Add", "Modify", "Delete" })
        {
            await using var c = db.Open();
            if (action == "Add") c.Add(new AuditEvent());
            else
            {
                var e = await c.Set<AuditEvent>().FirstAsync();
                c.Entry(e).State = action == "Modify" ? EntityState.Modified : EntityState.Deleted;
            }
            await Assert.ThrowsAsync<InvalidOperationException>(() => c.SaveChangesAsync());
        }
        await using var verify = db.Open();
        Assert.Equal(1234567890.123456789m, (await verify.MonthlyIncomes.SingleAsync()).GrossIncome);
        Assert.Equal(4, await verify.Set<AuditEvent>().CountAsync());
    }

    [Fact]
    public async Task DetachedUpdateUsesStoredBeforeStateAndStaleTrackedWriteIsRejected()
    {
        using var db = new SqliteFileDatabase();
        var id = await SyntheticGraph.SeedAsync(db);
        await using var stale = db.Open();
        var staleIncome = await stale.MonthlyIncomes.SingleAsync();
        await using (var detached = db.Open())
        {
            detached.Update(SyntheticGraph.Income(id, amount: 7));
            await detached.SaveChangesAsync();
            var audit = await detached.Set<AuditEvent>().AsNoTracking().SingleAsync(e => e.Action == "Update");
            Assert.Contains("1234567890.123456789", detached.ReadAudit(audit).Before!.Value.GetRawText());
        }
        staleIncome.InsuranceAmount = 3;
        await Assert.ThrowsAsync<InvalidOperationException>(() => stale.SaveChangesAsync());
        await stale.Entry(staleIncome).ReloadAsync();
        Assert.Equal(7, staleIncome.GrossIncome);
        staleIncome.InsuranceAmount = 3;
        await stale.SaveChangesAsync();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SaveFalseRequiresAcceptanceAndOperationReuseDoesNotDuplicate(bool async)
    {
        using var db = new SqliteFileDatabase();
        await SyntheticGraph.SeedAsync(db);
        await using var c = db.Open();
        var income = await c.MonthlyIncomes.SingleAsync();
        var operation = Guid.NewGuid();
        c.AuditContext = new("synthetic", "save-false", OperationId: operation);
        income.GrossIncome = 5;
        if (async) await c.SaveChangesAsync(false); else c.SaveChanges(false);
        Assert.Equal(EntityState.Modified, c.Entry(income).State);
        await Assert.ThrowsAsync<InvalidOperationException>(() => c.SaveChangesAsync());
        c.ChangeTracker.AcceptAllChanges();
        Assert.Equal(0, await c.SaveChangesAsync());
        income.GrossIncome = 6;
        await Assert.ThrowsAsync<InvalidOperationException>(() => c.SaveChangesAsync());
        await using var verify = db.Open();
        Assert.Equal(5, (await verify.MonthlyIncomes.SingleAsync()).GrossIncome);
        Assert.Single(await verify.Set<AuditEvent>().Where(e => e.OperationId == operation).ToListAsync());
    }

    [Fact]
    public async Task UnloadedCascadeIsAuditedOrdinaryDeleteRetainsAuditPurgeRemovesOnlyTarget()
    {
        using var db = new SqliteFileDatabase();
        var a = await SyntheticGraph.SeedAsync(db); var b = await SyntheticGraph.SeedAsync(db);
        await using (var c = db.Open()) await new ProfileDeletionService(c).DeleteAsync(a);
        await using (var c = db.Open())
        {
            Assert.Equal(8, await c.Set<AuditEvent>().CountAsync(e => e.ProfileId == a));
            var deletes = await c.Set<AuditEvent>().AsNoTracking().Where(e => e.ProfileId == a && e.Action == "Delete").ToListAsync();
            Assert.Equal(4, deletes.Count);
            Assert.All(deletes, e => Assert.NotNull(c.ReadAudit(e).Before));
        }
        await using (var c = db.Open()) await new ProfilePurgeService(c).PurgeAsync(a, new("operator", "synthetic purge"));
        await using var verify = db.Open();
        Assert.Empty(await verify.Set<AuditEvent>().Where(e => e.ProfileId == a).ToListAsync());
        Assert.Equal(4, await verify.Set<AuditEvent>().CountAsync(e => e.ProfileId == b));
        Assert.Equal(b, (await verify.UserProfiles.SingleAsync()).Id);
        Assert.Single(await verify.MonthlyIncomes.ToListAsync()); Assert.Single(await verify.Dependents.ToListAsync());
    }
}
