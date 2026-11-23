using System.Data.Common;
using System.Text;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace TaxManager.Data.Tests.CalculationHistory;

internal sealed class HistoryCommandRecorder : DbCommandInterceptor
{
    internal List<string> Commands { get; } = [];
    private void Record(DbCommand command)
    {
        Commands.Add(command.CommandText);
        foreach (DbParameter p in command.Parameters)
            Commands.Add(p.Value is byte[] bytes ? Encoding.UTF8.GetString(bytes) : Convert.ToString(p.Value,
                System.Globalization.CultureInfo.InvariantCulture) ?? "");
    }
    public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData,
        InterceptionResult<DbDataReader> result) { Record(command); return result; }
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
        InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    { Record(command); return ValueTask.FromResult(result); }
    public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData,
        InterceptionResult<int> result) { Record(command); return result; }
    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    { Record(command); return ValueTask.FromResult(result); }
}
