using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using TaxManager.Data.Protection;
using TaxManager.Data.Infrastructure;
using TaxManager.Web.Demo;
using TaxManager.Web.Tests.MonthlySalary;
using Xunit;

namespace TaxManager.Web.Tests.Security;

public sealed class SyntheticStorageTests
{
    [Fact]
    public async Task Demo_uses_separate_temp_key_and_ciphertext_storage_without_logging_input()
    {
        const string marker = "789654321.123456789";
        await using var host = await DemoHost.StartAsync();
        using var form = new FormClient(host.Address);
        var result = await form.PostAsync(gross: marker);
        Assert.Contains("Kết quả 12 tháng", result);
        var db = host.Services.GetRequiredService<SyntheticDatabase>();
        await using var context = db.Open();
        context.ValidateProtection();
        var dbPath = Path.GetFullPath(context.Database.GetDbConnection().DataSource);
        var dbDirectory = Path.GetDirectoryName(dbPath)!;
        var root = Directory.GetParent(dbDirectory)!.FullName;
        var keyPath = Path.Combine(root, "synthetic-key.json");
        Assert.StartsWith(Path.GetFullPath(Path.GetTempPath()), dbPath, StringComparison.OrdinalIgnoreCase);
        Assert.NotEqual(Path.GetDirectoryName(keyPath), dbDirectory);
        Assert.DoesNotContain("taxmanager.local.db", dbPath, StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(keyPath));
        Assert.Throws<DataProtectionException>(() => FileKeyRing.Load(keyPath, root, dbDirectory));
        using var second = new SyntheticDatabase(Directory.GetCurrentDirectory());
        await using var secondContext = second.Open();
        var secondDbPath = Path.GetFullPath(secondContext.Database.GetDbConnection().DataSource);
        Assert.NotEqual(dbPath, secondDbPath);
        var secondRoot = Directory.GetParent(Path.GetDirectoryName(secondDbPath)!)!.FullName;
        var secondKey = Path.Combine(secondRoot, "synthetic-key.json");
        using var wrongKeys = FileKeyRing.Load(secondKey, Directory.GetCurrentDirectory(), dbDirectory);
        var options = new DbContextOptionsBuilder<TaxManagerDbContext>()
            .UseSqlite($"Data Source={dbPath};Foreign Keys=True;Pooling=False").Options;
        await using var wrongContext = new TaxManagerDbContext(options, wrongKeys);
        Assert.Throws<DataProtectionException>(() => wrongContext.ValidateProtection());
        Assert.Empty(host.Services.GetServices<ILoggerProvider>());
        foreach (var file in Directory.EnumerateFiles(dbDirectory))
        {
            var bytes = File.ReadAllBytes(file);
            Assert.DoesNotContain(marker, Encoding.UTF8.GetString(bytes));
            Assert.DoesNotContain(marker, Encoding.Unicode.GetString(bytes));
        }
    }
}
