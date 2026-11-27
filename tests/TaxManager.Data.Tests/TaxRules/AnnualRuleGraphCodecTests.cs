using TaxManager.Core;
using TaxManager.Core.Forecasting;
using TaxManager.Data.TaxRules;
using TaxManager.Data.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace TaxManager.Data.Tests.TaxRules;

public sealed class AnnualRuleGraphCodecTests
{
    [Fact]
    public async Task Reference_loader_rejects_an_unexpected_annual_graph()
    {
        using var database = new SqliteFileDatabase();
        await using (var context = database.Open())
        {
            await context.Database.MigrateAsync();
            await new TaxRuleStore(context).SeedReferencesAsync();
            var entity = await context.Set<TaxRuleSnapshot>().SingleAsync(x => x.RuleId == TaxRuleSet.Vietnam2026.Id);
            var monthly = TaxRuleSet.Vietnam2026;
            var annual = TaxRuleSet.CreateVietnam2026CashBonusProvider(InsuranceRegion.I).GetForMonth(2026, 1);
            var injected = new TaxRuleSet(monthly.Id, monthly.Version, monthly.Period, monthly.FromYear,
                monthly.PersonalDeduction, monthly.DependentDeduction, monthly.Brackets,
                monthly.PersonalDeductionBasis, monthly.DependentDeductionBasis, monthly.TaxableIncomeBasis,
                monthly.TaxBasis, roundingPolicy: annual.RoundingPolicy,
                annualSalaryRules: annual.AnnualSalaryRules, cashBonusRules: annual.CashBonusRules);
            entity.AnnualGraph = AnnualRuleGraphCodec.Encode(injected);
            await context.SaveChangesAsync();
        }
        await using var fresh = database.Open();
        await Assert.ThrowsAsync<InvalidOperationException>(() => new TaxRuleStore(fresh).LoadReferencesAsync());
    }

    [Fact]
    public void Mapper_preserves_supported_annual_graph_and_legal_status()
    {
        var source = TaxRuleSet.CreateVietnam2026CashBonusProvider(InsuranceRegion.I).GetForMonth(2026, 1);
        var manual = new TaxRuleSet(source.Id, source.Version, source.Period, source.FromYear,
            source.PersonalDeduction, source.DependentDeduction, source.Brackets,
            source.PersonalDeductionBasis, source.DependentDeductionBasis, source.TaxableIncomeBasis,
            source.TaxBasis, roundingPolicy: source.RoundingPolicy,
            annualSalaryRules: source.AnnualSalaryRules, cashBonusRules: source.CashBonusRules);

        var restored = TaxRuleMapper.ToCore(TaxRuleMapper.ToEntity(manual));

        Assert.Equal(manual.AnnualSalaryRules!.TaxRules.Brackets, restored.AnnualSalaryRules!.TaxRules.Brackets);
        Assert.Equal(manual.CashBonusRules!.TaxableBasis, restored.CashBonusRules!.TaxableBasis);
        Assert.Equal(VerificationStatus.Unverified, restored.CashBonusRules.TaxableBasis.Status);
        Assert.Throws<ArgumentException>(() => TaxRuleValidator.ValidateOfficial(restored));
    }

    [Fact]
    public void Annual_manual_insurance_graph_round_trips_with_legal_bases_and_computes_bonus()
    {
        var source = TaxRuleSet.CreateVietnam2026CashBonusProvider(InsuranceRegion.I).GetForMonth(2026, 1);
        var manual = new TaxRuleSet(source.Id, source.Version, source.Period, source.FromYear,
            source.PersonalDeduction, source.DependentDeduction, source.Brackets,
            source.PersonalDeductionBasis, source.DependentDeductionBasis, source.TaxableIncomeBasis,
            source.TaxBasis, roundingPolicy: source.RoundingPolicy,
            annualSalaryRules: source.AnnualSalaryRules, cashBonusRules: source.CashBonusRules);

        var reloaded = AnnualRuleGraphCodec.Decode(AnnualRuleGraphCodec.Encode(manual));
        Assert.Equal(manual.AnnualSalaryRules!.TaxRules.Brackets, reloaded.AnnualSalaryRules!.TaxRules.Brackets);
        Assert.Equal(manual.CashBonusRules!.TaxableBasis, reloaded.CashBonusRules!.TaxableBasis);
        Assert.Equal(VerificationStatus.Unverified, reloaded.CashBonusRules.TaxableBasis.Status);
        Assert.Null(reloaded.InsuranceRules);

        var provider = new TaxRuleProvider([new(reloaded, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31))]);
        var when = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        ForecastValue<decimal> Actual(decimal value) => new(value, ForecastStatus.Actual, "Phiếu", null, when);
        var dependents = new ForecastValue<int>(0, ForecastStatus.Actual, "Tự khai", null, when);
        var months = Enumerable.Range(1, 12).Select(m => new ForecastMonth(m, Actual(30_000_000m),
            Actual(0m), dependents, null));
        var result = AnnualForecastCalculator.Calculate(2026, months,
            [new ForecastBonus("bonus", 12, Actual(10_000_000m), false)], provider);
        Assert.Equal(12_400_000m, result.Obligation);
        Assert.Null(result.Difference);
    }
}
