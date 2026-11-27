using Microsoft.EntityFrameworkCore;
using TaxManager.Data.Infrastructure;
using TaxManager.Data.Protection;
using TaxManager.Data.Audit;

namespace TaxManager.Data.Forecasting;

public sealed class ForecastStore(TaxManagerDbContext context)
{
    public async Task<IReadOnlyList<ForecastDocument>> ListAsync(CancellationToken token = default)
    {
        // An untouched database has no sentinel until its first protected write.
        if (!await context.Set<DataProtectionMetadata>().AsNoTracking().AnyAsync(token) &&
            !await context.Set<AuditEvent>().AsNoTracking().AnyAsync(token) &&
            !await context.UserProfiles.AsNoTracking().AnyAsync(token) &&
            !await context.MonthlyIncomes.AsNoTracking().AnyAsync(token) &&
            !await context.Dependents.AsNoTracking().AnyAsync(token) &&
            !await context.DependentEligibilityPeriods.AsNoTracking().AnyAsync(token) &&
            !await context.CalculationHistory.AsNoTracking().AnyAsync(token) &&
            !await context.Set<ForecastDocument>().AsNoTracking().AnyAsync(token) &&
            !await context.Set<ForecastSnapshot>().AsNoTracking().AnyAsync(token) &&
            !(context.Database.GetAppliedMigrations().Any(m => m.EndsWith("_AddAnnualForecastResults", StringComparison.Ordinal)) &&
              await context.Set<AnnualCalculationRecord>().AsNoTracking().AnyAsync(token))) return [];
        context.ValidateProtection();
        return await context.Set<ForecastDocument>().AsNoTracking().OrderBy(x => x.Id).ToListAsync(token);
    }
    public async Task<Guid> CreateAsync(string payload, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);
        var document = new ForecastDocument { Payload = payload };
        context.Set<ForecastDocument>().Add(document);
        context.Set<ForecastSnapshot>().Add(new ForecastSnapshot { DocumentId = document.Id, Revision = 1, Payload = payload });
        await context.SaveChangesAsync(token);
        return document.Id;
    }

    public async Task<ForecastDocument?> ReadAsync(Guid id, CancellationToken token = default)
    {
        context.ValidateProtection();
        return await context.Set<ForecastDocument>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, token);
    }

    public async Task UpdateAsync(Guid id, int revision, string payload, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);
        context.ValidateProtection();
        var document = await context.Set<ForecastDocument>().SingleOrDefaultAsync(x => x.Id == id, token);
        if (document is null || document.Revision != revision)
            throw new InvalidOperationException("Hồ sơ đã thay đổi hoặc đã bị xóa; hãy mở lại trước khi lưu.");
        document.Payload = payload;
        document.Revision = checked(revision + 1);
        context.Set<ForecastSnapshot>().Add(new ForecastSnapshot { DocumentId = id, Revision = document.Revision, Payload = payload });
        try { await context.SaveChangesAsync(token); }
        catch (DbUpdateConcurrencyException ex)
        { throw new InvalidOperationException("Hồ sơ đã thay đổi; hãy mở lại trước khi lưu.", ex); }
    }

    public async Task<IReadOnlyList<ForecastSnapshot>> HistoryAsync(Guid id, CancellationToken token = default)
    {
        context.ValidateProtection();
        return await context.Set<ForecastSnapshot>().AsNoTracking().Where(x => x.DocumentId == id).OrderBy(x => x.Revision).ToListAsync(token);
    }

    public async Task DeleteAsync(Guid id, int revision, CancellationToken token = default)
    {
        context.ValidateProtection();
        var document = await context.Set<ForecastDocument>().SingleOrDefaultAsync(x => x.Id == id, token);
        if (document is null || document.Revision != revision)
            throw new InvalidOperationException("Hồ sơ đã thay đổi hoặc đã bị xóa; hãy mở lại trước khi xóa.");
        context.Remove(document);
        try { await context.SaveChangesAsync(token); }
        catch (DbUpdateConcurrencyException ex)
        { throw new InvalidOperationException("Hồ sơ đã thay đổi; hãy mở lại trước khi xóa.", ex); }
    }
}
