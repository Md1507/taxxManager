using Microsoft.EntityFrameworkCore;
using TaxManager.Data.Income;
using TaxManager.Data.Profiles;
using TaxManager.Data.Tests.Fixtures;
using Xunit;

namespace TaxManager.Data.Tests.Income;

public class IncomeValidationTests
{
    [Fact]
    public async Task MissingAmountsAreRejectedButExplicitZeroIsSaved()
    {
        using var database = new SqliteFileDatabase();
        await using var context = database.Open();
        await context.Database.MigrateAsync();
        var profile = new UserProfile();
        context.UserProfiles.Add(profile);
        var income = new MonthlyIncome { ProfileId = profile.Id, Year = 2026, Month = 1 };
        context.MonthlyIncomes.Add(income);
        Assert.Throws<ArgumentException>(() => context.SaveChanges());
        income.GrossIncome = 0;
        income.InsuranceAmount = 0;
        await Assert.ThrowsAsync<ArgumentException>(() => context.SaveChangesAsync());
        income.ActualWithheldTax = 0;
        await context.SaveChangesAsync();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NegativeAmountsAreRejectedForAddedAndModifiedRecords(bool asyncSave)
    {
        using var database = new SqliteFileDatabase();
        await using var context = database.Open();
        await context.Database.MigrateAsync();
        var profile = new UserProfile();
        context.UserProfiles.Add(profile);
        var income = new MonthlyIncome
        {
            ProfileId = profile.Id,
            Year = 2026,
            Month = 1,
            GrossIncome = 0,
            InsuranceAmount = 0,
            ActualWithheldTax = 0
        };
        context.MonthlyIncomes.Add(income);
        for (int state = 0; state < 2; state++)
        {
            for (int field = 0; field < 3; field++)
            {
                if (field == 0)
                {
                    income.GrossIncome = -1;
                }
                if (field == 1)
                {
                    income.InsuranceAmount = -1;
                }
                if (field == 2)
                {
                    income.ActualWithheldTax = -1;
                }
                if (asyncSave)
                {
                    await Assert.ThrowsAsync<ArgumentException>(() => context.SaveChangesAsync());
                }
                else
                {
                    Assert.Throws<ArgumentException>(() => context.SaveChanges());
                }
                income.GrossIncome = 0;
                income.InsuranceAmount = 0;
                income.ActualWithheldTax = 0;
            }
            if (state == 0)
            {
                await context.SaveChangesAsync();
            }
        }
    }

    [Fact]
    public void EmptyProfileIdIsRejectedBeforeTracking()
    {
        Assert.Throws<ArgumentException>(() => new UserProfile { Id = Guid.Empty });
    }
}
