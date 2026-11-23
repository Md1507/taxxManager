using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace TaxManager.Data.Tests.CalculationHistory;

internal sealed class ConsistencyProbeInterceptor(string databasePath) : DbCommandInterceptor
{
    internal bool Attempted { get; private set; }
    internal int SourceReads { get; private set; }
    private DbTransaction? transaction;

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
        CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        if (command.CommandText.StartsWith("SELECT", StringComparison.Ordinal) &&
            new[] { "MonthlyIncomes", "DependentEligibilityPeriods", "TaxRule" }.Any(command.CommandText.Contains))
        {
            Assert.NotNull(command.Transaction);
            transaction ??= command.Transaction;
            Assert.Same(transaction, command.Transaction);
            SourceReads++;
            if (!Attempted && command.CommandText.Contains("MonthlyIncomes", StringComparison.Ordinal))
            {
                Attempted = true;
                using var competing = new SqliteConnection($"Data Source={databasePath};Foreign Keys=True;Pooling=False;Default Timeout=1");
                competing.Open();
                using var write = competing.CreateCommand();
                write.CommandTimeout = 1;
                write.CommandText = "UPDATE TaxRuleSnapshot SET PersonalDeduction='999'";
                Assert.Equal(5, Assert.Throws<SqliteException>(() => write.ExecuteNonQuery()).SqliteErrorCode);
            }
        }
        return ValueTask.FromResult(result);
    }
}
