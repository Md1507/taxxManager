using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TaxManager.Data.Audit;
using TaxManager.Data.Dependents;
using TaxManager.Data.Income;
using TaxManager.Data.Infrastructure;
using TaxManager.Data.Profiles;
using TaxManager.Data.Protection;
using TaxManager.Data.TaxRules;

namespace TaxManager.Data.Upgrades;

/// <summary>Development-only offline copy-forward. Both paths must be synthetic files inside TEMP.</summary>
public sealed class SyntheticDatabaseUpgrader(params IInterceptor[] targetInterceptors)
{
    public async Task<UpgradeResult> UpgradeAsync(string sourcePath, string targetPath, FileKeyRing keys,
        bool syntheticDataOnly, CancellationToken token = default)
    {
        if (!syntheticDataOnly) throw new ArgumentException("Explicit synthetic-data intent is required.");
        var source = ValidatePath(sourcePath);
        var target = ValidatePath(targetPath);
        if (string.Equals(source, target, StringComparison.OrdinalIgnoreCase) || !TargetVacant(target))
            throw new ArgumentException("The target must be a distinct, new file.");
        token.ThrowIfCancellationRequested();
        var pending = target + ".pending-" + Guid.NewGuid().ToString("N");
        var created = false;
        try
        {
            await using var input = new SqliteConnection(new SqliteConnectionStringBuilder
            { DataSource = source, Mode = SqliteOpenMode.ReadOnly, Pooling = false, ForeignKeys = true }.ToString());
            await input.OpenAsync(token);
            using var snapshotTransaction = input.BeginTransaction(deferred: true);
            var data = await LegacyDataReader.ReadAsync(input, snapshotTransaction, token);
            // Reserve only our own random file. Neither source nor existing target is ever overwritten.
            using (new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { created = true; }
            await using (var output = Open(pending, keys))
            {
                await output.Database.MigrateAsync(token);
                output.AuditContext = new AuditWriteContext("synthetic-upgrader", "Explicit legacy import; timestamps describe import, not historical changes");
                output.AddRange(data.Profiles);
                output.AddRange(data.Incomes);
                output.AddRange(data.Dependents);
                output.AddRange(data.Periods);
                output.AddRange(data.Bases);
                output.AddRange(data.Rules);
                output.AddRange(data.Brackets);
                output.AddRange(data.Registrations);
                await output.SaveChangesAsync(token);
            }
            // New context, no tracked plaintext or identity-map cache may satisfy readback.
            await using (var verify = Open(pending, keys))
            {
                verify.ValidateProtection();
                Compare(verify, data.Profiles, await verify.UserProfiles.AsNoTracking().ToListAsync(token));
                Compare(verify, data.Incomes, await verify.MonthlyIncomes.AsNoTracking().ToListAsync(token));
                Compare(verify, data.Dependents, await verify.Dependents.AsNoTracking().ToListAsync(token));
                Compare(verify, data.Periods, await verify.DependentEligibilityPeriods.AsNoTracking().ToListAsync(token));
                Compare(verify, data.Bases, await verify.Set<TaxRuleLegalBasis>().AsNoTracking().ToListAsync(token));
                Compare(verify, data.Rules, await verify.Set<TaxRuleSnapshot>().AsNoTracking().ToListAsync(token));
                Compare(verify, data.Brackets, await verify.Set<TaxRuleBracket>().AsNoTracking().ToListAsync(token));
                Compare(verify, data.Registrations, await verify.Set<TaxRulePeriod>().AsNoTracking().ToListAsync(token));
                foreach (var audit in await verify.Set<AuditEvent>().AsNoTracking().ToListAsync(token))
                    _ = verify.ReadAudit(audit);
                await verify.Database.OpenConnectionAsync(token);
                await using var check = verify.Database.GetDbConnection().CreateCommand();
                check.CommandText = "PRAGMA foreign_key_check";
                await using (var reader = await check.ExecuteReaderAsync(token))
                    if (await reader.ReadAsync(token)) throw new InvalidOperationException("Upgrade foreign key validation failed.");
                check.CommandText = """
                    SELECT count(*) FROM sqlite_master WHERE type = 'trigger' AND
                    (name LIKE 'Period_NoOverlap_%' OR name LIKE 'TaxRulePeriod_NoOverlap_%')
                    """;
                if (Convert.ToInt64(await check.ExecuteScalarAsync(token)) != 4)
                    throw new InvalidOperationException("Upgrade triggers are missing.");
                check.CommandText = """
                    SELECT count(*) FROM pragma_table_info('UserProfiles') WHERE name = 'DisplayName'
                    UNION ALL SELECT count(*) FROM pragma_table_info('Dependents') WHERE name = 'DisplayName'
                    UNION ALL SELECT count(*) FROM pragma_table_info('MonthlyIncomes') WHERE name IN ('GrossIncome','InsuranceAmount','ActualWithheldTax')
                    """;
                await using (var reader = await check.ExecuteReaderAsync(token))
                    while (await reader.ReadAsync(token))
                        if (reader.GetInt64(0) != 0) throw new InvalidOperationException("Legacy plaintext columns remain.");
                check.CommandText = "PRAGMA wal_checkpoint(TRUNCATE)";
                await check.ExecuteNonQueryAsync(token);
                check.CommandText = "PRAGMA journal_mode=DELETE";
                await check.ExecuteNonQueryAsync(token);
            }
            token.ThrowIfCancellationRequested();
            if (!TargetVacant(target)) throw new IOException("Synthetic target or its SQLite sidecars already exist.");
            File.Move(pending, target, overwrite: false);
            return new(target, data.Profiles.Count, data.Incomes.Count, data.Dependents.Count, data.Periods.Count);
        }
        finally
        {
            if (created)
                foreach (var file in new[] { pending, pending + "-wal", pending + "-shm", pending + "-journal" })
                    if (File.Exists(file)) File.Delete(file);
        }
    }

    private static bool TargetVacant(string target) => !Directory.Exists(target) &&
        new[] { target, target + "-wal", target + "-shm", target + "-journal" }.All(p => !File.Exists(p));

    private TaxManagerDbContext Open(string path, FileKeyRing keys) => new(
        new DbContextOptionsBuilder<TaxManagerDbContext>().UseSqlite(new SqliteConnectionStringBuilder
        { DataSource = path, Pooling = false, ForeignKeys = true }.ToString()).AddInterceptors(targetInterceptors).Options, keys);

    private static string ValidatePath(string path)
    {
        if (!Path.IsPathFullyQualified(path)) throw new ArgumentException("Absolute synthetic paths are required.");
        var full = Path.GetFullPath(path);
        var relative = Path.GetRelativePath(Path.GetFullPath(Path.GetTempPath()), full);
        if (Path.IsPathRooted(relative) || relative == ".." || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) ||
            relative == "." || string.Equals(Path.GetFileName(full), "taxmanager.local.db", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Only synthetic files under TEMP are supported.");
        for (FileSystemInfo? part = new FileInfo(full); part is not null; part = part switch
             { FileInfo f => f.Directory, DirectoryInfo d => d.Parent, _ => null })
            if (part.Exists && (part.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new ArgumentException("Synthetic paths must not contain links.");
        return full;
    }

    private static void Compare<T>(TaxManagerDbContext context, IEnumerable<T> expected, IEnumerable<T> actual) where T : class
    {
        string Encode(T entity)
        {
            if (ProtectionSession.IsBusiness(entity))
                return JsonSerializer.Serialize(new { Profile = ProtectionSession.ProfileId(entity),
                    Key = ProtectionSession.RowKey(entity), Value = ProtectionSession.Snapshot(entity) });
            var properties = context.Model.FindEntityType(typeof(T))!.GetProperties().OrderBy(p => p.Name);
            return JsonSerializer.Serialize(properties.ToDictionary(p => p.Name, p => p.PropertyInfo!.GetValue(entity)));
        }
        if (!expected.Select(Encode).Order(StringComparer.Ordinal).SequenceEqual(actual.Select(Encode).Order(StringComparer.Ordinal)))
            throw new InvalidOperationException("Upgrade readback did not match the legacy snapshot.");
    }
}
