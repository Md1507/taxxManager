using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace TaxManager.Data.Tests.CalculationHistory;

internal sealed class CancelHistoryPurgeInterceptor(CancellationTokenSource source) : DbCommandInterceptor
{
    public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData,
        int result, CancellationToken cancellationToken = default)
    {
        if (command.CommandText.StartsWith("DELETE FROM CalculationHistory", StringComparison.Ordinal)) source.Cancel();
        return ValueTask.FromResult(result);
    }
}
