using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TaxManager.Data.TaxRules;

namespace TaxManager.Data.Tests.Fixtures;

internal static class LegacySyntheticDatabase
{
    internal static async Task<(Guid Profile, Guid Dependent)> SeedAsync(SqliteFileDatabase db)
    {
        await using var context = db.Open();
        await context.GetService<IMigrator>().MigrateAsync("20261122161649_AddMonthlyTaxRules");
        var p = Guid.NewGuid(); var d = Guid.NewGuid();
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO UserProfiles (Id, DisplayName) VALUES ({p}, {"Synthetic Nguyễn LEGACY_SECRET_734981"})");
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO MonthlyIncomes VALUES ({p},2026,1,{"1234567890.123456789"},{"0.0000000000000000000000000001"},{"79228162514264337593543950335"})");
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO Dependents VALUES ({p},{d},{""})");
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO DependentEligibilityPeriods VALUES ({p},{d},'2026-01-01','2026-12-01')");
        await new TaxRuleStore(context).SeedReferencesAsync();
        return (p, d);
    }
}
