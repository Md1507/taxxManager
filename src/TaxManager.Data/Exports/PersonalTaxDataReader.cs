using Microsoft.EntityFrameworkCore;
using TaxManager.Data.Audit;
using TaxManager.Data.CalculationHistory;
using TaxManager.Data.Forecasting;
using TaxManager.Data.Infrastructure;

namespace TaxManager.Data.Exports;

public sealed record ExportProfile(Guid Id, string? DisplayName);
public sealed record ExportIncome(Guid ProfileId, int Year, int Month, decimal GrossIncome, decimal InsuranceAmount, decimal ActualWithheldTax);
public sealed record ExportDependent(Guid ProfileId, Guid Id, string? DisplayName);
public sealed record ExportEligibility(Guid ProfileId, Guid DependentId, DateOnly EligibleFromMonth, DateOnly EligibleToMonth);
public sealed record ExportForecast(Guid Id, int Revision, string? Payload);
public sealed record ExportForecastVersion(Guid Id, Guid DocumentId, int Revision, int SchemaVersion, string? Payload);
public sealed record ExportAudit(Guid Id, Guid OperationId, int Ordinal, Guid ProfileId, string Entity,
    string EntityKey, string Action, DateTimeOffset TimestampUtc, AuditPayload Content);
public sealed record PersonalTaxData(ExportProfile[] Profiles, ExportIncome[] MonthlyIncome,
    ExportDependent[] Dependents, ExportEligibility[] EligibilityPeriods, ExportForecast[] Forecasts,
    ExportForecastVersion[] ForecastVersions, CalculationSnapshot[] MonthlyResults,
    AnnualCalculationSnapshot[] AnnualResults, ExportAudit[] Audit);

/// <summary>Read-only allowlist; includes retained history even after ordinary profile deletion.</summary>
public sealed class PersonalTaxDataReader(TaxManagerDbContext context)
{
    public const int MaxRowsPerGroup = 10_000;

    public async Task<PersonalTaxData> ReadAsync(CancellationToken token = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(token);
        // Existing stores must authenticate even when empty; untouched stores must stay untouched.
        var forecasts = await new ForecastStore(context).ListAsync(token);
        CheckCount(forecasts.Count);
        var result = new PersonalTaxData(
            (await Rows(context.UserProfiles, token)).Select(p => new ExportProfile(p.Id, p.DisplayName)).ToArray(),
            (await Rows(context.MonthlyIncomes, token)).Select(i => new ExportIncome(i.ProfileId, i.Year, i.Month,
                i.GrossIncome, i.InsuranceAmount, i.ActualWithheldTax)).ToArray(),
            (await Rows(context.Dependents, token)).Select(d => new ExportDependent(d.ProfileId, d.Id, d.DisplayName)).ToArray(),
            (await Rows(context.DependentEligibilityPeriods, token)).Select(p => new ExportEligibility(p.ProfileId,
                p.DependentId, p.EligibleFromMonth, p.EligibleToMonth)).ToArray(),
            forecasts.Select(f => new ExportForecast(f.Id, f.Revision, f.Payload)).ToArray(),
            (await Rows(context.Set<ForecastSnapshot>(), token)).Select(s => new ExportForecastVersion(s.Id,
                s.DocumentId, s.Revision, s.SchemaVersion, s.Payload)).ToArray(),
            (await Rows(context.CalculationHistory, token)).Select(r => r.Snapshot).ToArray(),
            (await Rows(context.Set<AnnualCalculationRecord>(), token)).Select(r => r.Snapshot).ToArray(),
            (await Rows(context.Set<AuditEvent>(), token)).Select(a => new ExportAudit(a.Id, a.OperationId, a.Ordinal,
                a.ProfileId, a.Entity, a.EntityKey, a.Action, a.TimestampUtc, context.ReadAudit(a))).ToArray());
        await transaction.CommitAsync(token);
        return result;
    }

    private static async Task<T[]> Rows<T>(IQueryable<T> query, CancellationToken token) where T : class
    {
        var rows = await query.AsNoTracking().Take(MaxRowsPerGroup + 1).ToArrayAsync(token);
        CheckCount(rows.Length);
        return rows;
    }
    private static void CheckCount(int count)
    {
        if (count > MaxRowsPerGroup) throw new InvalidDataException("Personal export exceeds the row limit.");
    }
}
