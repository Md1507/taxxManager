using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TaxManager.Data.Audit;
using TaxManager.Data.Infrastructure;
using TaxManager.Data.Protection;
using TaxManager.Data.TaxRules;

namespace TaxManager.Web.Demo;

/// <summary>Only creates a new synthetic database and an unrelated synthetic key in TEMP.</summary>
public sealed class SyntheticDatabase : IDisposable
{
    private readonly string databasePath;
    private readonly FileKeyRing keys;

    public SyntheticDatabase(string repositoryDirectory)
    {
        var temp = Path.GetFullPath(Path.GetTempPath());
        for (DirectoryInfo? part = new(temp); part is not null; part = part.Parent)
            if ((part.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException("TEMP không được chứa liên kết.");
        var root = Path.Combine(temp, "taxmanager-web-demo-" + Guid.NewGuid().ToString("N"));
        var databaseDirectory = Path.Combine(root, "data");
        Directory.CreateDirectory(databaseDirectory);
        var keyPath = Path.Combine(root, "synthetic-key.json");
        using (var file = new FileStream(keyPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            JsonSerializer.Serialize(file, new { activeKeyId = "synthetic", keys = new Dictionary<string, string>
                { ["synthetic"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) } });
        keys = FileKeyRing.Load(keyPath, Path.GetFullPath(repositoryDirectory), databaseDirectory);
        databasePath = Path.Combine(databaseDirectory, "synthetic.db");
        using (new FileStream(databasePath, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { }
        using var context = Open();
        context.Database.Migrate();
        new TaxRuleStore(context).SeedReferencesAsync().GetAwaiter().GetResult();
    }

    public TaxManagerDbContext Open()
    {
        var options = new DbContextOptionsBuilder<TaxManagerDbContext>()
            .UseSqlite($"Data Source={databasePath};Foreign Keys=True;Pooling=False").Options;
        return new TaxManagerDbContext(options, keys)
        {
            AuditContext = new AuditWriteContext("synthetic-demo", "demo", OperationId: Guid.NewGuid())
        };
    }

    public void Dispose() => keys.Dispose();
}
