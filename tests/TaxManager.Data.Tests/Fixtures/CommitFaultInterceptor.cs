using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace TaxManager.Data.Tests.Fixtures;

internal sealed class CommitFaultInterceptor : DbTransactionInterceptor
{
    internal bool Armed { get; set; }
    public override InterceptionResult TransactionCommitting(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result)
    {
        if (Armed) throw new InvalidOperationException("Synthetic commit fault.");
        return result;
    }
    public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData,
        InterceptionResult result, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(TransactionCommitting(transaction, eventData, result));
}
