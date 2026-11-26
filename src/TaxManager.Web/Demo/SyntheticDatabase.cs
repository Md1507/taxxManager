using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TaxManager.Data.Audit;
using TaxManager.Data.Infrastructure;
using TaxManager.Data.Protection;
using TaxManager.Data.TaxRules;

namespace TaxManager.Web.Demo;

/// <summary>Creates or explicitly reopens an isolated synthetic store in TEMP.</summary>
public sealed class SyntheticDatabase : IDisposable
{
    private readonly string databasePath;
    private readonly FileKeyRing keys;

    public string RootPath { get; }

    public SyntheticDatabase(string repositoryDirectory, string? existingRoot = null)
    {
        var temp = Path.GetFullPath(Path.GetTempPath());
        for (DirectoryInfo? part = new(temp); part is not null; part = part.Parent)
            if ((part.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException("TEMP không được chứa liên kết.");
        var root = existingRoot is null ? Path.Combine(temp, "taxmanager-web-demo-" + Guid.NewGuid().ToString("N")) : Path.GetFullPath(existingRoot);
        if (existingRoot is not null && (!Path.IsPathFullyQualified(existingRoot) ||
            !string.Equals(Path.GetDirectoryName(root), temp.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase) ||
            !Path.GetFileName(root).StartsWith("taxmanager-web-demo-", StringComparison.Ordinal)))
            throw new InvalidOperationException("Chỉ mở lại kho demo synthetic ngay dưới TEMP.");
        RootPath = root;
        var databaseDirectory = Path.Combine(root, "data");
        var keyPath = Path.Combine(root, "synthetic-key.json");
        databasePath = Path.Combine(databaseDirectory, "synthetic.db");
        var marker = Path.Combine(root, "synthetic-demo.marker");
        if (existingRoot is null)
        {
            Directory.CreateDirectory(databaseDirectory);
            File.WriteAllText(marker, "TaxManager synthetic demo v1");
            using (var file = new FileStream(keyPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                JsonSerializer.Serialize(file, new { activeKeyId = "synthetic", keys = new Dictionary<string, string>
                    { ["synthetic"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) } });
            using (new FileStream(databasePath, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { }
        }
        else
        {
            foreach (var path in new[] { root, databaseDirectory, keyPath, databasePath, marker })
                if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidOperationException("Kho synthetic không được chứa liên kết.");
            if (File.ReadAllText(marker) != "TaxManager synthetic demo v1")
                throw new InvalidOperationException("Không nhận diện được kho synthetic.");
        }
        keys = FileKeyRing.Load(keyPath, Path.GetFullPath(repositoryDirectory), databaseDirectory);
        using var context = Open();
        if (existingRoot is null)
        {
            context.Database.Migrate();
            new TaxRuleStore(context).SeedReferencesAsync().GetAwaiter().GetResult();
        }
        else new TaxManager.Data.Forecasting.ForecastStore(context).ListAsync().GetAwaiter().GetResult();
        Console.WriteLine($"Kho demo synthetic: {RootPath}");
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
