using Microsoft.EntityFrameworkCore.Diagnostics;

namespace TaxManager.Data.Tests.Fixtures;

internal sealed class CancelAfterSaveInterceptor(CancellationTokenSource source) : SaveChangesInterceptor
{
    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result) { source.Cancel(); return result; }
    public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result,
        CancellationToken cancellationToken = default) => ValueTask.FromResult(SavedChanges(eventData, result));
}
