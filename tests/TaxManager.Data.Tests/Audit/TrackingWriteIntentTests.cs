using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TaxManager.Data.Audit;
using TaxManager.Data.Dependents;
using TaxManager.Data.Income;
using TaxManager.Data.Infrastructure;
using TaxManager.Data.Profiles;
using TaxManager.Data.Tests.Fixtures;
using Xunit;

namespace TaxManager.Data.Tests.Audit;

public class TrackingWriteIntentTests
{
    public static IEnumerable<object[]> EntityCases =>
        from kind in new[] { "Profile", "Dependent", "Income", "Period" }
        from async in new[] { false, true }
        select new object[] { kind, async };

    public static IEnumerable<object[]> SetterCases =>
        from kind in new[] { "Profile", "Dependent", "Income", "Period" }
        from edit in kind is "Profile" or "Dependent" ? new[] { "name", "null", "empty" } :
            kind == "Income" ? new[] { "gross", "insurance", "tax" } : new[] { "end" }
        from async in new[] { false, true }
        select new object[] { kind, edit, async };

    public static IEnumerable<object[]> ReadOnlyCases =>
        from entityCase in EntityCases
        from addChild in new[] { false, true }
        select new object[] { entityCase[0], entityCase[1], addChild };

    [Theory]
    [MemberData(nameof(EntityCases))]
    public async Task KeyOnlyAttachNoOpPreservesStoredValuesAndAudit(string kind, bool async)
    {
        using var db = new SqliteFileDatabase();
        var keys = await Seed(db);
        string before;
        await using (var c = db.Open()) before = Snapshot(await Load(c, kind));
        await using (var c = db.Open())
        {
            var reference = Reference(kind, keys);
            c.Attach(reference);
            c.AuditContext = null;
            Assert.Equal(EntityState.Unchanged, c.Entry(reference).State);
            Assert.False(c.ChangeTracker.HasChanges());
            Assert.Equal(0, await Save(c, async));
        }
        await using var verify = db.Open();
        Assert.Equal(before, Snapshot(await Load(verify, kind)));
        Assert.Equal(4, await verify.Set<AuditEvent>().CountAsync());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task AttachedPrincipalAddingChildDoesNotWriteOrAuditParent(bool async, bool dependent)
    {
        using var db = new SqliteFileDatabase();
        var keys = await Seed(db);
        var kind = dependent ? "Dependent" : "Profile";
        string before;
        await using (var c = db.Open()) before = Snapshot(await Load(c, kind));
        await using (var c = db.Open())
        {
            var parent = Reference(kind, keys);
            c.Attach(parent);
            if (dependent) c.Add(new DependentEligibilityPeriod
            {
                ProfileId = keys.Profile, DependentId = keys.Dependent,
                EligibleFromMonth = new(2027, 1, 1), EligibleToMonth = new(2027, 12, 1)
            });
            else c.Add(SyntheticGraph.Income(keys.Profile, 2));
            Assert.Equal(1, await Save(c, async));
            Assert.Equal(EntityState.Unchanged, c.Entry(parent).State);
        }
        await using var verify = db.Open();
        Assert.Equal(before, Snapshot(await Load(verify, kind)));
        var events = await verify.Set<AuditEvent>().AsNoTracking().ToListAsync();
        Assert.Equal(5, events.Count);
        Assert.All(events, e => Assert.Equal("Add", e.Action));
        Assert.Equal(2, dependent ? await verify.DependentEligibilityPeriods.CountAsync() : await verify.MonthlyIncomes.CountAsync());
    }

    [Theory]
    [MemberData(nameof(ReadOnlyCases))]
    public async Task ReadOnlyEntityChangedByAnotherContextDoesNotBecomeStaleWrite(string kind, bool async, bool addChild)
    {
        using var db = new SqliteFileDatabase();
        var keys = await Seed(db);
        await using var reader = db.Open();
        var readOnly = await Load(reader, kind);
        string changed;
        await using (var writer = db.Open())
        {
            var entity = await Load(writer, kind);
            Edit(entity);
            changed = Snapshot(entity);
            await writer.SaveChangesAsync();
        }
        Assert.Equal(EntityState.Unchanged, reader.Entry(readOnly).State);
        Assert.False(reader.ChangeTracker.HasChanges());
        if (addChild) reader.Add(SyntheticGraph.Income(keys.Profile, 2));
        else reader.AuditContext = null;
        Assert.Equal(addChild ? 1 : 0, await Save(reader, async));
        Assert.Equal(EntityState.Unchanged, reader.Entry(readOnly).State);
        await using var verify = db.Open();
        var persisted = kind == "Income" ? await verify.MonthlyIncomes.SingleAsync(x => x.Month == 1) : await Load(verify, kind);
        Assert.Equal(changed, Snapshot(persisted));
        Assert.Equal(addChild ? 6 : 5, await verify.Set<AuditEvent>().CountAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MultipleAmountSettersAfterAttachRemainIntentAndEqualSettersAreNoOp(bool async)
    {
        using var db = new SqliteFileDatabase();
        var keys = await Seed(db);
        await using (var c = db.Open())
        {
            var reference = (MonthlyIncome)Reference("Income", keys);
            c.Attach(reference);
            reference.GrossIncome = 10m;
            reference.InsuranceAmount = 3m;
            reference.ActualWithheldTax = 4m;
            Assert.Equal(1, await Save(c, async));
            reference.GrossIncome = 10m;
            reference.InsuranceAmount = 3m;
            reference.ActualWithheldTax = 4m;
            c.AuditContext = null;
            Assert.Equal(0, await Save(c, async));
        }
        await using var verify = db.Open();
        var income = await verify.MonthlyIncomes.SingleAsync();
        Assert.Equal(10m, income.GrossIncome);
        Assert.Equal(3m, income.InsuranceAmount);
        Assert.Equal(4m, income.ActualWithheldTax);
        var payload = verify.ReadAudit(await verify.Set<AuditEvent>().AsNoTracking().SingleAsync(e => e.Action == "Update"));
        Assert.Equal("0.1", payload.Before!.Value.GetProperty("InsuranceAmount").GetString());
        Assert.Equal("2", payload.Before!.Value.GetProperty("ActualWithheldTax").GetString());
        Assert.Equal(Snapshot(income), payload.After!.Value.GetRawText());
        Assert.Equal(5, await verify.Set<AuditEvent>().CountAsync());
    }

    [Theory]
    [MemberData(nameof(SetterCases))]
    public async Task ExplicitSetterAfterAttachUpdatesOnlyIntendedFieldWithActualAudit(string kind, string field, bool async)
    {
        using var db = new SqliteFileDatabase();
        var keys = await Seed(db);
        string before, after;
        await using (var c = db.Open())
        {
            var expected = await Load(c, kind);
            before = Snapshot(expected);
            Edit(expected, field);
            after = Snapshot(expected);
        }
        await using (var c = db.Open())
        {
            var reference = Reference(kind, keys);
            c.Attach(reference);
            Edit(reference, field);
            Assert.Equal(1, await Save(c, async));
            Assert.Equal(after, Snapshot(reference));
        }
        await using var verify = db.Open();
        Assert.Equal(after, Snapshot(await Load(verify, kind)));
        var update = await verify.Set<AuditEvent>().AsNoTracking().SingleAsync(e => e.Action == "Update");
        var payload = verify.ReadAudit(update);
        Assert.Equal(before, payload.Before!.Value.GetRawText());
        Assert.Equal(after, payload.After!.Value.GetRawText());
        Assert.Equal(5, await verify.Set<AuditEvent>().CountAsync());
    }

    [Theory]
    [MemberData(nameof(EntityCases))]
    public async Task DetachedUpdateThenKeyOnlyRemoveUsesActualDatabaseState(string kind, bool async)
    {
        using var db = new SqliteFileDatabase();
        var keys = await Seed(db);
        object entity;
        string before, after;
        await using (var c = db.Open())
        {
            entity = await Load(c, kind);
            before = Snapshot(entity);
        }
        Edit(entity);
        after = Snapshot(entity);
        await using (var c = db.Open())
        {
            c.Update(entity);
            Assert.Equal(1, await Save(c, async));
        }
        await using (var c = db.Open())
        {
            var payload = c.ReadAudit(await c.Set<AuditEvent>().AsNoTracking().SingleAsync(e => e.Action == "Update"));
            Assert.Equal(before, payload.Before!.Value.GetRawText());
            Assert.Equal(after, payload.After!.Value.GetRawText());
            c.Remove(Reference(kind, keys));
            await Save(c, async);
        }
        await using var verify = db.Open();
        var name = entity.GetType().Name;
        var deleted = await verify.Set<AuditEvent>().AsNoTracking().SingleAsync(e => e.Entity == name && e.Action == "Delete");
        var deletion = verify.ReadAudit(deleted);
        Assert.Equal(after, deletion.Before!.Value.GetRawText());
        Assert.Null(deletion.After);
        var deleteCount = kind == "Profile" ? 4 : kind == "Dependent" ? 2 : 1;
        Assert.Equal(5 + deleteCount, await verify.Set<AuditEvent>().CountAsync());
    }

    [Theory]
    [MemberData(nameof(EntityCases))]
    public async Task ActuallyEditedLoadedEntityStillRejectsConcurrentChange(string kind, bool async)
    {
        using var db = new SqliteFileDatabase();
        await Seed(db);
        await using var stale = db.Open();
        var entity = await Load(stale, kind);
        await using (var writer = db.Open())
        {
            Edit(await Load(writer, kind));
            await writer.SaveChangesAsync();
        }
        Edit(entity, "other");
        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => Save(stale, async));
        Assert.Contains("changed since it was loaded", failure.Message);
        await using var verify = db.Open();
        Assert.Equal(5, await verify.Set<AuditEvent>().CountAsync());
    }

    private static async Task<(Guid Profile, Guid Dependent)> Seed(SqliteFileDatabase db)
    {
        var profile = await SyntheticGraph.SeedAsync(db);
        await using var c = db.Open();
        return (profile, await c.Dependents.Select(x => x.Id).SingleAsync());
    }

    private static object Reference(string kind, (Guid Profile, Guid Dependent) keys) => kind switch
    {
        "Profile" => new UserProfile { Id = keys.Profile },
        "Dependent" => new Dependent { ProfileId = keys.Profile, Id = keys.Dependent },
        "Income" => new MonthlyIncome { ProfileId = keys.Profile, Year = 2026, Month = 1 },
        _ => new DependentEligibilityPeriod { ProfileId = keys.Profile, DependentId = keys.Dependent, EligibleFromMonth = new(2026, 1, 1) }
    };

    private static async Task<object> Load(TaxManagerDbContext c, string kind) => kind switch
    {
        "Profile" => await c.UserProfiles.SingleAsync(),
        "Dependent" => await c.Dependents.SingleAsync(),
        "Income" => await c.MonthlyIncomes.SingleAsync(),
        _ => await c.DependentEligibilityPeriods.SingleAsync()
    };

    private static void Edit(object entity, string field = "default")
    {
        var name = field == "null" ? null : field == "empty" ? "" : "Synthetic changed " + field;
        switch (entity)
        {
            case UserProfile p: p.DisplayName = name; break;
            case Dependent d: d.DisplayName = name; break;
            case MonthlyIncome i:
                if (field == "insurance") i.InsuranceAmount = 7.25m;
                else if (field == "tax") i.ActualWithheldTax = 8.5m;
                else i.GrossIncome = field == "other" ? 20m : 10m;
                break;
            case DependentEligibilityPeriod p: p.EligibleToMonth = new(2026, field == "other" ? 10 : 11, 1); break;
        }
    }

    private static string Snapshot(object entity) => entity switch
    {
        UserProfile p => JsonSerializer.Serialize(new { p.DisplayName }),
        Dependent d => JsonSerializer.Serialize(new { d.DisplayName }),
        MonthlyIncome i => JsonSerializer.Serialize(new
        {
            GrossIncome = i.GrossIncome.ToString("G29", CultureInfo.InvariantCulture),
            InsuranceAmount = i.InsuranceAmount.ToString("G29", CultureInfo.InvariantCulture),
            ActualWithheldTax = i.ActualWithheldTax.ToString("G29", CultureInfo.InvariantCulture)
        }),
        DependentEligibilityPeriod p => JsonSerializer.Serialize(new { p.EligibleFromMonth, p.EligibleToMonth }),
        _ => throw new ArgumentException(nameof(entity))
    };

    private static Task<int> Save(TaxManagerDbContext c, bool async) =>
        async ? c.SaveChangesAsync() : Task.FromResult(c.SaveChanges());
}
