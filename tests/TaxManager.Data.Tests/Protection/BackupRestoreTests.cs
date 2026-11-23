using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TaxManager.Data.Audit;
using TaxManager.Data.Infrastructure;
using TaxManager.Data.Protection;
using TaxManager.Data.Tests.Fixtures;
using Xunit;

namespace TaxManager.Data.Tests.Protection;

public class BackupRestoreTests
{
    [Fact]
    public async Task ConsistentSqliteBackupAndReopenedExternalKeysRestoreTogether()
    {
        using var db = new SqliteFileDatabase(); using var wrong = new SqliteFileDatabase();
        var id = await SyntheticGraph.SeedAsync(db);
        var backupPath = db.DatabasePath + ".backup";
        using (var source = new SqliteConnection($"Data Source={db.DatabasePath};Pooling=False"))
        using (var backup = new SqliteConnection($"Data Source={backupPath};Pooling=False"))
        {
            source.Open(); backup.Open(); source.BackupDatabase(backup);
        }
        using var restoredKeys = FileKeyRing.Load(db.KeyPath, Path.GetFullPath("."), Path.GetDirectoryName(backupPath)!);
        var options = new DbContextOptionsBuilder<TaxManagerDbContext>()
            .UseSqlite($"Data Source={backupPath};Foreign Keys=True;Pooling=False").Options;
        await using (var restored = new TaxManagerDbContext(options, restoredKeys))
        {
            restored.ValidateProtection();
            Assert.Equal(id, (await restored.UserProfiles.AsNoTracking().SingleAsync()).Id);
            Assert.Equal(1234567890.123456789m, (await restored.MonthlyIncomes.AsNoTracking().SingleAsync()).GrossIncome);
            Assert.All(await restored.Set<AuditEvent>().AsNoTracking().ToListAsync(), e => Assert.NotNull(restored.ReadAudit(e).After));
        }
        await using var badRestore = new TaxManagerDbContext(options, wrong.Keys);
        Assert.Throws<DataProtectionException>(badRestore.ValidateProtection);
    }
}
