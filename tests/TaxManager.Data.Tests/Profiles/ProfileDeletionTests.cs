using Microsoft.EntityFrameworkCore;
using TaxManager.Data.Dependents;
using TaxManager.Data.Income;
using TaxManager.Data.Profiles;
using TaxManager.Data.Tests.Fixtures;
using Xunit;

namespace TaxManager.Data.Tests.Profiles;

public class ProfileDeletionTests
{
    [Fact]
    public async Task DeletingDependentAndOwnerPreservesOtherOwnersCompleteGraph()
    {
        using var database = new SqliteFileDatabase();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var dependentA = Guid.NewGuid();
        var dependentB = Guid.NewGuid();
        var secondDependentA = Guid.NewGuid();
        await using (var writer = database.Open())
        {
            await writer.Database.MigrateAsync();
            foreach (var (owner, dependent) in new[] { (a, dependentA), (b, dependentB) })
            {
                writer.UserProfiles.Add(new UserProfile { Id = owner });
                writer.MonthlyIncomes.Add(new MonthlyIncome
                {
                    ProfileId = owner,
                    Year = 2026,
                    Month = 1,
                    GrossIncome = 1,
                    InsuranceAmount = 2,
                    ActualWithheldTax = 3
                });
                writer.Dependents.Add(new Dependent { ProfileId = owner, Id = dependent });
                writer.DependentEligibilityPeriods.Add(new DependentEligibilityPeriod
                {
                    ProfileId = owner,
                    DependentId = dependent,
                    EligibleFromMonth = new(2026, 1, 1),
                    EligibleToMonth = new(2026, 2, 1)
                });
            }
            writer.Dependents.Add(new Dependent { ProfileId = a, Id = secondDependentA });
            writer.DependentEligibilityPeriods.Add(new DependentEligibilityPeriod
            {
                ProfileId = a,
                DependentId = secondDependentA,
                EligibleFromMonth = new(2026, 1, 1),
                EligibleToMonth = new(2026, 2, 1)
            });
            await writer.SaveChangesAsync();
        }
        await using (var editor = database.Open())
        {
            Assert.Equal(2, await editor.Dependents.CountAsync(x => x.ProfileId == a));
            Assert.Single(await editor.DependentEligibilityPeriods.Where(x => x.ProfileId == b).ToListAsync());
            editor.Dependents.Remove(await editor.Dependents.SingleAsync(x => x.ProfileId == a && x.Id == dependentA));
            await editor.SaveChangesAsync();
            Assert.Single(await editor.DependentEligibilityPeriods.Where(x => x.ProfileId == a).ToListAsync());
            Assert.Single(await editor.MonthlyIncomes.Where(x => x.ProfileId == a).ToListAsync());
            editor.UserProfiles.Remove(await editor.UserProfiles.SingleAsync(x => x.Id == a));
            await editor.SaveChangesAsync();
        }
        await using var reader = database.Open();
        Assert.Single(await reader.UserProfiles.ToListAsync());
        Assert.Single(await reader.MonthlyIncomes.Where(x => x.ProfileId == b).ToListAsync());
        Assert.Single(await reader.Dependents.Where(x => x.ProfileId == b).ToListAsync());
        Assert.Single(await reader.DependentEligibilityPeriods.Where(x => x.ProfileId == b).ToListAsync());
        Assert.Empty(await reader.MonthlyIncomes.Where(x => x.ProfileId == a).ToListAsync());
        Assert.Empty(await reader.Dependents.Where(x => x.ProfileId == a).ToListAsync());
        Assert.Empty(await reader.DependentEligibilityPeriods.Where(x => x.ProfileId == a).ToListAsync());
    }
}