using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TaxManager.Core;
using TaxManager.Core.Forecasting;
using TaxManager.Data.Infrastructure;
using TaxManager.Data.TaxRules;

namespace TaxManager.Data.Forecasting;

public sealed class ForecastRevisionConflictException() : InvalidOperationException("The saved forecast revision has changed.");

/// <summary>A read-only view, calculated from one saved revision. This is not a history record.</summary>
public sealed record ForecastMonthView(Guid DocumentId, int Revision, int Year, ForecastMonth Input,
    IReadOnlyList<ForecastBonus> Bonuses, IReadOnlyList<ForecastInputKind> Missing,
    TaxRuleRegistration? Registration, MonthlySalaryResult? Result)
{
    public ForecastStatus? Status => Result is null ? null :
        new[] { Input.Gross!.Status, Input.Insurance!.Status, Input.Dependents!.Status }
            .Concat(Bonuses.Select(b => b.Amount.Status)).Max();
    public decimal? Difference => Result is not null && Input.Withheld is not null
        ? Result.Pit.TotalTax.Value - Input.Withheld.Value : null;
}

/// <summary>Reads persisted input and catalog together; never creates demo profiles or writes results.</summary>
public sealed class ForecastMonthService(TaxManagerDbContext context)
{
    public async Task<ForecastMonthView?> ReadAsync(Guid documentId, int revision, int month, CancellationToken token = default)
    {
        if (documentId == Guid.Empty || revision < 1 || month is < 1 or > 12)
            throw new ArgumentException("An explicit document, revision and month are required.");
        context.ChangeTracker.DetectChanges();
        if (context.ChangeTracker.HasChanges()) throw new InvalidOperationException("Reading a saved month requires no pending writes.");
        await using var owned = context.Database.CurrentTransaction is null
            ? await context.Database.BeginTransactionAsync(token) : null;
        if (context.Database.CurrentTransaction!.GetDbTransaction().IsolationLevel == IsolationLevel.ReadUncommitted)
            throw new InvalidOperationException("Reading a saved month requires consistent transaction reads.");
        context.ValidateProtection(); // Also required for an empty ID lookup.
        var document = await context.Set<ForecastDocument>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == documentId, token);
        if (document is null) return null;
        if (document.Revision != revision) throw new ForecastRevisionConflictException();
        var source = await context.Set<ForecastSnapshot>().AsNoTracking()
            .SingleAsync(x => x.DocumentId == documentId && x.Revision == revision, token);
        if (source.Payload != document.Payload) throw new InvalidOperationException("Saved input snapshot differs from the document.");
        var input = ForecastDraftReader.Read(document.Payload!);
        if (input.Year != 2026) throw new NotSupportedException("Only the 2026 forecast is supported.");
        var completeness = AnnualForecastCalculator.Assess(input.Months, input.Bonuses);
        var selected = input.Months.Single(m => m.Month == month);
        var bonuses = input.Bonuses.Where(b => b.Month == month).ToArray();
        if (selected.Gross?.Value < 0 || selected.Insurance?.Value < 0 || selected.Dependents?.Value < 0 || selected.Withheld?.Value < 0)
            throw new ArgumentException("Saved monthly values cannot be negative.");
        var missing = completeness.MissingObligation.Where(m => m.Month == month).Select(m => m.Kind).ToArray();
        MonthlySalaryResult? result = null;
        TaxRuleRegistration? registration = null;
        if (missing.Length == 0)
        {
            var catalog = await new AnnualForecastRuleCatalog(context).ReadAsync(input.Year, token);
            var date = new DateOnly(input.Year, month, 1);
            var row = catalog.Rules.Single(r => r.PeriodFrom <= date && date <= r.PeriodTo);
            registration = new(catalog.Provider.GetForMonth(input.Year, month), row.PeriodFrom, row.PeriodTo);
            // Remove included bonuses from the base, then pass ALL bonuses through Core once.
            // This both validates bonus treatment/timing and avoids counting them twice.
            var baseGross = selected.Gross!.Value - bonuses.Where(b => b.IncludedInGross).Sum(b => b.Amount.Value);
            var calculationInput = new SalaryMonthInput(input.Year, month, baseGross, 0m, selected.Insurance!.Value,
                bonuses: bonuses.Select(b => new CashBonus(b.Id, b.Amount.Value, date, CashBonusTreatment.TaxableCashFromCurrentEmployer)));
            // Core's required withholding placeholder is never returned as a saved input.
            result = BonusMonthlySalaryCalculator.Calculate(calculationInput, selected.Dependents!.Value, catalog.Provider).Salary;
        }
        var view = new ForecastMonthView(document.Id, revision, input.Year, selected,
            Array.AsReadOnly(bonuses), Array.AsReadOnly(missing), registration, result);
        token.ThrowIfCancellationRequested();
        if (owned is not null) await owned.CommitAsync(token);
        return view;
    }
}
