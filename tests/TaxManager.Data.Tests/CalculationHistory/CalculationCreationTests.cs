using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TaxManager.Core;
using TaxManager.Data.Audit;
using TaxManager.Data.CalculationHistory;
using TaxManager.Data.Dependents;
using TaxManager.Data.TaxRules;
using TaxManager.Data.Tests.Fixtures;
using Xunit;

namespace TaxManager.Data.Tests.CalculationHistory;

public class CalculationCreationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PersistedInputIsCalculatedByCoreWithCompleteSnapshotAndAudit(bool official)
    {
        using var db = new SqliteFileDatabase();
        var id = await HistoryFixture.SeedAsync(db, official);
        var other = await SyntheticGraph.SeedAsync(db);
        await using (var c = db.Open())
        {
            var d = await c.Dependents.SingleAsync(d => d.ProfileId == id);
            var period = await c.DependentEligibilityPeriods.SingleAsync(p => p.ProfileId == id);
            period.EligibleToMonth = new(2026, 1, 1); // inclusive final month
            c.Add(new DependentEligibilityPeriod { ProfileId = id, DependentId = d.Id,
                EligibleFromMonth = new(2026, 2, 1), EligibleToMonth = new(2026, 3, 1) });
            var future = new Dependent { ProfileId = id };
            c.Add(future);
            c.Add(new DependentEligibilityPeriod { ProfileId = id, DependentId = future.Id,
                EligibleFromMonth = new(2026, 2, 1), EligibleToMonth = new(2026, 3, 1) });
            await c.SaveChangesAsync();
        }
        var snapshot = await HistoryFixture.CreateAsync(db, id,
            official ? CalculationSourceMode.Official : CalculationSourceMode.References);
        var rules = official ? HistoryFixture.Rules() : TaxRuleSet.Vietnam2026;
        var expected = MonthlySalaryCalculator.Calculate(1234567890.123456789m, .1m, 1, rules);
        Assert.Equal(id, snapshot.ProfileId);
        Assert.Equal(2026, snapshot.Year); Assert.Equal(1, snapshot.Month); Assert.Equal(1, snapshot.SchemaVersion);
        Assert.Equal(HistoryFixture.Now, snapshot.CreatedAtUtc); Assert.Equal(TimeSpan.Zero, snapshot.CreatedAtUtc.Offset);
        Assert.Equal(1234567890.123456789m, snapshot.Input.GrossIncome);
        Assert.Equal(.1m, snapshot.Input.ManualInsurance); Assert.Equal(2m, snapshot.Input.ActualWithheldTax);
        Assert.Equal(0m, snapshot.Input.ExemptIncome); Assert.Equal(1, snapshot.Input.DependentCount);
        Assert.Single(snapshot.Input.Dependents);
        Assert.Equal(new DateOnly(2026, 1, 1), snapshot.Input.Dependents[0].ToMonth);
        Assert.Equal(rules.Id, snapshot.Rules.RuleId); Assert.Equal(rules.Version, snapshot.Rules.Version);
        Assert.Equal(rules.Period, snapshot.Rules.Period); Assert.Equal(rules.FromYear, snapshot.Rules.FromYear);
        Assert.Equal(rules.Brackets, snapshot.Rules.Brackets);
        Assert.Equal(rules.PersonalDeductionBasis, snapshot.Rules.PersonalDeductionBasis);
        Assert.Equal(rules.DependentDeductionBasis, snapshot.Rules.DependentDeductionBasis);
        Assert.Equal(rules.TaxableIncomeBasis, snapshot.Rules.TaxableIncomeBasis);
        Assert.Equal(rules.TaxBasis, snapshot.Rules.TaxBasis);
        Assert.Equal(expected.GrossIncome.Value, snapshot.Result.GrossIncome.Value);
        Assert.Equal(LegalBasis.Missing, snapshot.Result.GrossIncome.Basis);
        Assert.Equal(expected.Insurance.Value, snapshot.Result.Insurance.Value);
        Assert.Equal(LegalBasis.Missing, snapshot.Result.Insurance.Basis);
        Assert.Equal(expected.PersonalDeduction.Value, snapshot.Result.PersonalDeduction.Value);
        Assert.Equal(expected.PersonalDeduction.Basis, snapshot.Result.PersonalDeduction.Basis);
        Assert.Equal(expected.DependentDeduction.Value, snapshot.Result.DependentDeduction.Value);
        Assert.Equal(expected.DependentDeduction.Basis, snapshot.Result.DependentDeduction.Basis);
        Assert.Equal(expected.TaxableIncomeBeforeClamp.Value, snapshot.Result.TaxableIncomeBeforeClamp.Value);
        Assert.Equal(expected.TaxableIncomeBeforeClamp.Basis, snapshot.Result.TaxableIncomeBeforeClamp.Basis);
        Assert.Equal(expected.TaxableIncome.Value, snapshot.Result.TaxableIncome.Value);
        Assert.Equal(expected.TaxableIncome.Basis, snapshot.Result.TaxableIncome.Basis);
        Assert.Equal(expected.Pit.TaxableIncome.Value, snapshot.Result.PitTaxableIncome.Value);
        Assert.Equal(expected.Pit.TaxableIncome.Basis, snapshot.Result.PitTaxableIncome.Basis);
        Assert.Equal(expected.Pit.TotalTax.Value, snapshot.Result.TotalTax.Value);
        Assert.Equal(expected.Pit.TotalTax.Basis, snapshot.Result.TotalTax.Basis);
        Assert.Equal(expected.TaxableGrossIncome, snapshot.Result.TaxableGrossIncome);
        Assert.Equal(0m, snapshot.Result.ExemptIncome); Assert.Equal(1, snapshot.Result.Dependents);
        Assert.Equal(expected.Pit.Portions.Select(p => (p.LowerBound, p.UpperBound, p.Rate, p.TaxableAmount, p.Tax, p.Basis)),
            snapshot.Result.Portions.Select(p => (p.LowerBound, p.UpperBound, p.Rate, p.TaxableAmount, p.Tax, p.Basis)));
        var assembly = typeof(MonthlySalaryCalculator).Assembly;
        Assert.Equal(typeof(MonthlySalaryCalculator).FullName, snapshot.Engine.Name);
        Assert.Equal(assembly.ManifestModule.ModuleVersionId, snapshot.Engine.ModuleVersionId);
        Assert.Equal(assembly.GetName().Version!.ToString(), snapshot.Engine.AssemblyVersion);
        Assert.Equal(assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion, snapshot.Engine.InformationalVersion);
        await using var fresh = db.Open();
        var historyAudit = await fresh.Set<AuditEvent>().SingleAsync(e => e.Entity == nameof(CalculationRecord));
        Assert.Equal(id, historyAudit.ProfileId); Assert.Equal("Add", historyAudit.Action);
        Assert.Null(fresh.ReadAudit(historyAudit).Before);
        Assert.True(JsonElement.DeepEquals(JsonSerializer.SerializeToElement(snapshot), fresh.ReadAudit(historyAudit).After!.Value));
        Assert.Null(await new CalculationHistoryService(fresh).ReadAsync(other, snapshot.HistoryId));
    }

    [Theory]
    [InlineData("official-on-references")]
    [InlineData("references-on-official")]
    [InlineData("missing-income")]
    [InlineData("missing-profile")]
    [InlineData("empty-catalog")]
    [InlineData("pending")]
    [InlineData("bad-mode")]
    [InlineData("bad-month")]
    [InlineData("no-actor")]
    public async Task InvalidSourcesOrPendingWorkNeverCreateHistory(string reason)
    {
        using var db = new SqliteFileDatabase();
        var id = await HistoryFixture.SeedAsync(db, reason == "references-on-official");
        await using var c = db.Open();
        if (reason == "empty-catalog")
        {
            await c.Database.ExecuteSqlRawAsync("DELETE FROM TaxRulePeriod");
            await c.Database.ExecuteSqlRawAsync("DELETE FROM TaxRuleSnapshot");
        }
        if (reason == "pending") (await c.MonthlyIncomes.SingleAsync()).GrossIncome = 4;
        if (reason == "no-actor") c.AuditContext = null;
        var before = await c.Set<AuditEvent>().CountAsync();
        await Assert.ThrowsAnyAsync<Exception>(() => new CalculationHistoryService(c).CreateMonthlyAsync(
            reason == "missing-profile" ? Guid.NewGuid() : id, 2026,
            reason == "missing-income" ? 2 : reason == "bad-month" ? 13 : 1,
            reason == "bad-mode" ? (CalculationSourceMode)99 : reason == "official-on-references" ?
                CalculationSourceMode.Official : CalculationSourceMode.References));
        Assert.Null(c.Database.CurrentTransaction);
        await using var fresh = db.Open();
        Assert.Empty(await fresh.CalculationHistory.ToListAsync());
        Assert.Equal(before, await fresh.Set<AuditEvent>().CountAsync());
        Assert.Equal(1234567890.123456789m, (await fresh.MonthlyIncomes.SingleAsync()).GrossIncome);
    }
}
