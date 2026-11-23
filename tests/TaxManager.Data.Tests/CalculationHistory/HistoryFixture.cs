using Microsoft.EntityFrameworkCore;
using TaxManager.Core;
using TaxManager.Data.CalculationHistory;
using TaxManager.Data.Infrastructure;
using TaxManager.Data.TaxRules;
using TaxManager.Data.Tests.Fixtures;

namespace TaxManager.Data.Tests.CalculationHistory;

internal static class HistoryFixture
{
    internal static readonly DateTimeOffset Now = new(2026, 11, 23, 7, 8, 9, TimeSpan.Zero);

    internal static async Task<Guid> SeedAsync(SqliteFileDatabase db, bool official = false)
    {
        var id = await SyntheticGraph.SeedAsync(db);
        await using var c = db.Open();
        if (official)
            await new TaxRuleStore(c).ImportOfficialAsync([new(Rules(), new(2026, 1, 1), new(2026, 12, 31))]);
        else await new TaxRuleStore(c).SeedReferencesAsync();
        return id;
    }

    internal static TaxRuleSet Rules(decimal personal = 123.12345678901234567890123456m) => new("synthetic-history", "v7",
        "synthetic-period", 2020, personal, 456.1m,
        [new(10000.000000000000000000000001m, .1234567890123456789012345678m, Basis("bracket-0")),
            new(null, .25m, Basis("bracket-1"))], Basis("personal"), Basis("dependent"), Basis("income"), Basis("tax"));

    internal static LegalBasis Basis(string role) => new(VerificationStatus.Verified, "synthetic-" + role, "basis-v3",
        point: "p", clause: "c", article: "a", documentNumber: "synthetic-doc", summary: "SYNTHETIC_PROVENANCE_82974_" + role,
        sourceUrl: "https://vbpl.vn/Pages/vbpq-toanvan.aspx?ItemID=123", period: "legal-period",
        effectiveFrom: new(2026, 7, 1), effectiveTo: new(2027, 6, 30), verifiedOn: new(2026, 11, 20));

    internal static async Task<CalculationSnapshot> CreateAsync(SqliteFileDatabase db, Guid id,
        CalculationSourceMode mode = CalculationSourceMode.References)
    {
        await using var c = db.Open();
        c.Clock = new HistoryClock();
        return await new CalculationHistoryService(c).CreateMonthlyAsync(id, 2026, 1, mode);
    }

    internal static TaxManagerDbContext Open(SqliteFileDatabase db, TaxManager.Data.Protection.FileKeyRing? keys,
        Action<string>? log = null)
    {
        var options = new DbContextOptionsBuilder<TaxManagerDbContext>()
            .UseSqlite($"Data Source={db.DatabasePath};Foreign Keys=True;Pooling=False");
        if (log is not null) options.LogTo(log);
        return new(options.Options, keys);
    }
}
