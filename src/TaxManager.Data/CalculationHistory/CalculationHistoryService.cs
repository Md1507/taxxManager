using System.Collections.Immutable;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TaxManager.Core;
using TaxManager.Data.Infrastructure;
using TaxManager.Data.TaxRules;

namespace TaxManager.Data.CalculationHistory;

/// <summary>IDs and audit actor are caller metadata, not authentication or authorization.</summary>
public sealed class CalculationHistoryService(TaxManagerDbContext context)
{
    public async Task<CalculationSnapshot?> ReadAsync(Guid profileId, Guid historyId, CancellationToken token = default)
    {
        if (profileId == Guid.Empty || historyId == Guid.Empty) throw new ArgumentException("Explicit profile and history IDs are required.");
        context.ValidateProtection(); // Even an empty lookup must fail with missing/wrong keys.
        var record = await context.CalculationHistory.AsNoTracking()
            .SingleOrDefaultAsync(h => h.ProfileId == profileId && h.Id == historyId, token);
        return record?.Snapshot;
    }

    public async Task<CalculationSnapshot> CreateMonthlyAsync(Guid profileId, int year, int month,
        CalculationSourceMode sourceMode, CancellationToken token = default)
    {
        if (profileId == Guid.Empty || year is < 1 or > 9999 || month is < 1 or > 12 || !Enum.IsDefined(sourceMode))
            throw new ArgumentException("Explicit profile, valid month and source mode are required.");
        context.ChangeTracker.DetectChanges();
        if (context.ChangeTracker.HasChanges() || context.AuthorizedHistoryAppend is not null)
            throw new InvalidOperationException("Calculation requires no pending writes and cannot be reentered.");
        token.ThrowIfCancellationRequested();
        var owned = context.Database.CurrentTransaction is null ? await context.Database.BeginTransactionAsync(token) : null;
        var transaction = context.Database.CurrentTransaction!;
        var savepoint = "calculation_" + Guid.NewGuid().ToString("N");
        var created = false;
        var commitStarted = false;
        CalculationRecord? record = null;
        try
        {
            SqliteWriteRequirements.Validate(context);
            if (transaction.GetDbTransaction().IsolationLevel == IsolationLevel.ReadUncommitted)
                throw new InvalidOperationException("Calculation requires consistent transaction reads.");
            if (owned is null)
            {
                if (!transaction.SupportsSavepoints) throw new InvalidOperationException("Calculation requires savepoints.");
                await transaction.CreateSavepointAsync(savepoint, token);
                created = true;
            }
            // Acquire SQLite's writer lock before the first source read, including deferred caller transactions.
            await context.Database.ExecuteSqlRawAsync("UPDATE TaxRuleSnapshot SET RuleId = RuleId WHERE 0", token);
            context.ValidateProtection();
            if (!await context.UserProfiles.AsNoTracking().AnyAsync(p => p.Id == profileId, token))
                throw new InvalidOperationException("Calculation profile does not exist.");
            var income = await context.MonthlyIncomes.AsNoTracking()
                .SingleOrDefaultAsync(i => i.ProfileId == profileId && i.Year == year && i.Month == month, token)
                ?? throw new InvalidOperationException("Calculation month has no persisted income.");
            var date = new DateOnly(year, month, 1);
            var dependents = await context.DependentEligibilityPeriods.AsNoTracking()
                .Where(p => p.ProfileId == profileId && p.EligibleFromMonth <= date && p.EligibleToMonth >= date)
                .OrderBy(p => p.DependentId).ThenBy(p => p.EligibleFromMonth).ToListAsync(token);
            var registrations = await new TaxRuleProviderLoader(context).ReadRegistrationsAsync(token);
            TaxRuleProviderLoader.ValidateSourcePolicy(registrations, sourceMode == CalculationSourceMode.Official);
            var registration = registrations.SingleOrDefault(r => r.PeriodFrom <= date && date <= r.PeriodTo)
                ?? throw new NotSupportedException("No tax rule is registered for the calculation month.");
            var input = new CalculationInputSnapshot(income.GrossIncome, income.InsuranceAmount,
                income.ActualWithheldTax, 0m, dependents.Select(d => d.DependentId).Distinct().Count(),
                dependents.Select(d => new CalculationDependentSnapshot(d.DependentId, d.EligibleFromMonth,
                    d.EligibleToMonth)).ToImmutableArray());
            var result = MonthlySalaryCalculator.Calculate(input.GrossIncome, input.ManualInsurance,
                input.DependentCount, registration.RuleSet, input.ExemptIncome);
            var snapshot = new CalculationSnapshot(1, Guid.NewGuid(), profileId, year, month,
                context.Clock.GetUtcNow().ToUniversalTime(), sourceMode, CalculationEngineSnapshot.Current(),
                input, CalculationRuleSnapshot.FromCore(registration), CalculationResultSnapshot.FromCore(result));
            record = new CalculationRecord(snapshot);
            context.AuthorizedHistoryAppend = record;
            context.CalculationHistory.Add(record);
            await context.SaveChangesAsync(token);
            token.ThrowIfCancellationRequested();
            if (owned is not null)
            {
                commitStarted = true;
                await owned.CommitAsync(token);
            }
            else await transaction.ReleaseSavepointAsync(savepoint, token);
            return snapshot;
        }
        catch (Exception failure)
        {
            try
            {
                if (owned is not null) await owned.RollbackAsync(CancellationToken.None);
                else if (created) await transaction.RollbackToSavepointAsync(savepoint, CancellationToken.None);
            }
            catch when (commitStarted) { /* Preserve unknown commit outcome; never retry automatically. */ }
            if (commitStarted)
                throw new InvalidOperationException("Commit outcome may be unknown; reconcile OperationId with a fresh context before retrying.", failure);
            throw;
        }
        finally
        {
            context.AuthorizedHistoryAppend = null;
            if (record is not null) context.Entry(record).State = EntityState.Detached;
            if (owned is not null) await owned.DisposeAsync();
        }
    }
}
