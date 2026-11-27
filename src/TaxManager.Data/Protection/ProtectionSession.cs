using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using TaxManager.Data.Audit;
using TaxManager.Data.Dependents;
using TaxManager.Data.Income;
using TaxManager.Data.Infrastructure;
using TaxManager.Data.Profiles;
using TaxManager.Data.CalculationHistory;
using TaxManager.Data.Forecasting;

namespace TaxManager.Data.Protection;

internal sealed class ProtectionSession(TaxManagerDbContext context, FileKeyRing? keys)
{
    private Guid databaseId;

    internal void Ensure(bool initialize = false)
    {
        if (keys is null) throw new DataProtectionException("An external durable key ring is required.");
        // Revalidate on every unit of work/read, rather than treating an in-memory ID as a key check.
        var metadata = context.Set<DataProtectionMetadata>().AsNoTracking().SingleOrDefault();
        if (metadata is null)
        {
            if (!initialize || context.UserProfiles.AsNoTracking().Any() ||
                context.MonthlyIncomes.AsNoTracking().Any() || context.Dependents.AsNoTracking().Any() ||
                context.DependentEligibilityPeriods.AsNoTracking().Any() ||
                context.Set<AuditEvent>().AsNoTracking().Any() || context.CalculationHistory.AsNoTracking().Any() ||
                HasForecastData())
                throw new DataProtectionException("Database protection metadata is missing.");
            if (context.Database.CurrentTransaction is null)
                throw new InvalidOperationException("Protection initialization requires an explicit transaction.");
            databaseId = Guid.NewGuid();
            var check = keys.Protect("TaxManager key check v1"u8, SentinelContext());
            context.Database.ExecuteSqlInterpolated(
                $"INSERT INTO DataProtectionMetadata (Id, DatabaseId, KeyCheck) VALUES (1, {databaseId}, {check})");
        }
        else
        {
            databaseId = metadata.DatabaseId;
            if (!keys.Unprotect(metadata.KeyCheck, SentinelContext()).AsSpan().SequenceEqual("TaxManager key check v1"u8))
                throw new DataProtectionException("Database key check failed.");
        }
    }

    private bool HasForecastData()
    {
        // Older supported schemas do not yet contain the forecast tables.
        var migrations = context.Database.GetAppliedMigrations().ToHashSet(StringComparer.Ordinal);
        return migrations.Contains("20261126101030_AddForecastDocuments") && context.Set<ForecastDocument>().AsNoTracking().Any() ||
            migrations.Contains("20261126101326_AddForecastSnapshots") && context.Set<ForecastSnapshot>().AsNoTracking().Any() ||
            migrations.Any(m => m.EndsWith("_AddAnnualForecastResults", StringComparison.Ordinal)) && context.Set<AnnualCalculationRecord>().AsNoTracking().Any();
    }

    private ProtectionContext SentinelContext() => new(databaseId, databaseId, "Metadata", "1", "KeyCheck");
    internal ProtectionContext FieldContext(object entity, string field) =>
        new(databaseId, ProfileId(entity), entity.GetType().Name, RowKey(entity),
            entity is CalculationRecord h ? JsonSerializer.Serialize(new { Field = field, h.Year, h.Month, h.SchemaVersion, h.CreatedAtUtc }) :
            entity is AnnualCalculationRecord a ? JsonSerializer.Serialize(new { Field = field, a.Year, a.InputRevision, a.SchemaVersion, a.CreatedAtUtc }) : field);

    internal void Bind(object entity)
    {
        if (entity is not (UserProfile or Dependent or MonthlyIncome or CalculationRecord or ForecastDocument or ForecastSnapshot or AnnualCalculationRecord)) return;
        Ensure();
        BindFields(entity);
        _ = Snapshot(entity); // Fail at materialization, including AsNoTracking and client projections.
        if (entity is MonthlyIncome income && (income.GrossIncome < 0 || income.InsuranceAmount < 0 || income.ActualWithheldTax < 0))
            throw new DataProtectionException("Protected income payload is invalid.");
    }

