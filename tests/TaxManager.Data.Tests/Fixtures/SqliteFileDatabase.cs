using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TaxManager.Data.Infrastructure;
using TaxManager.Data.Protection;
using TaxManager.Data.Audit;
using System.Security.Cryptography;
using System.Text.Json;

namespace TaxManager.Data.Tests.Fixtures;

public sealed class SqliteFileDatabase : IDisposable
{
    private readonly string directory = Path.Combine(
        Path.GetTempPath(), "taxmanager-tests", Guid.NewGuid().ToString("N"));

    public SqliteFileDatabase()
    {
        Directory.CreateDirectory(directory);
        Directory.CreateDirectory(Path.Combine(directory, "data"));
        KeyPath = Path.Combine(directory, "synthetic-keys.json");
        File.WriteAllText(KeyPath, JsonSerializer.Serialize(new
        {
            activeKeyId = "synthetic", keys = new Dictionary<string, string>
            { ["synthetic"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) }
        }));
        Keys = FileKeyRing.Load(KeyPath, Path.GetFullPath("."), Path.Combine(directory, "data"));
    }

    public string DatabasePath => Path.Combine(directory, "data", "test.db");
    public string KeyPath { get; }
    public FileKeyRing Keys { get; }

    public TaxManagerDbContext Open(params IInterceptor[] interceptors)
    {
        var connectionString = $"Data Source={DatabasePath};Foreign Keys=True;Pooling=False";
        var options = new DbContextOptionsBuilder<TaxManagerDbContext>()
            .UseSqlite(connectionString)
            .AddInterceptors(interceptors)
            .Options;
        return new TaxManagerDbContext(options, Keys)
        {
            AuditContext = new AuditWriteContext("synthetic-caller", "fixture")
        };
    }

    public void Dispose()
    {
        Keys.Dispose();
        Directory.Delete(directory, true);
    }
}
