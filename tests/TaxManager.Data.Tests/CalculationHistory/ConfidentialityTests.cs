using System.Text;
using Microsoft.EntityFrameworkCore;
using TaxManager.Data.Audit;
using TaxManager.Data.CalculationHistory;
using TaxManager.Data.Tests.Fixtures;
using Xunit;

namespace TaxManager.Data.Tests.CalculationHistory;

public class ConfidentialityTests
{
    [Fact]
    public async Task HistoryAuditLogsAndDatabaseFilesKeepPersonalPayloadEncrypted()
    {
        using var db = new SqliteFileDatabase();
        var id = await HistoryFixture.SeedAsync(db, true);
        var logs = new List<string>();
        var commands = new HistoryCommandRecorder();
        await using (var c = new TaxManager.Data.Infrastructure.TaxManagerDbContext(
            new DbContextOptionsBuilder<TaxManager.Data.Infrastructure.TaxManagerDbContext>()
                .UseSqlite($"Data Source={db.DatabasePath};Foreign Keys=True;Pooling=False")
                .AddInterceptors(commands).LogTo(logs.Add).Options, db.Keys))
        {
            await c.Database.OpenConnectionAsync();
            await c.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL");
            c.AuditContext = new("HISTORY_ACTOR_SECRET_82974", "HISTORY_CONTEXT_SECRET_82974", "HISTORY_CORRELATION_SECRET_82974");
            var snapshot = await new CalculationHistoryService(c).CreateMonthlyAsync(id, 2026, 1, CalculationSourceMode.Official);
            var record = await c.CalculationHistory.SingleAsync();
            var audit = await c.Set<AuditEvent>().SingleAsync(e => e.Entity == nameof(CalculationRecord));
            var sensitive = new[] { "1234567890.123456789", "HISTORY_ACTOR_SECRET_82974", "HISTORY_CONTEXT_SECRET_82974",
                "HISTORY_CORRELATION_SECRET_82974", "SYNTHETIC_PROVENANCE_82974", "SECRET_NAME_849372" };
            foreach (var marker in sensitive)
            {
                Assert.True(record.PayloadEncrypted.AsSpan().IndexOf(Encoding.UTF8.GetBytes(marker)) < 0);
                Assert.True(audit.Payload.AsSpan().IndexOf(Encoding.UTF8.GetBytes(marker)) < 0);
                Assert.DoesNotContain(marker, string.Join('\n', logs));
                Assert.DoesNotContain(marker, string.Join('\n', commands.Commands));
            }
            // Public tax-rule tables intentionally contain their public provenance and deductions.
            foreach (var file in Directory.GetFiles(Path.GetDirectoryName(db.DatabasePath)!))
            foreach (var marker in sensitive.Where(m => !m.StartsWith("SYNTHETIC_PROVENANCE", StringComparison.Ordinal)))
            {
                using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var buffer = new MemoryStream();
                stream.CopyTo(buffer);
                var bytes = buffer.ToArray();
                Assert.True(bytes.AsSpan().IndexOf(Encoding.UTF8.GetBytes(marker)) < 0);
                Assert.True(bytes.AsSpan().IndexOf(Encoding.Unicode.GetBytes(marker)) < 0);
            }
            Assert.Equal("HISTORY_ACTOR_SECRET_82974", c.ReadAudit(audit).Actor);
            Assert.Contains("SYNTHETIC_PROVENANCE_82974", c.ReadAudit(audit).After!.Value.GetRawText());
            Assert.DoesNotContain(File.ReadAllText(db.KeyPath), string.Join('\n', logs));
            Assert.DoesNotContain("1234567890", snapshot.ToString());
        }
    }
}
