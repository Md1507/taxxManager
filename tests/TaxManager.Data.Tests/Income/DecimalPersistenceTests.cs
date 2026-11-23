using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TaxManager.Data.Income;
using TaxManager.Data.Profiles;
using TaxManager.Data.Tests.Fixtures;
using Xunit;

namespace TaxManager.Data.Tests.Income;

public class DecimalPersistenceTests
{
    [Theory]
    [InlineData("en-US")]
    [InlineData("vi-VN")]
    public async Task AllAmountsRoundtripAndUpdateExactlyInEachCulture(string cultureName)
    {
        var oldCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            using var db = new SqliteFileDatabase();
            var id = Guid.NewGuid();
            decimal[] amounts = [0m, 0.1m, 10000000.1m, 1234567890.123456789m,
                0.0000000000000000000000000001m, decimal.MaxValue];
            await using (var writer = db.Open())
            {
                await writer.Database.MigrateAsync();
                writer.UserProfiles.Add(new UserProfile { Id = id });
                for (int i = 0; i < amounts.Length; i++)
                {
                    writer.MonthlyIncomes.Add(new MonthlyIncome
                    {
                        ProfileId = id,
                        Year = 2026,
                        Month = i + 1,
                        GrossIncome = amounts[i],
                        InsuranceAmount = amounts[i],
                        ActualWithheldTax = amounts[i]
                    });
                }
                await writer.SaveChangesAsync();
            }

            await using (var reader = db.Open())
            {
                var items = await reader.MonthlyIncomes.OrderBy(x => x.Month).ToListAsync();
                for (int i = 0; i < amounts.Length; i++)
                {
                    Assert.Equal(amounts[i], items[i].GrossIncome);
                    Assert.Equal(amounts[i], items[i].InsuranceAmount);
                    Assert.Equal(amounts[i], items[i].ActualWithheldTax);
                }
                for (int i = 0; i < amounts.Length; i++)
                {
                    items[i].GrossIncome = amounts[amounts.Length - i - 1];
                    items[i].InsuranceAmount = amounts[amounts.Length - i - 1];
                    items[i].ActualWithheldTax = amounts[amounts.Length - i - 1];
                }
                await reader.SaveChangesAsync();
            }

            await using var verify = db.Open();
            var updated = await verify.MonthlyIncomes.OrderBy(x => x.Month).ToListAsync();
            for (int i = 0; i < amounts.Length; i++)
            {
                Assert.Equal(amounts[amounts.Length - i - 1], updated[i].GrossIncome);
                Assert.Equal(amounts[amounts.Length - i - 1], updated[i].InsuranceAmount);
                Assert.Equal(amounts[amounts.Length - i - 1], updated[i].ActualWithheldTax);
            }
            await verify.Database.OpenConnectionAsync();
            await using var command = verify.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT typeof(GrossIncomeEncrypted) || ',' || typeof(InsuranceAmountEncrypted) || ',' || " +
                "typeof(ActualWithheldTaxEncrypted) FROM MonthlyIncomes LIMIT 1";
            Assert.Equal("blob,blob,blob", await command.ExecuteScalarAsync());
        }
        finally
        {
            CultureInfo.CurrentCulture = oldCulture;
        }
    }

    [Fact]
    public async Task NegativeValuesAreRejectedOnSyncAndAsyncSave()
    {
        using var db = new SqliteFileDatabase();
        await using var context = db.Open();
        await context.Database.MigrateAsync();
        var income = new MonthlyIncome
        {
            ProfileId = Guid.NewGuid(),
            Year = 2026,
            Month = 1,
            GrossIncome = -1,
            InsuranceAmount = 0,
            ActualWithheldTax = 0
        };
        context.MonthlyIncomes.Add(income);
        Assert.Throws<ArgumentException>(() => context.SaveChanges());
        income.GrossIncome = 0;
        income.InsuranceAmount = -1;
        await Assert.ThrowsAsync<ArgumentException>(() => context.SaveChangesAsync());
        income.InsuranceAmount = 0;
        income.ActualWithheldTax = -1;
        Assert.Throws<ArgumentException>(() => context.SaveChanges());
    }
}
