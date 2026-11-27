using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TaxManager.Data.Dependents;
using TaxManager.Data.Income;
using TaxManager.Data.Infrastructure;
using TaxManager.Data.Profiles;
using TaxManager.Data.TaxRules;

namespace TaxManager.Data.Upgrades;

internal static class LegacyDataReader
{
    internal const string Migration = "20261122161649_AddMonthlyTaxRules";
    internal static async Task<LegacyDatabaseSnapshot> ReadAsync(SqliteConnection connection, SqliteTransaction transaction, CancellationToken token)
    {
        await using var context = new TaxManagerDbContext(new DbContextOptionsBuilder<TaxManagerDbContext>().UseSqlite(connection).Options);
        await context.Database.UseTransactionAsync(transaction, token);
        var history = (await context.Database.GetAppliedMigrationsAsync(token)).ToArray();
        if (!history.SequenceEqual(new[] { "20261122100144_InitialPersistence", Migration }))
            throw new InvalidOperationException("Synthetic upgrade requires exactly the two legacy migrations.");
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "PRAGMA foreign_key_check";
        await using (var check = await command.ExecuteReaderAsync(token))
            if (await check.ReadAsync(token)) throw new InvalidOperationException("Legacy foreign keys are invalid.");
        command.CommandText = "SELECT EXISTS(SELECT 1 FROM MonthlyIncomes WHERE typeof(Year)<>'integer' OR typeof(Month)<>'integer')";
        if (Convert.ToInt64(await command.ExecuteScalarAsync(token), CultureInfo.InvariantCulture) != 0)
            throw new InvalidOperationException("Legacy income calendar storage is invalid.");
        var profiles = new List<UserProfile>();
        var incomes = new List<MonthlyIncome>();
        var dependents = new List<Dependent>();
        var periods = new List<DependentEligibilityPeriod>();
        command.CommandText = "SELECT Id, DisplayName FROM UserProfiles";
        await using (var reader = await command.ExecuteReaderAsync(token))
            while (await reader.ReadAsync(token))
                profiles.Add(new UserProfile { Id = reader.GetGuid(0), DisplayName = reader.IsDBNull(1) ? null : reader.GetString(1) });
        command.CommandText = "SELECT ProfileId, Year, Month, GrossIncome, InsuranceAmount, ActualWithheldTax FROM MonthlyIncomes";
        await using (var reader = await command.ExecuteReaderAsync(token))
            while (await reader.ReadAsync(token))
                incomes.Add(new MonthlyIncome
                {
                    ProfileId = reader.GetGuid(0), Year = reader.GetInt32(1), Month = reader.GetInt32(2),
                    GrossIncome = TaxRuleStorageValidator.ParseExactDecimal(reader.GetString(3)),
                    InsuranceAmount = TaxRuleStorageValidator.ParseExactDecimal(reader.GetString(4)),
                    ActualWithheldTax = TaxRuleStorageValidator.ParseExactDecimal(reader.GetString(5))
                });
        command.CommandText = "SELECT ProfileId, Id, DisplayName FROM Dependents";
        await using (var reader = await command.ExecuteReaderAsync(token))
            while (await reader.ReadAsync(token))
                dependents.Add(new Dependent { ProfileId = reader.GetGuid(0), Id = reader.GetGuid(1),
                    DisplayName = reader.IsDBNull(2) ? null : reader.GetString(2) });
        command.CommandText = "SELECT ProfileId, DependentId, EligibleFromMonth, EligibleToMonth FROM DependentEligibilityPeriods";
        await using (var reader = await command.ExecuteReaderAsync(token))
            while (await reader.ReadAsync(token))
                periods.Add(new DependentEligibilityPeriod
                {
                    ProfileId = reader.GetGuid(0), DependentId = reader.GetGuid(1),
                    EligibleFromMonth = DateOnly.ParseExact(reader.GetString(2), "yyyy-MM-dd", CultureInfo.InvariantCulture),
                    EligibleToMonth = DateOnly.ParseExact(reader.GetString(3), "yyyy-MM-dd", CultureInfo.InvariantCulture)
                });
        await TaxRuleStorageValidator.ValidateAsync(context, token);
        return new(profiles, incomes, dependents, periods,
            await context.Set<TaxRuleLegalBasis>().AsNoTracking().ToListAsync(token),
            await new TaxRuleStoreSupport(context).SnapshotRows().AsNoTracking().ToListAsync(token),
            await context.Set<TaxRuleBracket>().AsNoTracking().ToListAsync(token),
            await context.Set<TaxRulePeriod>().AsNoTracking().ToListAsync(token));
    }
}
