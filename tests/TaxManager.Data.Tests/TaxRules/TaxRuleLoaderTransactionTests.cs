using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TaxManager.Data.TaxRules;
using TaxManager.Data.Tests.Fixtures;
using Xunit;

namespace TaxManager.Data.Tests.TaxRules;

public class TaxRuleLoaderTransactionTests
{
    [Fact]
    public async Task AllReadsUseOneTransactionAndItIsDisposedOnSuccessAndFailure()
    {
        using var database = new SqliteFileDatabase();
        await using (var writer = database.Open())
        {
            await writer.Database.MigrateAsync();
            await new TaxRuleStore(writer).SeedReferencesAsync();
        }

        var interceptor = new ReadTransactionInterceptor();
        await using var reader = database.Open(interceptor);
        var store = new TaxRuleStore(reader);
        await store.LoadReferencesAsync();
        Assert.True(interceptor.Transactions.Count >= 5);
        Assert.All(interceptor.Transactions, transaction => Assert.Same(interceptor.Transactions[0], transaction));
        Assert.Null(reader.Database.CurrentTransaction);

        interceptor.Transactions.Clear();
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.LoadOfficialAsync());
        Assert.True(interceptor.Transactions.Count >= 5);
        Assert.All(interceptor.Transactions, transaction => Assert.Same(interceptor.Transactions[0], transaction));
        Assert.Null(reader.Database.CurrentTransaction);

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.LoadReferencesAsync(cancellation.Token));
        Assert.Null(reader.Database.CurrentTransaction);
    }

    private sealed class ReadTransactionInterceptor : DbCommandInterceptor
    {
        public List<DbTransaction> Transactions { get; } = [];

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Assert.NotNull(command.Transaction);
            Transactions.Add(command.Transaction);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
