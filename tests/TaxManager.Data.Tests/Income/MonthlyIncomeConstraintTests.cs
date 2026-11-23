using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TaxManager.Data.Profiles;
using TaxManager.Data.Tests.Fixtures;
using TaxManager.Data.Protection;
using Xunit;

namespace TaxManager.Data.Tests.Income;

public class MonthlyIncomeConstraintTests
{
    [Fact]
    public async Task SqliteEnforcesOwnerMonthUniquenessAndCalendarBounds()
    {
        using var db = new SqliteFileDatabase();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        await using var c = db.Open();
        await c.Database.MigrateAsync();
        c.UserProfiles.AddRange(new UserProfile { Id = a }, new UserProfile { Id = b });
        await c.SaveChangesAsync();
        await c.Database.OpenConnectionAsync();
        await using var cmd = c.Database.GetDbConnection().CreateCommand();

        async Task<int> Execute(string sql)
        {
            cmd.CommandText = sql;
            return await cmd.ExecuteNonQueryAsync();
        }

        var databaseId = (await c.Set<DataProtectionMetadata>().AsNoTracking().SingleAsync()).DatabaseId;
        string Insert(Guid owner, int year, int month)
        {
            string Cipher(string field) => "X'" + Convert.ToHexString(db.Keys.Protect("0"u8,
                new ProtectionContext(databaseId, owner, "MonthlyIncome", $"{year:D4}/{month:D2}", field))) + "'";
            return "INSERT INTO MonthlyIncomes (ProfileId,Year,Month,GrossIncomeEncrypted,InsuranceAmountEncrypted,ActualWithheldTaxEncrypted) " +
                $"VALUES ('{owner.ToString().ToUpperInvariant()}',{year},{month}," +
                $"{Cipher("GrossIncome")},{Cipher("InsuranceAmount")},{Cipher("ActualWithheldTax")})";
        }

        var income = Insert(a, 2026, 1);
        await Execute(income);
        await Assert.ThrowsAsync<SqliteException>(() => Execute(income));
        await Assert.ThrowsAsync<SqliteException>(() => Execute(Insert(Guid.NewGuid(), 2026, 1)));
        foreach (var (year, month) in new[] { (0, 1), (10000, 1), (2026, 0), (2026, 13) })
        {
            await Assert.ThrowsAsync<SqliteException>(() => Execute(Insert(a, year, month)));
        }
        await Execute(Insert(b, 2026, 1));
    }
}
