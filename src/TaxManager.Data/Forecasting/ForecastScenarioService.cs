using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TaxManager.Core.Forecasting;
using TaxManager.Data.Infrastructure;
using TaxManager.Data.TaxRules;

namespace TaxManager.Data.Forecasting;

public sealed record ForecastScenarioView(Guid DocumentId, int Revision, ForecastAnnualInput Input,
    ForecastCompleteness Completeness, ForecastValue<decimal>? AdditionalBonus, int? BonusMonth,
    AnnualForecastScenarios? Results, AnnualRuleVersion[] Rules, DateTimeOffset CalculatedAt);

/// <summary>Ephemeral comparison of one saved revision; does not append history or alter input.</summary>
public sealed class ForecastScenarioService(TaxManagerDbContext context)
{
    public async Task<ForecastScenarioView?> CompareAsync(Guid documentId, int? revision,
        decimal? bonusAmount = null, int? bonusMonth = null, CancellationToken token = default)
    {
        if (documentId == Guid.Empty || revision is < 1 || bonusAmount is < 0 || bonusMonth is < 1 or > 12 ||
            (bonusAmount is null) != (bonusMonth is null)) throw new ArgumentException("Invalid scenario input.");
        context.ChangeTracker.DetectChanges();
        if (context.ChangeTracker.HasChanges()) throw new InvalidOperationException("Comparison requires no pending writes.");
        await using var owned = context.Database.CurrentTransaction is null
            ? await context.Database.BeginTransactionAsync(token) : null;
        if (context.Database.CurrentTransaction!.GetDbTransaction().IsolationLevel == IsolationLevel.ReadUncommitted)
            throw new InvalidOperationException("Comparison requires consistent reads.");
        context.ValidateProtection();
        var document = await context.Set<ForecastDocument>().AsNoTracking().SingleOrDefaultAsync(d => d.Id == documentId, token);
        if (document is null) return null;
        if (revision is not null && revision != document.Revision) throw new ForecastRevisionConflictException();
        var snapshot = await context.Set<ForecastSnapshot>().AsNoTracking()
            .SingleAsync(s => s.DocumentId == documentId && s.Revision == document.Revision, token);
        if (snapshot.SchemaVersion != 1 || snapshot.Payload != document.Payload)
            throw new InvalidOperationException("The saved snapshot does not match the document.");
        var input = ForecastDraftReader.Read(snapshot.Payload!);
        if (input.Year != 2026) throw new NotSupportedException("Only the 2026 forecast is supported.");
        var completeness = AnnualForecastCalculator.Assess(input.Months, input.Bonuses);
        if (input.Months.Any(m => m.Gross?.Value < 0 || m.Insurance?.Value < 0 || m.Dependents?.Value < 0 || m.Withheld?.Value < 0))
            throw new ArgumentException("Saved inputs must be nonnegative.");
        var now = context.Clock.GetUtcNow().ToUniversalTime();
        ForecastValue<decimal>? additional = bonusAmount is { } amount
            ? new(amount, ForecastStatus.Assumed, "Người dùng thử kịch bản",
                $"Thử thêm một khoản thưởng ngoài gross vào tháng {bonusMonth}.", now) : null;
        AnnualForecastScenarios? results = null;
        AnnualRuleVersion[] rules = [];
        if (completeness.MissingObligation.Count == 0)
        {
            var catalog = await new AnnualForecastRuleCatalog(context).ReadAsync(input.Year, token);
            results = AnnualForecastCalculator.CalculateScenarios(input.Year, input.Months, input.Bonuses,
                additional, bonusMonth, catalog.Provider);
            // No withholding assumption was supplied for the additional bonus.
            if (additional is not null) results = results with { WithBonus = results.WithBonus with { Difference = null } };
            rules = catalog.Rules.Select(r => new AnnualRuleVersion(r.PeriodFrom, r.PeriodTo, r.Graph)).ToArray();
        }
        if (owned is not null) await owned.CommitAsync(token);
        return new(document.Id, document.Revision, input, completeness, additional, bonusMonth, results, rules, now);
    }
}
