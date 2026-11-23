using Microsoft.EntityFrameworkCore;
using TaxManager.Core;
using TaxManager.Data.TaxRules;
using TaxManager.Data.Tests.Fixtures;
using Xunit;

namespace TaxManager.Data.Tests.TaxRules;

public class TaxRuleStoreTests
{
    [Fact]
    public async Task ReferenceSeedIsExplicitIdempotentAndLoadsFromFreshContext()
    {
        using var database = new SqliteFileDatabase();
        await using (var context = database.Open())
        {
            await context.Database.MigrateAsync();
            await Assert.ThrowsAsync<InvalidOperationException>(() => new TaxRuleStore(context).LoadReferencesAsync());
            await new TaxRuleStore(context).SeedReferencesAsync();
        }
        await using (var context = database.Open())
            await new TaxRuleStore(context).SeedReferencesAsync();
        await using (var context = database.Open())
        {
            var store = new TaxRuleStore(context);
            var provider = await store.LoadReferencesAsync();
            var rules = provider.GetForMonth(2025, 1);
            Assert.Equal(TaxRuleSet.Vietnam2025.PersonalDeduction, rules.PersonalDeduction);
            Assert.Equal(TaxRuleSet.Vietnam2025.Brackets.Select(b => b.Basis), rules.Brackets.Select(b => b.Basis));
            Assert.Equal(250000m, MonthlySalaryCalculator.Calculate(16000000m, 0, 0, rules).Pit.TotalTax.Value);
            Assert.NotSame(TaxRuleSet.Vietnam2025, rules);
            await Assert.ThrowsAsync<InvalidOperationException>(() => store.LoadOfficialAsync());
            Assert.Equal(2, await context.Set<TaxRulePeriod>().CountAsync());
        }
    }

    [Fact]
    public async Task SeedDetectsDriftWithoutOverwriting()
    {
        using var database = new SqliteFileDatabase();
        await using (var context = database.Open())
        {
            await context.Database.MigrateAsync();
            await new TaxRuleStore(context).SeedReferencesAsync();
            var snapshot = await context.Set<TaxRuleSnapshot>().FirstAsync();
            snapshot.PersonalDeduction += 1;
            await context.SaveChangesAsync();
        }
        await using var fresh = database.Open();
        await Assert.ThrowsAsync<InvalidOperationException>(() => new TaxRuleStore(fresh).SeedReferencesAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => new TaxRuleStore(fresh).LoadReferencesAsync());
    }

