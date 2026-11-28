using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TaxManager.Core.Forecasting;
using TaxManager.Data.Infrastructure;

namespace TaxManager.Data.Forecasting;

public enum ReconciliationMetric { Gross, Insurance, Withheld }
public sealed record ReconciliationMonth(int Month, ForecastValue<decimal>? Value);
public sealed record ReconciliationTotal(bool SameYear, IReadOnlyList<ReconciliationMonth> Months, decimal? Amount);

/// <summary>Input reconciliation only: no tax calculation, catalog lookup or writes.</summary>
public sealed record ReconciliationProfile(Guid DocumentId, int Revision, ForecastAnnualInput Input)
{
    public ReconciliationTotal Summarize(ReconciliationMetric metric, int year, int fromMonth, int toMonth)
    {
        if (!Enum.IsDefined(metric) || year is < 1 or > 9999 || fromMonth is < 1 or > 12 || toMonth < fromMonth || toMonth > 12)
            throw new ArgumentException("A defined metric and an ordered period are required.");
        if (year != Input.Year) return new(false, [], null);
        var months = Input.Months.Where(m => m.Month >= fromMonth && m.Month <= toMonth).OrderBy(m => m.Month)
            .Select(m => new ReconciliationMonth(m.Month, metric switch {
                ReconciliationMetric.Gross => m.Gross,
                ReconciliationMetric.Insurance => m.Insurance,
                _ => m.Withheld
            })).ToArray();
        // A partial sum is never the total for the requested period. Zero remains a supplied value.
        decimal? total = months.Any(m => m.Value is null) ? null : months.Sum(m => m.Value!.Value);
        return new(true, Array.AsReadOnly(months), total);
    }
}

public sealed class ForecastReconciliationService(TaxManagerDbContext context)
{
    public async Task<ReconciliationProfile?> ReadAsync(Guid documentId, int? revision, CancellationToken token = default)
    {
        if (documentId == Guid.Empty || revision is < 1) throw new ArgumentException("Invalid document or revision.");
        context.ChangeTracker.DetectChanges();
        if (context.ChangeTracker.HasChanges()) throw new InvalidOperationException("Reconciliation requires no pending writes.");
        await using var owned = context.Database.CurrentTransaction is null
            ? await context.Database.BeginTransactionAsync(token) : null;
        if (context.Database.CurrentTransaction!.GetDbTransaction().IsolationLevel == IsolationLevel.ReadUncommitted)
            throw new InvalidOperationException("Reconciliation requires consistent reads.");
        context.ValidateProtection();
        var document = await context.Set<ForecastDocument>().AsNoTracking().SingleOrDefaultAsync(d => d.Id == documentId, token);
        if (document is null) return null;
        if (revision is not null && document.Revision != revision) throw new ForecastRevisionConflictException();
        var snapshot = await context.Set<ForecastSnapshot>().AsNoTracking()
            .SingleAsync(s => s.DocumentId == documentId && s.Revision == document.Revision, token);
        if (snapshot.SchemaVersion != 1 || snapshot.Payload != document.Payload)
            throw new InvalidOperationException("Snapshot and document do not match.");
        var input = ForecastDraftReader.Read(snapshot.Payload!);
        if (input.Year != 2026) throw new NotSupportedException("Only the 2026 saved forecast is supported.");
        _ = AnnualForecastCalculator.Assess(input.Months, input.Bonuses);
        if (input.Months.Any(m => m.Gross?.Value < 0 || m.Insurance?.Value < 0 || m.Dependents?.Value < 0 || m.Withheld?.Value < 0))
            throw new ArgumentException("Saved input must be nonnegative.");
        if (owned is not null) await owned.CommitAsync(token);
        return new(document.Id, document.Revision, input);
    }
}
