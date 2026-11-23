using Microsoft.EntityFrameworkCore;
using TaxManager.Core;
using TaxManager.Data.Income;
using TaxManager.Data.Profiles;
using TaxManager.Data.Tests.Fixtures;
using Xunit;

namespace TaxManager.Data.Tests.Income;

public class SalaryInputMapperTests
{
    [Fact]
    public async Task TwelvePersistedMonthsMapFromFreshContext()
    {
        using var db = new SqliteFileDatabase();
        var id = Guid.NewGuid();
        await using (var writer = db.Open())
        {
            await writer.Database.MigrateAsync();
            writer.UserProfiles.Add(new UserProfile { Id = id });
            for (int month = 1; month <= 12; month++)
            {
                writer.MonthlyIncomes.Add(new MonthlyIncome
                {
                    ProfileId = id,
                    Year = 2026,
                    Month = month,
                    GrossIncome = month + 0.1m,
                    InsuranceAmount = 1m,
                    ActualWithheldTax = 2m
                });
            }
            await writer.SaveChangesAsync();
        }

        await using var reader = db.Open();
        var incomes = await reader.MonthlyIncomes.Where(x => x.ProfileId == id)
            .OrderBy(x => x.Month).ToListAsync();
        var values = incomes.Select(SalaryInputMapper.ToCoreInput).ToArray();
        Assert.Equal(12, values.Length);
        for (int i = 0; i < 12; i++)
        {
            Assert.Equal(i + 1, values[i].Month);
            Assert.Equal(i + 1 + 0.1m, values[i].GrossIncome);
            Assert.Equal(1m, values[i].InsuranceAmount);
            Assert.Equal(2m, values[i].ActualWithheldTax);
        }
    }

    [Fact]
    public void UnsupportedInputIsRejected()
    {
        var id = Guid.NewGuid();
        var automatic = new SalaryMonthInput(2026, 1, 1m, 0m,
            insuranceInput: new InsuranceInput(default, default, 1m));
        Assert.Throws<ArgumentException>(() => SalaryInputMapper.FromCoreInput(id, automatic));
        var bonus = new CashBonus("synthetic", 1m, new DateOnly(2026, 1, 1),
            CashBonusTreatment.TaxableCashFromCurrentEmployer);
        var withBonus = new SalaryMonthInput(2026, 1, 1m, 0m, 0m, bonuses: [bonus]);
        Assert.Throws<ArgumentException>(() => SalaryInputMapper.FromCoreInput(id, withBonus));
    }

    [Fact]
    public void ManualInputRoundtripsWithoutSynthesizingMonths()
    {
        var id = Guid.NewGuid();
        var input = new SalaryMonthInput(2026, 2, 100.1m, 12.3m, 5.5m);
        var saved = SalaryInputMapper.FromCoreInput(id, input);
        var result = SalaryInputMapper.ToCoreInput(saved);
        Assert.Equal(id, saved.ProfileId);
        Assert.Equal(input.Year, result.Year);
        Assert.Equal(input.Month, result.Month);
        Assert.Equal(input.GrossIncome, result.GrossIncome);
        Assert.Equal(input.InsuranceAmount, result.InsuranceAmount);
        Assert.Equal(input.ActualWithheldTax, result.ActualWithheldTax);
        Assert.Empty(result.Bonuses);
        Assert.Empty(result.Extras);
    }
}
