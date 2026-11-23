using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace TaxManager.Data.Tests.Fixtures;

internal sealed class SaveActionInterceptor(Action<DbContext> action) : SaveChangesInterceptor
{
    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        action(eventData.Context!);
        return result;
    }
    public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result,
        CancellationToken cancellationToken = default) => ValueTask.FromResult(SavedChanges(eventData, result));
}
