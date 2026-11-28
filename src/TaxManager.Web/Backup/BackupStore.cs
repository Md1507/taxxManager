using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using TaxManager.Data.Audit;
using TaxManager.Data.Forecasting;
using TaxManager.Data.Infrastructure;
using TaxManager.Data.Protection;
using TaxManager.Data.TaxRules;
using TaxManager.Web.Demo;
using TaxManager.Core.Forecasting;

namespace TaxManager.Web.Backup;

public sealed record BackupSummary(DateTimeOffset CreatedAt, int Profiles, int Versions, int MonthlyResults, int AnnualResults, int AuditEvents, Guid[] ProfileIds);

/// <summary>Validates a whole-store copy without writing to the source or upgrading an import.</summary>
public sealed class BackupStore
{
    private readonly Lazy<string[]> expectedSchema = new(CreateSchema);

    internal static string Fingerprint(SyntheticDatabase store)
    {
        foreach (var path in new[] { store.RootPath, Path.Combine(store.RootPath, "data"), DbPath(store.RootPath), KeyPath(store.RootPath) })
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) throw new InvalidDataException("Staged store contains a link.");
        if (new FileInfo(DbPath(store.RootPath)).Length > BackupEnvelope.MaxDatabaseBytes || new FileInfo(KeyPath(store.RootPath)).Length > BackupEnvelope.MaxKeyBytes)
            throw new InvalidDataException("Staged store exceeds limits.");
        using var db = File.OpenRead(DbPath(store.RootPath));
        using var key = File.OpenRead(KeyPath(store.RootPath));
        return Convert.ToHexString(SHA256.HashData(db)) + Convert.ToHexString(SHA256.HashData(key));
    }

    public async Task<byte[]> ExportAsync(SyntheticDatabase source, string password, DateTimeOffset now, CancellationToken token)
    {
        var root = CreateRoot();
        try
        {
            await using (var context = source.Open())
            {
                await context.Database.OpenConnectionAsync(token);
                using var target = Connection(root);
                await target.OpenAsync(token);
                ((SqliteConnection)context.Database.GetDbConnection()).BackupDatabase(target);
            }
            var keys = await File.ReadAllBytesAsync(Path.Combine(source.RootPath, "synthetic-key.json"), token);
            try
            {
                if (keys.Length > BackupEnvelope.MaxKeyBytes) throw new InvalidDataException("Key file is too large.");
                await File.WriteAllBytesAsync(KeyPath(root), keys, token);
                await ValidateAsync(root, now, token);
                var db = await File.ReadAllBytesAsync(DbPath(root), token);
                try { return BackupEnvelope.Seal(db, keys, password, now); }
                finally { CryptographicOperations.ZeroMemory(db); }
            }
            finally { CryptographicOperations.ZeroMemory(keys); }
        }
        finally { DeleteRoot(root); }
    }

    public async Task<(SyntheticDatabase Store, BackupSummary Summary)> ImportAsync(byte[] file, string password, CancellationToken token)
    {
        using var contents = BackupEnvelope.Open(file, password);
        var root = CreateRoot();
        try
        {
            await File.WriteAllBytesAsync(DbPath(root), contents.Database, token);
            await File.WriteAllBytesAsync(KeyPath(root), contents.Keys, token);
            var summary = await ValidateAsync(root, contents.CreatedAt, token);
            token.ThrowIfCancellationRequested();
            // Validation above guarantees current schema, so the ordinary opener cannot migrate/seed.
            return (new SyntheticDatabase(Directory.GetCurrentDirectory(), root), summary);
        }
        catch { DeleteRoot(root); throw; }
    }

    private async Task<BackupSummary> ValidateAsync(string root, DateTimeOffset created, CancellationToken token)
    {
        if (new FileInfo(DbPath(root)).Length > BackupEnvelope.MaxDatabaseBytes) throw new InvalidDataException("Database is too large.");
        using (var connection = Connection(root))
        {
            await connection.OpenAsync(token);
            if (!ReadSchema(connection).SequenceEqual(expectedSchema.Value)) throw new InvalidDataException("Unsupported database schema.");
            using var check = connection.CreateCommand();
            check.CommandText = "PRAGMA integrity_check";
            if (!Equals(await check.ExecuteScalarAsync(token), "ok")) throw new InvalidDataException("Database integrity failed.");
            check.CommandText = "PRAGMA foreign_key_check";
            using var violations = await check.ExecuteReaderAsync(token);
            if (await violations.ReadAsync(token)) throw new InvalidDataException("Database relations are invalid.");
        }
        using var keys = FileKeyRing.Load(KeyPath(root), Path.GetFullPath(Directory.GetCurrentDirectory()), Path.Combine(root, "data"));
        var options = new DbContextOptionsBuilder<TaxManagerDbContext>().UseSqlite(ConnectionString(root)).Options;
        await using var context = new TaxManagerDbContext(options, keys);
        if (!(await context.Database.GetAppliedMigrationsAsync(token)).SequenceEqual(context.Database.GetMigrations()))
            throw new InvalidDataException("Unsupported migration version.");
        await using var transaction = await context.Database.BeginTransactionAsync(token);
        // Truly untouched stores may have no sentinel. Any initialized store must authenticate even an empty read.
        if (await context.Set<DataProtectionMetadata>().AnyAsync(token)) context.ValidateProtection();
        var documents = await new ForecastStore(context).ListAsync(token);
        foreach (var document in documents) ValidateDraft(document.Payload);
        var snapshots = await context.Set<ForecastSnapshot>().AsNoTracking().ToArrayAsync(token);
        foreach (var snapshot in snapshots)
        {
            if (snapshot.SchemaVersion != 1 || snapshot.Revision < 1) throw new InvalidDataException("Unsupported snapshot.");
            ValidateDraft(snapshot.Payload);
        }
        foreach (var document in documents)
            if (!snapshots.Any(s => s.DocumentId == document.Id && s.Revision == document.Revision && s.Payload == document.Payload))
                throw new InvalidDataException("Current document snapshot is missing.");
        var profiles = await context.UserProfiles.AsNoTracking().ToArrayAsync(token);
        foreach (var profile in profiles) _ = profile.DisplayName;
        foreach (var dependent in await context.Dependents.AsNoTracking().ToArrayAsync(token)) _ = dependent.DisplayName;
        foreach (var income in await context.MonthlyIncomes.AsNoTracking().ToArrayAsync(token))
            _ = (income.GrossIncome, income.InsuranceAmount, income.ActualWithheldTax);
        var monthly = await context.CalculationHistory.AsNoTracking().ToArrayAsync(token);
        foreach (var record in monthly) _ = record.Snapshot;
        var annual = await context.Set<AnnualCalculationRecord>().AsNoTracking().ToArrayAsync(token);
        foreach (var record in annual) _ = record.Snapshot;
        var audit = await context.Set<AuditEvent>().AsNoTracking().ToArrayAsync(token);
        foreach (var record in audit) _ = context.ReadAudit(record);
        // The annual demo adapter currently accepts exactly its supported reference catalog.
        // Compare stored rows without seeding or recalculating historical results.
        var rules = await context.Set<AnnualForecastRule>().AsNoTracking().OrderBy(r => r.PeriodFrom).ToArrayAsync(token);
        if (!rules.Select(r => (r.Year, r.PeriodFrom, r.PeriodTo, r.Graph)).SequenceEqual(ExpectedAnnualRules.Value))
            throw new InvalidDataException("Unsupported annual catalog.");
        await transaction.CommitAsync(token);
        // The public catalog loader owns its own transaction; staging is immutable during validation.
        await transaction.DisposeAsync();
        _ = await new TaxRuleStore(context).LoadReferencesAsync(token);
        return new(created, documents.Count + profiles.Length, snapshots.Length, monthly.Length, annual.Length, audit.Length,
            documents.Select(d => d.Id).Concat(profiles.Select(p => p.Id)).Distinct().ToArray());
    }

    private static void ValidateDraft(string? payload)
    {
        var draft = ForecastDraftReader.Read(payload ?? throw new InvalidDataException("Missing forecast payload."));
        if (draft.Year != 2026 || draft.Bonuses is null) throw new InvalidDataException("Unsupported forecast input.");
        _ = AnnualForecastCalculator.Assess(draft.Months, draft.Bonuses);
        if (draft.Months.Any(m => m.Gross?.Value < 0 || m.Insurance?.Value < 0 || m.Dependents?.Value < 0 || m.Withheld?.Value < 0))
            throw new InvalidDataException("Negative forecast input.");
    }

    private static readonly Lazy<(int, DateOnly, DateOnly, string)[]> ExpectedAnnualRules = new(() =>
    {
        using var template = new SyntheticDatabase(Directory.GetCurrentDirectory());
        try
        {
            using var context = template.Open();
            return context.Set<AnnualForecastRule>().AsNoTracking().OrderBy(r => r.PeriodFrom)
                .AsEnumerable().Select(r => (r.Year, r.PeriodFrom, r.PeriodTo, r.Graph)).ToArray();
        }
        finally { template.Dispose(); DeleteRoot(template.RootPath); }
    });

    private static string[] CreateSchema()
    {
        using var template = new SyntheticDatabase(Directory.GetCurrentDirectory());
        try { using var connection = Connection(template.RootPath); connection.Open(); return ReadSchema(connection); }
        finally { template.Dispose(); DeleteRoot(template.RootPath); }
    }

    private static string[] ReadSchema(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT type || ':' || name || ':' || coalesce(sql, '') FROM sqlite_schema WHERE name NOT LIKE 'sqlite_%' ORDER BY type, name";
        using var reader = command.ExecuteReader();
        var rows = new List<string>();
        while (reader.Read()) rows.Add(reader.GetString(0));
        return rows.ToArray();
    }
    private static string ConnectionString(string root) => new SqliteConnectionStringBuilder { DataSource = DbPath(root), Pooling = false, ForeignKeys = true }.ToString();
    private static SqliteConnection Connection(string root) => new(ConnectionString(root));
    private static string DbPath(string root) => Path.Combine(root, "data", "synthetic.db");
    private static string KeyPath(string root) => Path.Combine(root, "synthetic-key.json");

    internal static string CreateRoot()
    {
        // Reuse the established TEMP/link guard before creating a path for decrypted material.
        var temp = Path.GetFullPath(Path.GetTempPath());
        for (DirectoryInfo? part = new(temp); part is not null; part = part.Parent)
            if ((part.Attributes & FileAttributes.ReparsePoint) != 0) throw new InvalidDataException("TEMP contains a link.");
        var root = Path.Combine(temp, "taxmanager-web-demo-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "data"));
        File.WriteAllText(Path.Combine(root, "synthetic-demo.marker"), "TaxManager synthetic demo v1");
        return root;
    }

    internal static void DeleteRoot(string root)
    {
        var full = Path.GetFullPath(root);
        if (Path.GetDirectoryName(full) != Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar) ||
            !Path.GetFileName(full).StartsWith("taxmanager-web-demo-", StringComparison.Ordinal))
            throw new InvalidOperationException("Refusing cleanup outside an owned TEMP store.");
        if (!Directory.Exists(full)) return;
        // Do not traverse a replaced root or child link, even on cleanup after a failed import.
        RejectLinks(new DirectoryInfo(full));
        Directory.Delete(full, recursive: true);
    }
    private static void RejectLinks(FileSystemInfo item)
    {
        if ((item.Attributes & FileAttributes.ReparsePoint) != 0) throw new InvalidOperationException("Refusing linked cleanup.");
        if (item is DirectoryInfo directory)
            foreach (var child in directory.EnumerateFileSystemInfos()) RejectLinks(child);
    }
}
