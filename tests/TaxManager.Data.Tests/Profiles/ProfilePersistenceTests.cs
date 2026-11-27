using Microsoft.EntityFrameworkCore;
using TaxManager.Data.Dependents;
using TaxManager.Data.Income;
using TaxManager.Data.Profiles;
using TaxManager.Data.Tests.Fixtures;
using Xunit;

namespace TaxManager.Data.Tests.Profiles;

public class ProfilePersistenceTests
{
    [Fact]
    public async Task FreshContextReadsProfileIncomeAndDependentRelations()
    {
        using var db = new SqliteFileDatabase();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var dependent = Guid.NewGuid();
        await using (var writer = db.Open())
        {
            await writer.Database.MigrateAsync();
            writer.UserProfiles.AddRange(new UserProfile { Id = a }, new UserProfile { Id = b, DisplayName = "Synthetic" });
            writer.MonthlyIncomes.AddRange(
                new MonthlyIncome
                {
                    ProfileId = a,
                    Year = 2026,
                    Month = 1,
                    GrossIncome = 0.1m,
                    InsuranceAmount = 1m,
                    ActualWithheldTax = 2m
                },
                new MonthlyIncome
                {
                    ProfileId = b,
                    Year = 2026,
                    Month = 1,
                    GrossIncome = 0,
                    InsuranceAmount = 0,
                    ActualWithheldTax = 0
                });
            writer.Dependents.Add(new Dependent { ProfileId = a, Id = dependent });
            writer.DependentEligibilityPeriods.Add(new DependentEligibilityPeriod
            {
                ProfileId = a,
                DependentId = dependent,
                EligibleFromMonth = new(2025, 12, 1),
                EligibleToMonth = new(2026, 2, 1)
            });
            await writer.SaveChangesAsync();
            await writer.Database.MigrateAsync();
        }

        await using (var reader = db.Open())
        {
            Assert.Equal(
                ["20261122100144_InitialPersistence", "20261122161649_AddMonthlyTaxRules", "20261123034403_AddDataProtectionAndAudit", "20261123045552_AddCalculationHistory", "20261126101030_AddForecastDocuments", "20261126101326_AddForecastSnapshots", "20261127043040_AddAnnualRuleGraph", "20261127102957_AddAnnualForecastResults"],
                await reader.Database.GetAppliedMigrationsAsync());
            Assert.Empty(await reader.Database.GetPendingMigrationsAsync());
            Assert.Null((await reader.UserProfiles.SingleAsync(x => x.Id == a)).DisplayName);
            var income = await reader.MonthlyIncomes.AsNoTracking().SingleAsync(x => x.ProfileId == a);
            Assert.Equal(0.1m, income.GrossIncome);
            Assert.Equal(1m, income.InsuranceAmount);
            Assert.Single(await reader.Dependents.Where(x => x.ProfileId == a).ToListAsync());
            var period = await reader.DependentEligibilityPeriods.SingleAsync(x => x.ProfileId == a);
            Assert.Equal(new DateOnly(2026, 2, 1), DependentPeriodMapper.ToCorePeriod(period).EligibleToMonth);
            Assert.Equal(0.1m, SalaryInputMapper.ToCoreInput(income).GrossIncome);
            reader.UserProfiles.Remove(await reader.UserProfiles.SingleAsync(x => x.Id == a));
            await reader.SaveChangesAsync();
        }

        await using var verify = db.Open();
        Assert.Single(await verify.UserProfiles.ToListAsync());
        Assert.Single(await verify.MonthlyIncomes.ToListAsync());
        Assert.Empty(await verify.Dependents.ToListAsync());
        Assert.Empty(await verify.DependentEligibilityPeriods.ToListAsync());
    }
}
