using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace TaxManager.Data.Infrastructure;

internal static class SqliteWriteRequirements
{
    internal static void Validate(TaxManagerDbContext context)
    {
        using var command = context.Database.GetDbConnection().CreateCommand();
        command.Transaction = context.Database.CurrentTransaction!.GetDbTransaction();
        command.CommandText = "PRAGMA foreign_keys";
        if (Convert.ToInt64(command.ExecuteScalar()) != 1)
            throw new InvalidOperationException("SQLite foreign keys must be enabled for supported writes and purge.");
    }
}
