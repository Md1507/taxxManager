using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TaxManager.Data.Dependents;
using TaxManager.Data.Profiles;
using TaxManager.Data.Tests.Fixtures;
using Xunit;

namespace TaxManager.Data.Tests.Dependents;

public class DependentPeriodConstraintTests
{
    [Fact]
    public async Task SqliteEnforcesDatesOwnershipAndOverlapOnInsertAndUpdate()
    {
        using var db = new SqliteFileDatabase();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var dependent = Guid.NewGuid();
        await using var context = db.Open();
        await context.Database.MigrateAsync();
        context.UserProfiles.AddRange(new UserProfile { Id = a }, new UserProfile { Id = b });
        context.Dependents.Add(new Dependent { ProfileId = a, Id = dependent });
        await context.SaveChangesAsync();
        await context.Database.OpenConnectionAsync();
        await using var command = context.Database.GetDbConnection().CreateCommand();

        async Task<int> Execute(string sql)
        {
            command.CommandText = sql;
            return await command.ExecuteNonQueryAsync();
        }

        string Insert(Guid owner, Guid person, string values) =>
            $"INSERT INTO DependentEligibilityPeriods VALUES ('{owner.ToString().ToUpperInvariant()}'," +
            $"'{person.ToString().ToUpperInvariant()}',{values})";

        command.CommandText = "PRAGMA foreign_keys";
        Assert.Equal(1L, await command.ExecuteScalarAsync());
        await Assert.ThrowsAsync<SqliteException>(() => Execute(Insert(b, dependent, "'2026-01-01','2026-02-01'")));
        var otherDependent = Guid.NewGuid();
        context.Dependents.AddRange(
            new Dependent { ProfileId = b, Id = dependent },
            new Dependent { ProfileId = a, Id = otherDependent });
        await context.SaveChangesAsync();
        await Execute(Insert(b, dependent, "'2026-01-01','2026-03-01'"));
        await Execute(Insert(a, otherDependent, "'2026-01-01','2026-03-01'"));
        await Execute(Insert(a, dependent, "'2026-01-01','2026-03-01'"));
        await Execute(Insert(a, dependent, "'2026-04-01','2026-06-01'"));
        await Assert.ThrowsAsync<SqliteException>(() => Execute(Insert(a, dependent, "'2026-03-01','2026-04-01'")));
        await Assert.ThrowsAsync<SqliteException>(() => Execute(Insert(a, dependent, "'2026-02-01','2026-02-01'")));
        await Execute(Insert(a, dependent, "'0001-01-01','0001-01-01'"));
        await Execute(Insert(a, dependent, "'9999-12-01','9999-12-01'"));
        var update = "UPDATE DependentEligibilityPeriods SET EligibleToMonth='2026-04-01' " +
            "WHERE EligibleFromMonth='2026-01-01'";
        await Assert.ThrowsAsync<SqliteException>(() => Execute(update));
        string[] invalidDates =
        [
            "'2026-07-02','2026-08-01'", "'2026-13-01','2026-13-01'",
            "'2026-09-01','2026-08-01'", "'0000-01-01','0000-01-01'",
            "'2026-00-01','2026-00-01'", "NULL,'2026-08-01'"
        ];
        foreach (var values in invalidDates)
        {
            await Assert.ThrowsAsync<SqliteException>(() => Execute(Insert(a, dependent, values)));
        }
    }
}
