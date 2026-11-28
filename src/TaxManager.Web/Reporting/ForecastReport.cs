using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TaxManager.Core.Forecasting;
using TaxManager.Data.Infrastructure;
using TaxManager.Data.Forecasting;

namespace TaxManager.Web.Reporting;

public sealed record ReportValue(int Month, string Label, string Unit, decimal? Amount,
    ForecastStatus? Status, string? Source, string? Note, DateTimeOffset? EnteredAt, bool? IncludedInGross = null);

public sealed record ForecastReport(Guid Id, int Revision, int CurrentRevision, ForecastAnnualInput Input,
    ForecastCompleteness Completeness, IReadOnlyList<ReportValue> Values, AnnualCalculationSnapshot? Calculation)
{
    public IEnumerable<ReportValue> MonthlyValues => Values.Where(v => v.IncludedInGross is null);
    public IEnumerable<ReportValue> Bonuses => Values.Where(v => v.IncludedInGross is not null);
}

public static class ForecastReportReader
{
    public static async Task<ForecastReport?> ReadAsync(TaxManagerDbContext context, Guid id,
        int? revision, Guid? record, CancellationToken token)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(token);
        var store = new ForecastStore(context);
        var document = await store.ReadAsync(id, token);
        if (document is null) return null;
        var selected = revision ?? document.Revision;
        var version = (await store.HistoryAsync(id, token)).SingleOrDefault(v => v.Revision == selected);
        if (version is null) return null;
        if (version.SchemaVersion != 1 || selected == document.Revision && version.Payload != document.Payload)
            throw new InvalidOperationException("Saved revision does not match the profile.");
        var input = ForecastDraftReader.Read(version.Payload!);
        if (input.Year != 2026) throw new NotSupportedException("Only the 2026 report is supported.");
        var completeness = AnnualForecastCalculator.Assess(input.Months, input.Bonuses);
        AnnualCalculationSnapshot? calculation = null;
        if (record != Guid.Empty)
        {
            var history = await new AnnualForecastService(context).HistoryAsync(id, token);
            calculation = history.FirstOrDefault(h => h.InputRevision == selected && (record is null || h.HistoryId == record));
            if (record is not null && calculation is null) return null;
            if (calculation is not null && calculation.DraftPayload != version.Payload)
                throw new InvalidOperationException("Annual snapshot does not match the selected input.");
        }
        var values = new List<ReportValue>();
        foreach (var month in input.Months.OrderBy(m => m.Month))
        {
            values.Add(Value(month.Month, "Lương gross", "đ", month.Gross));
            values.Add(Value(month.Month, "Bảo hiểm", "đ", month.Insurance));
            values.Add(Value(month.Month, "Người phụ thuộc", "người", month.Dependents));
            values.Add(Value(month.Month, "Khấu trừ", "đ", month.Withheld));
        }
        values.AddRange(input.Bonuses.Select(b => Value(b.Month, "Thưởng", "đ", b.Amount) with { IncludedInGross = b.IncludedInGross }));
        await transaction.CommitAsync(token);
        return new(id, selected, document.Revision, input, completeness, values, calculation);
    }

    private static ReportValue Value<T>(int month, string label, string unit, ForecastValue<T>? value) =>
        new(month, label, unit, value is null ? null : Convert.ToDecimal(value.Value, CultureInfo.InvariantCulture),
            value?.Status, value?.Source, value?.Note, value?.EnteredAt);
}
