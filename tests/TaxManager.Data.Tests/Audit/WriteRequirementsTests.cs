using Microsoft.EntityFrameworkCore;
using TaxManager.Data.Infrastructure;
using TaxManager.Data.Profiles;
using TaxManager.Data.Tests.Fixtures;
using Xunit;

namespace TaxManager.Data.Tests.Audit;

public class WriteRequirementsTests
{
    [Fact]
    public async Task DisabledForeignKeysRejectSaveAndPurgeBeforeAnyDeletion()
    {
        using var db = new SqliteFileDatabase(); var id = await SyntheticGraph.SeedAsync(db);
        var options = new DbContextOptionsBuilder<TaxManagerDbContext>()
            .UseSqlite($"Data Source={db.DatabasePath};Foreign Keys=False;Pooling=False").Options;
        await using (var c = new TaxManagerDbContext(options, db.Keys))
        {
            c.Add(new UserProfile());
            await Assert.ThrowsAsync<InvalidOperationException>(() => c.SaveChangesAsync());
        }
        await using (var c = new TaxManagerDbContext(options, db.Keys))
            await Assert.ThrowsAsync<InvalidOperationException>(() => new ProfilePurgeService(c).PurgeAsync(id, new("synthetic", "test")));
        await using var verify = db.Open();
        Assert.Single(await verify.UserProfiles.ToListAsync()); Assert.Single(await verify.MonthlyIncomes.ToListAsync());
        Assert.Single(await verify.Dependents.ToListAsync()); Assert.Single(await verify.DependentEligibilityPeriods.ToListAsync());
    }
}