    internal void BindFields(object entity)
    {
        byte[] Decode(byte[] bytes, string field) => keys!.Unprotect(bytes, FieldContext(entity, field));
        switch (entity)
        {
            case AnnualCalculationRecord a: a.Content.Bind(b => Decode(b, "Payload"), () => context.MarkSensitiveDirty(a, "PayloadEncrypted")); break;
            case ForecastDocument f: f.Content.Bind(b => Decode(b, "Payload"), () => context.MarkSensitiveDirty(f, "PayloadEncrypted")); break;
            case ForecastSnapshot f: f.Content.Bind(b => Decode(b, "Payload"), () => context.MarkSensitiveDirty(f, "PayloadEncrypted")); break;
            case CalculationRecord h:
                h.Bind(() =>
                {
                    Ensure();
                    return CalculationSnapshotCodec.Decode(Decode(h.PayloadEncrypted, "Snapshot"), h);
                });
                break;
            case UserProfile p: p.Name.Bind(b => Decode(b, "DisplayName"), () => context.MarkSensitiveDirty(p, "DisplayNameEncrypted")); break;
            case Dependent d: d.Name.Bind(b => Decode(b, "DisplayName"), () => context.MarkSensitiveDirty(d, "DisplayNameEncrypted")); break;
            case MonthlyIncome i:
                i.Gross.Bind(b => Decode(b, "GrossIncome"), () => context.MarkSensitiveDirty(i, "GrossIncomeEncrypted"));
                i.Insurance.Bind(b => Decode(b, "InsuranceAmount"), () => context.MarkSensitiveDirty(i, "InsuranceAmountEncrypted"));
                i.Withheld.Bind(b => Decode(b, "ActualWithheldTax"), () => context.MarkSensitiveDirty(i, "ActualWithheldTaxEncrypted"));
                break;
        }
    }

    internal static void Invalidate(object entity)
    {
        switch (entity)
        {
            case AnnualCalculationRecord a: a.Content.Invalidate(); break;
            case ForecastDocument f: f.Content.Invalidate(); break;
            case ForecastSnapshot f: f.Content.Invalidate(); break;
            case UserProfile p: p.Name.Invalidate(); break;
            case Dependent d: d.Name.Invalidate(); break;
            case MonthlyIncome i: i.Gross.Invalidate(); i.Insurance.Invalidate(); i.Withheld.Invalidate(); break;
        }
    }

    internal void Encrypt(EntityEntry entry)
    {
        var entity = entry.Entity;
        BindFields(entity);
        void Write<T>(string name, T value)
        {
            entry.Property(name + "Encrypted").CurrentValue =
                keys!.Protect(JsonSerializer.SerializeToUtf8Bytes(value), FieldContext(entity, name));
        }
        switch (entity)
        {
            case AnnualCalculationRecord a:
                var annualPayload = a.Payload; Write("Payload", annualPayload); a.Payload = annualPayload; break;
            case ForecastDocument f:
                var payload = f.Payload; Write("Payload", payload); f.Payload = payload; break;
            case ForecastSnapshot f:
                var snapshotPayload = f.Payload; Write("Payload", snapshotPayload); f.Payload = snapshotPayload; break;
            case CalculationRecord h:
                h.SetPayload(keys!.Protect(CalculationSnapshotCodec.Encode(h.Snapshot), FieldContext(h, "Snapshot")));
                h.AcceptPayload();
                break;
            case UserProfile p:
                var profileName = p.DisplayName; Write("DisplayName", profileName); p.DisplayName = profileName; break;
            case Dependent d:
                var dependentName = d.DisplayName; Write("DisplayName", dependentName); d.DisplayName = dependentName; break;
            case MonthlyIncome i:
                // Capture all values before replacing ciphertext invalidates cached plaintext.
                var gross = i.GrossIncome; var insurance = i.InsuranceAmount; var tax = i.ActualWithheldTax;
                Write("GrossIncome", gross); Write("InsuranceAmount", insurance); Write("ActualWithheldTax", tax);
                i.GrossIncome = gross; i.InsuranceAmount = insurance; i.ActualWithheldTax = tax;
                break;
        }
    }

