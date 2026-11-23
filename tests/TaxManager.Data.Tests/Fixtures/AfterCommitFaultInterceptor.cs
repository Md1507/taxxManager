using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace TaxManager.Data.Tests.Fixtures;

internal sealed class AfterCommitFaultInterceptor : DbTransactionInterceptor
{
    public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Synthetic exception after actual commit.");
}
