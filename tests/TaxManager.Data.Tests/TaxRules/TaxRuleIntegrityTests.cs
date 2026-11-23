using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TaxManager.Core;
using TaxManager.Data.TaxRules;
using TaxManager.Data.Tests.Fixtures;
using Xunit;

namespace TaxManager.Data.Tests.TaxRules;

public class TaxRuleIntegrityTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LastRegistrationCannotBeRemoved(bool asyncSave)
    {
        using var database = new SqliteFileDatabase();
        await using (var setup = database.Open())
        {
            await setup.Database.MigrateAsync();
            await new TaxRuleStore(setup).SeedReferencesAsync();
        }
        await using (var context = database.Open())
        {
            var period = await context.Set<TaxRulePeriod>().FirstAsync();
            context.Remove(period);
            var error = asyncSave
                ? await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync())
                : Assert.Throws<InvalidOperationException>(() => context.SaveChanges());
            Assert.Contains("without a registration", error.Message);
        }
        await using var fresh = database.Open();
        await new TaxRuleStore(fresh).LoadReferencesAsync();
        Assert.Equal(2, await fresh.Set<TaxRulePeriod>().CountAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RegistrationCanBeRemovedWhenAnotherRemains(bool asyncSave)
    {
        using var database = new SqliteFileDatabase();
        await using (var setup = database.Open())
        {
            await setup.Database.MigrateAsync();
            var basis = new LegalBasis(VerificationStatus.Verified, "synthetic", "1",
                sourceUrl: "https://vbpl.vn/Pages/vbpq-toanvan.aspx?ItemID=1",
                verifiedOn: new DateOnly(2026, 1, 1));
            var rule = new TaxRuleSet("synthetic", "1", "monthly", 2024, 0m, 0m,
                [new Bracket(null, 0.1m, basis)], basis, basis, basis, basis);
            await new TaxRuleStore(setup).ImportOfficialAsync(
            [
                new TaxRuleRegistration(rule, new DateOnly(2023, 1, 1), new DateOnly(2023, 12, 31)),
                new TaxRuleRegistration(rule, new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31))
            ]);
        }
        await using (var context = database.Open())
        {
            context.Remove(await context.Set<TaxRulePeriod>().FirstAsync());
            if (asyncSave)
                await context.SaveChangesAsync();
            else
                context.SaveChanges();
        }
        await using var fresh = database.Open();
        await new TaxRuleStore(fresh).LoadOfficialAsync();
        Assert.Equal(1, await fresh.Set<TaxRulePeriod>().CountAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ModifiedSnapshotChecksUnloadedCorruptBrackets(bool asyncSave)
    {
        using var database = new SqliteFileDatabase();
        await using (var setup = database.Open())
        {
            await setup.Database.MigrateAsync();
            await new TaxRuleStore(setup).SeedReferencesAsync();
            await setup.Database.ExecuteSqlRawAsync("UPDATE TaxRuleBracket SET UpperBound = '1' WHERE Ordinal = 1");
        }
        await using var context = database.Open();
        var snapshot = await context.Set<TaxRuleSnapshot>().FirstAsync();
        snapshot.PersonalDeduction += 1;
        if (asyncSave)
            await Assert.ThrowsAsync<ArgumentException>(() => context.SaveChangesAsync());
        else
            Assert.Throws<ArgumentException>(() => context.SaveChanges());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task DeletionCannotOrphanLegalBases(bool deleteBracket, bool asyncSave)
    {
        using var database = new SqliteFileDatabase();
        await using (var setup = database.Open())
        {
            await setup.Database.MigrateAsync();
            await new TaxRuleStore(setup).SeedReferencesAsync();
        }
        await using (var context = database.Open())
        {
            var snapshot = await context.Set<TaxRuleSnapshot>().FirstAsync();
            if (deleteBracket)
            {
                var brackets = await context.Set<TaxRuleBracket>()
                    .Where(x => x.RuleId == snapshot.RuleId && x.Version == snapshot.Version)
                    .OrderBy(x => x.Ordinal).ToListAsync();
                brackets[^2].UpperBound = null;
                context.Remove(brackets[^1]);
            }
            else
                context.Remove(snapshot);

            if (asyncSave)
                await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
            else
                Assert.Throws<InvalidOperationException>(() => context.SaveChanges());
        }
        await using var fresh = database.Open();
        await new TaxRuleStore(fresh).LoadReferencesAsync();
    }

    [Fact]
    public async Task CompleteRemovalRetainsSharedBasesAndOtherOfficialSnapshot()
    {
        using var database = new SqliteFileDatabase();
        await using (var setup = database.Open())
        {
            await setup.Database.MigrateAsync();
            var basis = new LegalBasis(VerificationStatus.Verified, "synthetic", "1",
                sourceUrl: "https://vbpl.vn/Pages/vbpq-toanvan.aspx?ItemID=1",
                verifiedOn: new DateOnly(2026, 1, 1));
            TaxRuleSet Rule(string id) => new(id, "1", "monthly", 2024, 0m, 0m,
                [new Bracket(null, 0.1m, basis)], basis, basis, basis, basis);
            await new TaxRuleStore(setup).ImportOfficialAsync(
            [
                new TaxRuleRegistration(Rule("a"), new DateOnly(2023, 1, 1), new DateOnly(2023, 12, 31)),
                new TaxRuleRegistration(Rule("b"), new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31))
            ]);
        }
        await using (var context = database.Open())
        {
            var first = await context.Set<TaxRuleSnapshot>().SingleAsync(x => x.RuleId == "a");
            var second = await context.Set<TaxRuleSnapshot>().SingleAsync(x => x.RuleId == "b");
            var oldKey = second.PersonalBasisKey;
            second.PersonalBasisKey = first.PersonalBasisKey;
            context.Remove(await context.Set<TaxRuleLegalBasis>().SingleAsync(x => x.Key == oldKey));
            await context.SaveChangesAsync();
        }
        await using (var context = database.Open())
        {
            var first = await context.Set<TaxRuleSnapshot>().SingleAsync(x => x.RuleId == "a");
            var second = await context.Set<TaxRuleSnapshot>().SingleAsync(x => x.RuleId == "b");
            var firstBrackets = await context.Set<TaxRuleBracket>().Where(x => x.RuleId == "a").ToListAsync();
            var keys = new[] { first.PersonalBasisKey, first.DependentBasisKey, first.IncomeBasisKey, first.TaxBasisKey }
                .Concat(firstBrackets.Select(x => x.BasisKey)).Where(x => x != second.PersonalBasisKey).Distinct().ToArray();
            context.RemoveRange(firstBrackets);
            context.Remove(first);
            context.RemoveRange(await context.Set<TaxRuleLegalBasis>().Where(x => keys.Contains(x.Key)).ToListAsync());
            await context.SaveChangesAsync();
        }
        await using var fresh = database.Open();
        await new TaxRuleStore(fresh).LoadOfficialAsync();
        Assert.Equal(1, await fresh.Set<TaxRuleSnapshot>().CountAsync());
    }

    [Theory]
    [InlineData("UPDATE TaxRuleSnapshot SET FromYear = 2025.5")]
    [InlineData("UPDATE TaxRuleLegalBasis SET Status = 1.5")]
    [InlineData("UPDATE TaxRuleBracket SET Ordinal = 0.5 WHERE Ordinal = 0")]
    [InlineData("UPDATE TaxRuleBracket SET Ordinal = 2147483648 WHERE Ordinal = 0")]
    public async Task IntegerColumnsRejectNonIntegerAndOutOfRange(string sql)
    {
        using var database = new SqliteFileDatabase();
        await using var context = database.Open();
        await context.Database.MigrateAsync();
        await new TaxRuleStore(context).SeedReferencesAsync();
        await Assert.ThrowsAsync<SqliteException>(() => context.Database.ExecuteSqlRawAsync(sql));
    }

    [Theory]
    [InlineData("UPDATE TaxRuleSnapshot SET FromYear = 2025.5")]
    [InlineData("UPDATE TaxRuleLegalBasis SET Status = 1.5")]
    [InlineData("UPDATE TaxRuleBracket SET Ordinal = 0.5 WHERE Ordinal = 0")]
    public async Task CorruptIntegerStorageIsNotTruncated(string sql)
    {
        using var database = new SqliteFileDatabase();
        await using (var context = database.Open())
        {
            await context.Database.MigrateAsync();
            await new TaxRuleStore(context).SeedReferencesAsync();
            await context.Database.OpenConnectionAsync();
            await context.Database.ExecuteSqlRawAsync("PRAGMA ignore_check_constraints = ON");
            await context.Database.ExecuteSqlRawAsync(sql);
        }
        await using var fresh = database.Open();
        var store = new TaxRuleStore(fresh);
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.LoadReferencesAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.SeedReferencesAsync());
    }

    [Theory]
    [InlineData("TaxRuleSnapshot", "PersonalDeduction")]
    [InlineData("TaxRuleSnapshot", "DependentDeduction")]
    [InlineData("TaxRuleBracket", "UpperBound")]
    [InlineData("TaxRuleBracket", "Rate")]
    public async Task PrecisionOverflowIsRejectedBeforeLoadingOrSeeding(string table, string column)
    {
        using var database = new SqliteFileDatabase();
        await using (var context = database.Open())
        {
            await context.Database.MigrateAsync();
            await new TaxRuleStore(context).SeedReferencesAsync();
            var sql = $"UPDATE {table} SET {column} = '0.12345678901234567890123456789'";
            await context.Database.ExecuteSqlRawAsync(sql);
        }
        await using var fresh = database.Open();
        var store = new TaxRuleStore(fresh);
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.LoadReferencesAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.SeedReferencesAsync());
    }

    [Fact]
    public async Task EqualDecimalRepresentationsAreAccepted()
    {
        using var database = new SqliteFileDatabase();
        await using (var context = database.Open())
        {
            await context.Database.MigrateAsync();
            await new TaxRuleStore(context).SeedReferencesAsync();
            await context.Database.ExecuteSqlRawAsync(
                "UPDATE TaxRuleSnapshot SET PersonalDeduction = PersonalDeduction || '000'");
        }
        await using var fresh = database.Open();
        await new TaxRuleStore(fresh).LoadReferencesAsync();
        await new TaxRuleStore(fresh).SeedReferencesAsync();
    }

    [Fact]
    public async Task ImportRejectsDirtyContextWithoutSavingPendingWork()
    {
        using var database = new SqliteFileDatabase();
        await using var context = database.Open();
        await context.Database.MigrateAsync();
        context.Add(new TaxRuleBracket { RuleId = "pending", Version = "1", Ordinal = 0 });
        await Assert.ThrowsAsync<InvalidOperationException>(() => new TaxRuleStore(context).SeedReferencesAsync());
        Assert.True(context.ChangeTracker.HasChanges());
        await using var fresh = database.Open();
        Assert.Equal(0, await fresh.Set<TaxRuleBracket>().CountAsync());
    }

    [Fact]
    public async Task IndependentBracketMutationIsValidatedOnBothSavePaths()
    {
        using var database = new SqliteFileDatabase();
        await using (var context = database.Open())
        {
            await context.Database.MigrateAsync();
            await new TaxRuleStore(context).SeedReferencesAsync();
        }
        await using var fresh = database.Open();
        var bracket = await fresh.Set<TaxRuleBracket>().FirstAsync(x => x.Ordinal == 1);
        bracket.UpperBound = 1m;
        Assert.Throws<ArgumentException>(() => fresh.SaveChanges());
        await Assert.ThrowsAsync<ArgumentException>(() => fresh.SaveChangesAsync());
    }

    [Fact]
    public async Task RemovingIndependentBracketCannotLeaveGap()
    {
        using var database = new SqliteFileDatabase();
        await using (var context = database.Open())
        {
            await context.Database.MigrateAsync();
            await new TaxRuleStore(context).SeedReferencesAsync();
        }
        await using var fresh = database.Open();
        var bracket = await fresh.Set<TaxRuleBracket>().FirstAsync(x => x.Ordinal == 1);
        fresh.Remove(bracket);
        await Assert.ThrowsAsync<ArgumentException>(() => fresh.SaveChangesAsync());
    }

    [Fact]
    public async Task BatchOverlapRollsBackAllTablesAndContextCanBeReused()
    {
        using var database = new SqliteFileDatabase();
        await using var context = database.Open();
        await context.Database.MigrateAsync();
        var store = new TaxRuleStore(context);
        await store.SeedReferencesAsync();
        var before = await TableContents(context);
        var basis = new LegalBasis(VerificationStatus.Verified, "synthetic", "1",
            sourceUrl: "https://vbpl.vn/Pages/vbpq-toanvan.aspx?ItemID=1",
            verifiedOn: new DateOnly(2026, 1, 1));
        TaxRuleSet Rule(string id) => new(id, "1", "monthly", 2024, 0m, 0m,
            [new Bracket(null, 0.1m, basis)], basis, basis, basis, basis);
        await Assert.ThrowsAsync<DbUpdateException>(() => store.ImportOfficialAsync(
        [
            new TaxRuleRegistration(Rule("a"), new DateOnly(2023, 1, 1), new DateOnly(2023, 12, 31)),
            new TaxRuleRegistration(Rule("b"), new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31))
        ]));
        await using (var fresh = database.Open())
            Assert.Equal(before, await TableContents(fresh));
        await store.SeedReferencesAsync();
    }

    private static async Task<string[]> TableContents(TaxManager.Data.Infrastructure.TaxManagerDbContext context)
    {
        await context.Database.OpenConnectionAsync();
        var result = new List<string>();
        foreach (var table in new[] { "TaxRuleSnapshot", "TaxRuleBracket", "TaxRuleLegalBasis", "TaxRulePeriod" })
        {
            await using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = $"SELECT * FROM {table} ORDER BY rowid";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var values = new object[reader.FieldCount];
                reader.GetValues(values);
                result.Add(System.Text.Json.JsonSerializer.Serialize(values));
            }
        }
        return result.ToArray();
    }
}