    internal byte[] ProtectAudit(AuditEvent e, AuditPayload payload) =>
        keys!.Protect(JsonSerializer.SerializeToUtf8Bytes(payload), AuditContext(e));

    internal AuditPayload ReadAudit(AuditEvent e)
    {
        Ensure();
        try { return JsonSerializer.Deserialize<AuditPayload>(keys!.Unprotect(e.Payload, AuditContext(e))) ??
                     throw new DataProtectionException("Audit payload is missing."); }
        catch (JsonException) { throw new DataProtectionException("Audit payload is invalid."); }
    }

    private ProtectionContext AuditContext(AuditEvent e) =>
        new(databaseId, e.ProfileId, "AuditEvent", e.Id.ToString("D"),
            JsonSerializer.Serialize(new { e.OperationId, e.Ordinal, e.Entity, e.EntityKey, e.Action, e.TimestampUtc }));

    internal static bool IsBusiness(object e) => e is UserProfile or Dependent or MonthlyIncome or DependentEligibilityPeriod or CalculationRecord or ForecastDocument or ForecastSnapshot or AnnualCalculationRecord;
    internal static Guid ProfileId(object e) => e switch
    {
        AnnualCalculationRecord a => a.DocumentId,
        UserProfile p => p.Id, Dependent d => d.ProfileId, MonthlyIncome i => i.ProfileId,
        DependentEligibilityPeriod p => p.ProfileId, CalculationRecord h => h.ProfileId, ForecastDocument f => f.Id, ForecastSnapshot f => f.DocumentId, _ => throw new InvalidOperationException()
    };
    internal static string RowKey(object e) => e switch
    {
        AnnualCalculationRecord a => a.Id.ToString("D"),
        UserProfile p => p.Id.ToString("D"),
        CalculationRecord h => h.Id.ToString("D"),
        ForecastDocument f => f.Id.ToString("D"),
        ForecastSnapshot f => f.Id.ToString("D"),
        Dependent d => d.Id.ToString("D"),
        MonthlyIncome i => FormattableString.Invariant($"{i.Year:D4}/{i.Month:D2}"),
        DependentEligibilityPeriod p => FormattableString.Invariant($"{p.DependentId:D}/{p.EligibleFromMonth:yyyy-MM-dd}"),
        _ => throw new InvalidOperationException()
    };
    internal static JsonElement Snapshot(object entity)
    {
        static string Money(decimal value) => value.ToString("G29", CultureInfo.InvariantCulture);
        return entity switch
        {
            AnnualCalculationRecord a => JsonSerializer.SerializeToElement(a.Snapshot),
            CalculationRecord h => CalculationSnapshotCodec.AuditSnapshot(h.Snapshot),
            ForecastDocument f => JsonSerializer.SerializeToElement(new { f.Payload, f.Revision }),
            ForecastSnapshot f => JsonSerializer.SerializeToElement(new { f.Payload, f.Revision, f.SchemaVersion }),
            UserProfile p => JsonSerializer.SerializeToElement(new { p.DisplayName }),
            Dependent d => JsonSerializer.SerializeToElement(new { d.DisplayName }),
            MonthlyIncome i => JsonSerializer.SerializeToElement(new
            {
                GrossIncome = Money(i.GrossIncome), InsuranceAmount = Money(i.InsuranceAmount),
                ActualWithheldTax = Money(i.ActualWithheldTax)
            }),
            DependentEligibilityPeriod p => JsonSerializer.SerializeToElement(new { p.EligibleFromMonth, p.EligibleToMonth }),
            _ => throw new InvalidOperationException()
        };
    }
}
