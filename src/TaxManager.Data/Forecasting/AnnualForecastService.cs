using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TaxManager.Core.Forecasting;
using TaxManager.Data.Infrastructure;
using TaxManager.Data.TaxRules;

namespace TaxManager.Data.Forecasting;

public sealed record AnnualCalculationAttempt(ForecastCompleteness Completeness, AnnualCalculationSnapshot? Snapshot);

/// <summary>Computes only from a persisted revision and stored catalog in one consistent transaction.</summary>
public sealed class AnnualForecastService(TaxManagerDbContext context)
{
    public async Task<IReadOnlyList<AnnualCalculationSnapshot>> HistoryAsync(Guid documentId, CancellationToken token = default)
    {
        context.ValidateProtection();
        var records = await context.Set<AnnualCalculationRecord>().AsNoTracking().Where(x => x.DocumentId == documentId).ToListAsync(token);
        return records.Select(x => x.Snapshot).OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.HistoryId).ToArray();
    }

    public async Task<AnnualCalculationSnapshot?> ReadAsync(Guid documentId, Guid historyId, CancellationToken token = default)
    {
        context.ValidateProtection();
        return (await context.Set<AnnualCalculationRecord>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.DocumentId == documentId && x.Id == historyId, token))?.Snapshot;
    }

    public async Task<AnnualCalculationAttempt> CalculateAsync(Guid documentId, int revision, CancellationToken token = default)
    {
        if (documentId == Guid.Empty || revision < 1) throw new ArgumentException("Explicit profile and revision are required.");
        context.ChangeTracker.DetectChanges();
        if (context.ChangeTracker.HasChanges() || context.AuthorizedAnnualAppend is not null)
            throw new InvalidOperationException("Annual calculation requires no pending writes.");
        var owned = context.Database.CurrentTransaction is null ? await context.Database.BeginTransactionAsync(token) : null;
        var transaction = context.Database.CurrentTransaction!;
        var savepoint = "annual_" + Guid.NewGuid().ToString("N");
        var created = false;
        var commitStarted = false;
        AnnualCalculationRecord? record = null;
        try
        {
            SqliteWriteRequirements.Validate(context);
            if (transaction.GetDbTransaction().IsolationLevel == IsolationLevel.ReadUncommitted)
                throw new InvalidOperationException("Annual calculation requires consistent reads.");
            if (owned is null)
            {
                if (!transaction.SupportsSavepoints) throw new InvalidOperationException("Annual calculation requires savepoints.");
                await transaction.CreateSavepointAsync(savepoint, token);
                created = true;
            }
            await context.Database.ExecuteSqlRawAsync("UPDATE TaxRuleSnapshot SET RuleId = RuleId WHERE 0", token);
            context.ValidateProtection();
            var document = await context.Set<ForecastDocument>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == documentId, token);
            if (document is null || document.Revision != revision)
                throw new InvalidOperationException("Hồ sơ đã thay đổi hoặc bị xóa; hãy mở lại trước khi tính.");
            var source = await context.Set<ForecastSnapshot>().AsNoTracking()
                .SingleAsync(x => x.DocumentId == documentId && x.Revision == revision, token);
            if (source.Payload != document.Payload) throw new InvalidOperationException("Input revision snapshot does not match the saved profile.");
            var input = ForecastDraftReader.Read(document.Payload!);
            if (input.Year != 2026) throw new NotSupportedException("Only the 2026 annual forecast is supported.");
            var completeness = AnnualForecastCalculator.Assess(input.Months, input.Bonuses);
            AnnualCalculationSnapshot? snapshot = null;
            if (completeness.MissingObligation.Count == 0)
            {
                var catalog = await new AnnualForecastRuleCatalog(context).ReadAsync(input.Year, token);
                var result = AnnualForecastCalculator.Calculate(input.Year, input.Months, input.Bonuses, catalog.Provider);
                snapshot = new(1, Guid.NewGuid(), documentId, revision, source.Id, input.Year,
                    context.Clock.GetUtcNow().ToUniversalTime(), typeof(AnnualForecastCalculator).Assembly.GetName().Version!.ToString(),
                    document.Payload!, input, AnnualResultSnapshot.FromCore(result),
                    catalog.Rules.Select(r => new AnnualRuleVersion(r.PeriodFrom, r.PeriodTo, r.Graph)).ToArray());
                record = new(snapshot);
                context.AuthorizedAnnualAppend = record;
                context.Add(record);
                await context.SaveChangesAsync(token);
            }
            token.ThrowIfCancellationRequested();
            if (owned is not null) { commitStarted = true; await owned.CommitAsync(token); }
            else await transaction.ReleaseSavepointAsync(savepoint, token);
            return new(completeness, snapshot);
        }
        catch (Exception failure)
        {
            try
            {
                if (owned is not null) await owned.RollbackAsync(CancellationToken.None);
                else if (created) await transaction.RollbackToSavepointAsync(savepoint, CancellationToken.None);
            }
            catch when (commitStarted) { }
            if (commitStarted) throw new InvalidOperationException("Commit outcome may be unknown; reconcile OperationId in a fresh context.", failure);
            throw;
        }
        finally
        {
            context.AuthorizedAnnualAppend = null;
            if (record is not null) context.Entry(record).State = EntityState.Detached;
            if (owned is not null) await owned.DisposeAsync();
        }
    }
}
