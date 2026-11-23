using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TaxManager.Data.CalculationHistory;
using TaxManager.Data.Protection;
using TaxManager.Data.TaxRules;
using TaxManager.Data.Tests.Fixtures;
using Xunit;

namespace TaxManager.Data.Tests.CalculationHistory;

public class SnapshotRoundTripTests
{
    [Theory]
    [InlineData("vi-VN")]
    [InlineData("en-US")]
    public async Task ReopenedKeyAndFreshReadsPreserveEntireDocumentAfterSourcesChangeAndDisappear(string culture)
    {
        using var db = new SqliteFileDatabase();
        var id = await HistoryFixture.SeedAsync(db, true);
        var original = await HistoryFixture.CreateAsync(db, id, CalculationSourceMode.Official);
        byte[] ciphertext;
        await using (var c = db.Open())
        {
            ciphertext = (await c.CalculationHistory.SingleAsync()).PayloadEncrypted;
            var income = await c.MonthlyIncomes.SingleAsync();
            income.GrossIncome = 78901.2m; income.InsuranceAmount = 3.4m; income.ActualWithheldTax = 5.6m;
            // FromMonth is part of the key; use delete/insert instead.
            c.Remove(await c.DependentEligibilityPeriods.SingleAsync());
            await c.SaveChangesAsync();
            var rule = await c.Set<TaxRuleSnapshot>().SingleAsync();
            rule.PersonalDeduction = 800m;
            var basis = await c.Set<TaxRuleLegalBasis>().FirstAsync();
            basis.Summary = "SYNTHETIC_CHANGED_AFTER_HISTORY";
            (await c.Set<TaxRulePeriod>().SingleAsync()).PeriodTo = new(2026, 11, 30);
            await c.SaveChangesAsync();
        }
        var next = await HistoryFixture.CreateAsync(db, id, CalculationSourceMode.Official);
        Assert.Equal(78901.2m, next.Input.GrossIncome); Assert.Equal(0, next.Input.DependentCount);
        Assert.Equal(800m, next.Rules.PersonalDeduction);
        Assert.NotEqual(original.Result.TotalTax.Value, next.Result.TotalTax.Value);
        await using (var c = db.Open())
        {
            c.Remove(await c.MonthlyIncomes.SingleAsync());
            c.Remove(await c.Dependents.SingleAsync());
            await c.SaveChangesAsync();
            c.RemoveRange(await c.Set<TaxRulePeriod>().ToListAsync());
            c.RemoveRange(await c.Set<TaxRuleBracket>().ToListAsync());
            c.RemoveRange(await c.Set<TaxRuleSnapshot>().ToListAsync());
            c.RemoveRange(await c.Set<TaxRuleLegalBasis>().ToListAsync());
            await c.SaveChangesAsync();
        }
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            using var keys = FileKeyRing.Load(db.KeyPath, Path.GetFullPath("."), Path.GetDirectoryName(db.DatabasePath)!);
            await using var fresh = HistoryFixture.Open(db, keys);
            var read = await new CalculationHistoryService(fresh).ReadAsync(id, original.HistoryId);
            Assert.Equal(JsonSerializer.Serialize(original), JsonSerializer.Serialize(read));
            var tracked = await fresh.CalculationHistory.SingleAsync(h => h.Id == original.HistoryId);
            Assert.Equal(ciphertext, tracked.PayloadEncrypted);
            var copy = tracked.PayloadEncrypted; copy[0] ^= 0xFF;
            Assert.Equal(ciphertext, tracked.PayloadEncrypted);
            await fresh.Entry(tracked).ReloadAsync();
            Assert.Equal(JsonSerializer.Serialize(original), JsonSerializer.Serialize(tracked.Snapshot));
            Assert.Equal(0, await fresh.SaveChangesAsync());
            Assert.Equal("CalculationSnapshot", original.ToString());
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [Theory]
    [InlineData("0.0000000000000000000000000001")]
    [InlineData("0.1")]
    [InlineData("12345678.123456789012345678901")]
    public async Task ExactAmountsAndNegativeClampSurviveFreshContext(string text)
    {
        using var db = new SqliteFileDatabase();
        var id = await HistoryFixture.SeedAsync(db);
        var value = decimal.Parse(text, CultureInfo.InvariantCulture);
        await using (var c = db.Open())
        {
            var income = await c.MonthlyIncomes.SingleAsync();
            income.GrossIncome = value; income.InsuranceAmount = value; income.ActualWithheldTax = value;
            await c.SaveChangesAsync();
        }
        var created = await HistoryFixture.CreateAsync(db, id);
        await using var fresh = db.Open();
        var read = (await new CalculationHistoryService(fresh).ReadAsync(id, created.HistoryId))!;
        Assert.Equal(value, read.Input.GrossIncome); Assert.Equal(value, read.Input.ManualInsurance);
        Assert.Equal(value, read.Input.ActualWithheldTax); Assert.True(read.Result.TaxableIncomeBeforeClamp.Value < 0);
        Assert.Equal(0m, read.Result.TaxableIncome.Value); Assert.Equal(0m, read.Result.TotalTax.Value);
        Assert.Null(read.Rules.Brackets[^1].UpperBound);
    }
}
