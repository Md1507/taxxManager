using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TaxManager.Core;
using TaxManager.Data.TaxRules;
using TaxManager.Data.Tests.Fixtures;
using Xunit;

namespace TaxManager.Data.Tests.TaxRules;

public class TaxRuleConcurrencyTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task StaleTrackedChangesCannotInvalidateCatalog(bool asyncSave, bool sharedBasis)
    {
        using var database = new SqliteFileDatabase();
        await using (var setup = database.Open())
        {
            await setup.Database.MigrateAsync();
            var basis = new LegalBasis(VerificationStatus.Verified, "synthetic", "1",
                sourceUrl: "https://vbpl.vn/Pages/vbpq-toanvan.aspx?ItemID=1",
                verifiedOn: new DateOnly(2026, 1, 1));
            var rule = new TaxRuleSet("synthetic", "1", "monthly", 2026, 0m, 0m,
                [new Bracket(10m, 0.1m, basis), new Bracket(20m, 0.2m, basis),
                    new Bracket(null, 0.3m, basis)], basis, basis, basis, basis);
            await new TaxRuleStore(setup).ImportOfficialAsync(
                [new(rule, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31))]);
            if (sharedBasis)
            {
                var snapshot = await setup.Set<TaxRuleSnapshot>().SingleAsync();
                var old = snapshot.DependentBasisKey;
                snapshot.DependentBasisKey = snapshot.PersonalBasisKey;
                setup.Remove(await setup.Set<TaxRuleLegalBasis>().SingleAsync(x => x.Key == old));
                await setup.SaveChangesAsync();
            }
        }

        await using var first = database.Open();
        await using var second = database.Open();
        if (sharedBasis)
        {
            var a = await first.Set<TaxRuleSnapshot>().SingleAsync();
            var b = await second.Set<TaxRuleSnapshot>().SingleAsync();
            a.PersonalBasisKey = a.IncomeBasisKey;
            b.DependentBasisKey = b.TaxBasisKey;
        }
        else
        {
            var a = await first.Set<TaxRuleBracket>().OrderBy(x => x.Ordinal).ToListAsync();
            var b = await second.Set<TaxRuleBracket>().OrderBy(x => x.Ordinal).ToListAsync();
            a[0].UpperBound = 15m;
            b[0].Rate = 0.15m;
            b[1].UpperBound = 12m;
        }

        if (asyncSave)
        {
            await first.SaveChangesAsync();
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => second.SaveChangesAsync());
            Assert.Contains("reload", error.Message);
        }
        else
        {
            first.SaveChanges();
            var error = Assert.Throws<InvalidOperationException>(() => second.SaveChanges());
            Assert.Contains("reload", error.Message);
        }

        await using var fresh = database.Open();
        await new TaxRuleStore(fresh).LoadOfficialAsync();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task CompetingWritersCannotInvalidateCatalog(bool asyncSave, bool sharedBasis)
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
            if (sharedBasis)
            {
                var snapshot = await setup.Set<TaxRuleSnapshot>().SingleAsync();
                var old = snapshot.DependentBasisKey;
                snapshot.DependentBasisKey = snapshot.PersonalBasisKey;
                setup.Remove(await setup.Set<TaxRuleLegalBasis>().SingleAsync(x => x.Key == old));
                await setup.SaveChangesAsync();
            }
        }

        await using var second = database.Open();
        ((SqliteConnection)second.Database.GetDbConnection()).DefaultTimeout = 1;
        var interceptor = new InterleaveInterceptor(second, asyncSave);
        await using (var first = database.Open(interceptor))
        {
            if (sharedBasis)
            {
                var firstSnapshot = await first.Set<TaxRuleSnapshot>().SingleAsync();
                var secondSnapshot = await second.Set<TaxRuleSnapshot>().SingleAsync();
                firstSnapshot.PersonalBasisKey = firstSnapshot.IncomeBasisKey;
                secondSnapshot.DependentBasisKey = secondSnapshot.TaxBasisKey;
            }
            else
            {
                var firstPeriods = await first.Set<TaxRulePeriod>().OrderBy(x => x.PeriodFrom).ToListAsync();
                var secondPeriods = await second.Set<TaxRulePeriod>().OrderBy(x => x.PeriodFrom).ToListAsync();
                first.Remove(firstPeriods[0]);
                second.Remove(secondPeriods[1]);
            }

            if (asyncSave) await first.SaveChangesAsync();
            else first.SaveChanges();
        }
        Assert.IsType<SqliteException>(interceptor.Error);
        await using var fresh = database.Open();
        await new TaxRuleStore(fresh).LoadOfficialAsync();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CallerTransactionRemainsOwnedByCaller(bool asyncSave)
    {
        using var database = new SqliteFileDatabase();
        await using var context = database.Open();
        await context.Database.MigrateAsync();
        await new TaxRuleStore(context).SeedReferencesAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var snapshot = await context.Set<TaxRuleSnapshot>().FirstAsync();
        snapshot.PersonalDeduction++;
        if (asyncSave) await context.SaveChangesAsync();
        else context.SaveChanges();
        Assert.Same(transaction, context.Database.CurrentTransaction);
        await transaction.RollbackAsync();
        await using var fresh = database.Open();
        Assert.NotEqual(snapshot.PersonalDeduction,
            (await fresh.Set<TaxRuleSnapshot>().FirstAsync()).PersonalDeduction);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OwnedTransactionReleasesLockAfterValidationFailure(bool asyncSave)
    {
        using var database = new SqliteFileDatabase();
        await using var context = database.Open();
        await context.Database.MigrateAsync();
        await new TaxRuleStore(context).SeedReferencesAsync();
        context.Remove(await context.Set<TaxRulePeriod>().FirstAsync());
        if (asyncSave) await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        else Assert.Throws<InvalidOperationException>(() => context.SaveChanges());
        Assert.Null(context.Database.CurrentTransaction);
        await using var other = database.Open();
        ((SqliteConnection)other.Database.GetDbConnection()).DefaultTimeout = 1;
        var snapshot = await other.Set<TaxRuleSnapshot>().FirstAsync();
        snapshot.PersonalDeduction++;
        await other.SaveChangesAsync();
    }

    [Fact]
    public async Task OwnedTransactionHonorsAcceptAllChangesAndCancellation()
    {
        using var database = new SqliteFileDatabase();
        await using var context = database.Open();
        await context.Database.MigrateAsync();
        await new TaxRuleStore(context).SeedReferencesAsync();
        var snapshot = await context.Set<TaxRuleSnapshot>().FirstAsync();
        snapshot.PersonalDeduction++;
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.SaveChangesAsync(false, cancelled.Token));
        Assert.Null(context.Database.CurrentTransaction);
        await context.SaveChangesAsync(false);
        Assert.Equal(EntityState.Modified, context.Entry(snapshot).State);
        context.ChangeTracker.AcceptAllChanges();
        Assert.Equal(EntityState.Unchanged, context.Entry(snapshot).State);
    }

    private sealed class InterleaveInterceptor(DbContext second, bool asyncSave) : SaveChangesInterceptor
    {
        public Exception? Error { get; private set; }

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData, InterceptionResult<int> result)
        {
            if (!asyncSave)
            {
                try { second.SaveChanges(); }
                catch (Exception error) { Error = error; }
            }
            return result;
        }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (asyncSave)
            {
                try { await second.SaveChangesAsync(cancellationToken); }
                catch (Exception error) { Error = error; }
            }
            return result;
        }
    }
}
