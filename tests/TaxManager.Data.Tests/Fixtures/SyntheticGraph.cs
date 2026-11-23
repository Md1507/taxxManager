using Microsoft.EntityFrameworkCore;
using TaxManager.Data.Dependents;
using TaxManager.Data.Income;
using TaxManager.Data.Profiles;

namespace TaxManager.Data.Tests.Fixtures;

internal static class SyntheticGraph
{
    internal static MonthlyIncome Income(Guid profile, int month = 1, decimal amount = 1234567890.123456789m) =>
        new() { ProfileId = profile, Year = 2026, Month = month, GrossIncome = amount, InsuranceAmount = 0.1m, ActualWithheldTax = 2m };

    internal static async Task<Guid> SeedAsync(SqliteFileDatabase database)
    {
        await using var context = database.Open();
        await context.Database.MigrateAsync();
        var profile = new UserProfile { DisplayName = "Synthetic Nguyễn SECRET_NAME_849372" };
        var dependent = new Dependent { ProfileId = profile.Id, DisplayName = "Synthetic DEPENDENT_SECRET_238721" };
        context.AddRange(profile, dependent, Income(profile.Id), new DependentEligibilityPeriod
        {
            ProfileId = profile.Id, DependentId = dependent.Id, EligibleFromMonth = new(2026, 1, 1), EligibleToMonth = new(2026, 12, 1)
        });
        await context.SaveChangesAsync();
        return profile.Id;
    }
}