    [Fact]
    public async Task DatabaseRejectsOverlappingInsertAndUpdate()
    {
        using var database = new SqliteFileDatabase();
        await using var context = database.Open();
        await context.Database.MigrateAsync();
        await new TaxRuleStore(context).SeedReferencesAsync();
        await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(() => context.Database.ExecuteSqlRawAsync(
            "INSERT INTO TaxRulePeriod (Key, RuleId, Version, PeriodFrom, PeriodTo) SELECT 'other', RuleId, Version, '2025-02-01', '2025-02-28' FROM TaxRulePeriod LIMIT 1"));
        await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(() => context.Database.ExecuteSqlRawAsync(
            "UPDATE TaxRulePeriod SET PeriodFrom = '2025-12-01' WHERE PeriodFrom = '2026-01-01'"));
    }
    [Fact]
    public async Task OfficialImportAndLoadUsesVerifiedDatabaseValues()
    {
        using var database = new SqliteFileDatabase();
        var basis = new LegalBasis(VerificationStatus.Verified, "synthetic", "1",
            sourceUrl: "https://vbpl.vn/Pages/vbpq-toanvan.aspx?ItemID=123",
            verifiedOn: new DateOnly(2026, 1, 1));
        var rules = new TaxRuleSet("test", "1", "monthly", 2026, 123m, 0m,
            [new Bracket(null, 0.1m, basis)], basis, basis, basis, basis);
        await using (var context = database.Open())
        {
            await context.Database.MigrateAsync();
            await new TaxRuleStore(context).ImportOfficialAsync(
                [new TaxRuleRegistration(rules, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31))]);
        }
        await using var fresh = database.Open();
        var loaded = (await new TaxRuleStore(fresh).LoadOfficialAsync()).GetForMonth(2026, 3);
        Assert.Equal(123m, loaded.PersonalDeduction);
        Assert.Equal(87.7m, MonthlySalaryCalculator.Calculate(1000m, 0, 0, loaded).Pit.TotalTax.Value);
        Assert.Equal(basis, loaded.TaxBasis);
        await Assert.ThrowsAsync<InvalidOperationException>(() => new TaxRuleStore(fresh).LoadReferencesAsync());
    }

    [Fact]
    public async Task FailedBatchDoesNotPersistEarlierSnapshot()
    {
        using var database = new SqliteFileDatabase();
        await using var context = database.Open();
        await context.Database.MigrateAsync();
        await new TaxRuleStore(context).SeedReferencesAsync();
        var basis = new LegalBasis(VerificationStatus.Verified, "synthetic", "1",
            sourceUrl: "https://vbpl.vn/Pages/vbpq-toanvan.aspx?ItemID=123",
            verifiedOn: new DateOnly(2026, 1, 1));
        var rule = new TaxRuleSet("official", "1", "monthly", 2024, 0m, 0m,
            [new Bracket(null, 0.1m, basis)], basis, basis, basis, basis);
        await Assert.ThrowsAnyAsync<Exception>(() => new TaxRuleStore(context).ImportOfficialAsync(
            [new TaxRuleRegistration(rule, new DateOnly(2024, 1, 1), new DateOnly(2025, 1, 31))]));
        await using var fresh = database.Open();
        Assert.Equal(2, await fresh.Set<TaxRuleSnapshot>().CountAsync());
    }
    [Fact]
    public async Task DecimalRoundTripsExactlyAsTextAndInvalidDatesFail()
    {
        using var database = new SqliteFileDatabase();
        var basis = new LegalBasis(VerificationStatus.Verified, "synthetic", "1",
            sourceUrl: "https://vbpl.vn/Pages/vbpq-toanvan.aspx?ItemID=1",
            verifiedOn: new DateOnly(2026, 1, 1));
        var amount = 0.1234567890123456789012345678m;
        var rules = new TaxRuleSet("precision", "1", "monthly", 2026, amount, decimal.MaxValue,
            [new Bracket(amount, amount, basis), new Bracket(null, 1m, basis)], basis, basis, basis, basis);
        await using (var context = database.Open())
        {
            await context.Database.MigrateAsync();
            await new TaxRuleStore(context).ImportOfficialAsync(
                [new TaxRuleRegistration(rules, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31))]);
            var connection = context.Database.GetDbConnection();
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT typeof(PersonalDeduction) FROM TaxRuleSnapshot";
            Assert.Equal("text", await command.ExecuteScalarAsync());
            await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(() => context.Database.ExecuteSqlRawAsync(
                "UPDATE TaxRulePeriod SET PeriodTo = '2026-02-30'"));
            await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(() => context.Database.ExecuteSqlRawAsync(
                "UPDATE TaxRulePeriod SET PeriodTo = '2026-02-27'"));
        }
        await using var fresh = database.Open();
        var actual = (await new TaxRuleStore(fresh).LoadOfficialAsync()).GetForMonth(2026, 1);
        Assert.Equal(amount, actual.PersonalDeduction);
        Assert.Equal(decimal.MaxValue, actual.DependentDeduction);
        Assert.Equal(amount, actual.Brackets[0].UpperBound);
        Assert.Equal(amount, actual.Brackets[0].Rate);
    }

    [Fact]
    public async Task MissingBracketIsRejectedByReferenceLoader()
    {
        using var database = new SqliteFileDatabase();
        await using (var context = database.Open())
        {
            await context.Database.MigrateAsync();
            await new TaxRuleStore(context).SeedReferencesAsync();
            await context.Database.ExecuteSqlRawAsync("DELETE FROM TaxRuleBracket WHERE Ordinal = 1 AND RuleId = 'vietnam-2025-reference'");
        }
        await using var fresh = database.Open();
        await Assert.ThrowsAsync<InvalidOperationException>(() => new TaxRuleStore(fresh).LoadReferencesAsync());
    }
    [Fact]
    public async Task SplitRegistrationsShareOneSnapshotAndGapsDoNotFallback()
    {
        using var database = new SqliteFileDatabase();
        var basis = new LegalBasis(VerificationStatus.Verified, "synthetic", "1",
            sourceUrl: "https://vbpl.vn/Pages/vbpq-chitiet.aspx?ItemID=7",
            verifiedOn: new DateOnly(2026, 1, 1));
        var rules = new TaxRuleSet("split", "1", "monthly", 2026, 0m, 0m,
            [new Bracket(null, 0.1m, basis)], basis, basis, basis, basis);
        await using (var context = database.Open())
        {
            await context.Database.MigrateAsync();
            await new TaxRuleStore(context).ImportOfficialAsync(
            [
                new TaxRuleRegistration(rules, new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31)),
                new TaxRuleRegistration(rules, new DateOnly(2026, 5, 1), new DateOnly(2026, 12, 31))
            ]);
        }
        await using var fresh = database.Open();
        var provider = await new TaxRuleStore(fresh).LoadOfficialAsync();
        Assert.Same(provider.GetForMonth(2026, 3), provider.GetForMonth(2026, 5));
        Assert.Throws<NotSupportedException>(() => provider.GetForMonth(2026, 4));
    }

    [Fact]
    public async Task CorruptionIsNotIgnoredDuringReferenceSeed()
    {
        using var database = new SqliteFileDatabase();
        await using (var context = database.Open())
        {
            await context.Database.MigrateAsync();
            await new TaxRuleStore(context).SeedReferencesAsync();
            await context.Database.ExecuteSqlRawAsync("DELETE FROM TaxRuleBracket WHERE Ordinal = 1 AND RuleId = 'vietnam-2025-reference'");
        }
        await using var fresh = database.Open();
        await Assert.ThrowsAnyAsync<Exception>(() => new TaxRuleStore(fresh).SeedReferencesAsync());
    }
    [Fact]
    public async Task RelationalKeysAndDateBoundariesAreEnforced()
    {
        using var database = new SqliteFileDatabase();
        await using var context = database.Open();
        await context.Database.MigrateAsync();
        await new TaxRuleStore(context).SeedReferencesAsync();
        await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(() => context.Database.ExecuteSqlRawAsync(
            "INSERT INTO TaxRuleBracket (RuleId, Version, Ordinal, UpperBound, Rate, BasisKey) SELECT RuleId, Version, Ordinal, UpperBound, Rate, BasisKey FROM TaxRuleBracket LIMIT 1"));
        await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(() => context.Database.ExecuteSqlRawAsync(
            "INSERT INTO TaxRulePeriod (Key, RuleId, Version, PeriodFrom, PeriodTo) VALUES ('missing', 'missing', '1', '2024-01-01', '2024-12-31')"));
        await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(() => context.Database.ExecuteSqlRawAsync(
            "UPDATE TaxRuleSnapshot SET FromYear = 10000"));
        await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(() => context.Database.ExecuteSqlRawAsync(
            "UPDATE TaxRulePeriod SET PeriodFrom = '0000-01-01' WHERE PeriodFrom = '2025-01-01'"));
        Assert.Equal(1, await context.Database.ExecuteSqlRawAsync(
            "UPDATE TaxRulePeriod SET PeriodFrom = PeriodFrom WHERE PeriodFrom = '2025-01-01'"));
    }

    [Fact]
    public async Task EntityValidationRunsForSyncAndAsyncSaves()
    {
        using var database = new SqliteFileDatabase();
        await using var context = database.Open();
        await context.Database.MigrateAsync();
        var bracket = new TaxRuleBracket { RuleId = "x", Version = "1", Ordinal = 0, Rate = 1.1m };
        context.Add(bracket);
        Assert.Throws<ArgumentException>(() => context.SaveChanges());
        await Assert.ThrowsAsync<ArgumentException>(() => context.SaveChangesAsync());
    }
}
