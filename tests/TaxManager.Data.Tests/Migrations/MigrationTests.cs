using Microsoft.EntityFrameworkCore;
using TaxManager.Data.Tests.Fixtures;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TaxManager.Data.Profiles;
using TaxManager.Data.Income;
using TaxManager.Data.Dependents;
using Xunit;

namespace TaxManager.Data.Tests.Migrations;

public class MigrationTests
{
    [Fact]
    public async Task MigrationOnNewFileIsIdempotent()
    {
        using var database = new SqliteFileDatabase();
        await using var context = database.Open();
        await context.Database.MigrateAsync();
        await context.Database.MigrateAsync();
        Assert.Equal(3, (await context.Database.GetAppliedMigrationsAsync()).Count());
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
    }
    [Fact]
    public async Task UpgradeAndDownUpPreserveInitialDataAndTriggers()
    {
        using var database = new SqliteFileDatabase();
        var profile = Guid.NewGuid();
        var dependent = Guid.NewGuid();
        await using var context = database.Open();
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync("20261122100144_InitialPersistence");
        await context.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO UserProfiles (Id, DisplayName) VALUES ({profile}, {'S'.ToString()})");
        await context.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO MonthlyIncomes VALUES ({profile}, 2025, 1, '123.45', '1', '2')");
        await context.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Dependents VALUES ({profile}, {dependent}, {'D'.ToString()})");
        await context.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO DependentEligibilityPeriods VALUES ({profile}, {dependent}, '2025-01-01', '2025-12-01')");
        await migrator.MigrateAsync("20261122161649_AddMonthlyTaxRules");
        await migrator.MigrateAsync("20261122100144_InitialPersistence");
        await migrator.MigrateAsync("20261122161649_AddMonthlyTaxRules");
        await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(() => migrator.MigrateAsync());
        Assert.Equal(2, (await context.Database.GetAppliedMigrationsAsync()).Count());
        await context.Database.OpenConnectionAsync();
        await using (var check = context.Database.GetDbConnection().CreateCommand())
        {
            check.CommandText = "SELECT DisplayName || ':' || (SELECT GrossIncome FROM MonthlyIncomes) || ':' || (SELECT DisplayName FROM Dependents) || ':' || (SELECT count(*) FROM DependentEligibilityPeriods) FROM UserProfiles";
            Assert.Equal("S:123.45:D:1", await check.ExecuteScalarAsync());
        }
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT count(*) FROM sqlite_master WHERE type='trigger' AND name LIKE 'Period_NoOverlap_%'";
        Assert.Equal(2L, (long)(await command.ExecuteScalarAsync())!);
    }
}
