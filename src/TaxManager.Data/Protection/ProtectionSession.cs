using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using TaxManager.Data.Audit;
using TaxManager.Data.Dependents;
using TaxManager.Data.Income;
using TaxManager.Data.Infrastructure;
using TaxManager.Data.Profiles;

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
                context.Set<AuditEvent>().AsNoTracking().Any())
                throw new DataProtectionException("Database protection metadata is missing.");
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

    private ProtectionContext SentinelContext() => new(databaseId, databaseId, "Metadata", "1", "KeyCheck");
    internal ProtectionContext FieldContext(object entity, string field) =>
        new(databaseId, ProfileId(entity), entity.GetType().Name, RowKey(entity), field);

    internal void Bind(object entity)
    {
        if (entity is not (UserProfile or Dependent or MonthlyIncome)) return;
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

    internal static bool IsBusiness(object e) => e is UserProfile or Dependent or MonthlyIncome or DependentEligibilityPeriod;
    internal static Guid ProfileId(object e) => e switch
    {
        UserProfile p => p.Id, Dependent d => d.ProfileId, MonthlyIncome i => i.ProfileId,
        DependentEligibilityPeriod p => p.ProfileId, _ => throw new InvalidOperationException()
    };
    internal static string RowKey(object e) => e switch
    {
        UserProfile p => p.Id.ToString("D"),
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
