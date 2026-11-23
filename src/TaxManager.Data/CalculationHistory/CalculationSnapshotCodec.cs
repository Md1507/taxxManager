using System.Text.Json;
using System.Text.Json.Serialization;
using TaxManager.Core;
using TaxManager.Data.Protection;

namespace TaxManager.Data.CalculationHistory;

internal static class CalculationSnapshotCodec
{
    private static readonly JsonSerializerOptions Options = new()
    {
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new ExactDecimalJsonConverter() }
    };

    internal static byte[] Encode(CalculationSnapshot snapshot)
    {
        Validate(snapshot);
        return JsonSerializer.SerializeToUtf8Bytes(snapshot, Options);
    }

    internal static CalculationSnapshot Decode(byte[] bytes, CalculationRecord header)
    {
        try
        {
            var snapshot = JsonSerializer.Deserialize<CalculationSnapshot>(bytes, Options)
                ?? throw new JsonException();
            // Includes nullable Core fields with optional constructor arguments. Reject absent,
            // duplicate or unrecognized fields rather than manufacturing defaults on read.
            using var original = JsonDocument.Parse(bytes);
            using var canonical = JsonDocument.Parse(Encode(snapshot));
            CheckShape(original.RootElement, canonical.RootElement);
            if (snapshot.HistoryId != header.Id || snapshot.ProfileId != header.ProfileId ||
                snapshot.Year != header.Year || snapshot.Month != header.Month ||
                snapshot.CreatedAtUtc != header.CreatedAtUtc || snapshot.SchemaVersion != header.SchemaVersion)
                throw new JsonException();
            return snapshot;
        }
        catch (Exception e) when (e is JsonException or ArgumentException or InvalidOperationException or OverflowException)
        {
            // Do not include the JSON parser exception, which can contain source text.
            throw new DataProtectionException("Calculation history payload is invalid or unsupported.");
        }
    }

    private static void CheckShape(JsonElement source, JsonElement canonical)
    {
        if (source.ValueKind != canonical.ValueKind) throw new JsonException();
        if (source.ValueKind == JsonValueKind.Object)
        {
            var properties = source.EnumerateObject().ToArray();
            var expected = canonical.EnumerateObject().ToArray();
            if (properties.Length != expected.Length || properties.Select(p => p.Name).Distinct().Count() != properties.Length)
                throw new JsonException();
            foreach (var p in expected)
            {
                if (!source.TryGetProperty(p.Name, out var value)) throw new JsonException();
                CheckShape(value, p.Value);
            }
        }
        else if (source.ValueKind == JsonValueKind.Array)
        {
            if (source.GetArrayLength() != canonical.GetArrayLength()) throw new JsonException();
            for (var i = 0; i < source.GetArrayLength(); i++) CheckShape(source[i], canonical[i]);
        }
    }

    private static void Validate(CalculationSnapshot s)
    {
        if (s.SchemaVersion != 1 || s.HistoryId == Guid.Empty || s.ProfileId == Guid.Empty ||
            s.Year is < 1 or > 9999 || s.Month is < 1 or > 12 || s.CreatedAtUtc.Offset != TimeSpan.Zero ||
            !Enum.IsDefined(s.SourceMode) || s.Engine.ContractVersion != 1 ||
            string.IsNullOrWhiteSpace(s.Engine.Name) || string.IsNullOrWhiteSpace(s.Engine.AssemblyVersion) ||
            string.IsNullOrWhiteSpace(s.Engine.InformationalVersion) || s.Engine.ModuleVersionId == Guid.Empty)
            throw new JsonException();
        var input = s.Input;
        var month = new DateOnly(s.Year, s.Month, 1);
        if (input.GrossIncome < 0 || input.ManualInsurance < 0 || input.ActualWithheldTax < 0 ||
            input.ExemptIncome != 0 || input.Dependents.IsDefault || input.DependentCount < 0 ||
            input.Dependents.Any(d => d is null || d.DependentId == Guid.Empty || d.FromMonth.Day != 1 ||
                d.ToMonth.Day != 1 || d.FromMonth > month || d.ToMonth < month) ||
            input.Dependents.Select(d => d.DependentId).Distinct().Count() != input.DependentCount)
            throw new JsonException();
        var rules = s.Rules;
        if (rules.Brackets.IsDefaultOrEmpty || rules.Brackets.Any(b => b is null) || rules.FromYear > 9999 ||
            rules.PeriodFrom > month || rules.PeriodTo < month)
            throw new JsonException();
        _ = new TaxRuleRegistration(new TaxRuleSet(rules.RuleId, rules.Version, rules.Period, rules.FromYear,
            rules.PersonalDeduction, rules.DependentDeduction, rules.Brackets,
            rules.PersonalDeductionBasis, rules.DependentDeductionBasis, rules.TaxableIncomeBasis, rules.TaxBasis),
            rules.PeriodFrom, rules.PeriodTo);
        var result = s.Result;
        if (result.Portions.IsDefault || result.Portions.Length != rules.Brackets.Length ||
            result.Portions.Any(p => p is null || p.Basis is null) || result.Dependents != input.DependentCount ||
            result.GrossIncome.Value != input.GrossIncome || result.Insurance.Value != input.ManualInsurance ||
            result.ExemptIncome != input.ExemptIncome)
            throw new JsonException();
    }

    internal static JsonElement AuditSnapshot(CalculationSnapshot snapshot) =>
        JsonSerializer.SerializeToElement(snapshot, Options);
}
