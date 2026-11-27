using System.Text.Json;
using TaxManager.Core.Forecasting;

namespace TaxManager.Data.Forecasting;

public sealed record ForecastAnnualInput(int Year, ForecastMonth[] Months, ForecastBonus[] Bonuses);

/// <summary>Reads the original saved draft without rewriting its bytes or entry timestamps.</summary>
public static class ForecastDraftReader
{
    public static ForecastAnnualInput Read(string payload)
    {
        using var json = JsonDocument.Parse(payload);
        var root = json.RootElement;
        // Explicit normalized input contract for non-Web callers; draft v1 remains readable.
        if (root.TryGetProperty("InputSchemaVersion", out var version))
        {
            if (version.GetInt32() != 1) throw new JsonException("Unsupported forecast input version.");
            return root.GetProperty("Input").Deserialize<ForecastAnnualInput>() ?? throw new JsonException();
        }
        using var stampJson = JsonDocument.Parse(root.GetProperty("Stamps").GetString()!);
        var stamps = stampJson.RootElement;
        ForecastValue<T>? Value<T>(JsonElement item, string field, string status, string source, string note, string key)
        {
            if (!item.TryGetProperty(field, out var value) || value.ValueKind == JsonValueKind.Null) return null;
            var entered = stamps.GetProperty(key).GetProperty("EnteredAt").GetDateTimeOffset();
            return new(value.Deserialize<T>()!, item.GetProperty(status).Deserialize<ForecastStatus>(),
                Text(item, source), Text(item, note), entered);
        }
        var schedule = new SalarySchedule(2026);
        var exceptions = root.GetProperty("Exceptions").EnumerateArray().ToArray();
        if (exceptions.Length != 12 || !exceptions.Select(x => x.GetProperty("Month").GetInt32()).Order().SequenceEqual(Enumerable.Range(1, 12)))
            throw new JsonException("Twelve distinct months are required.");
        foreach (var item in exceptions)
        {
            var month = item.GetProperty("Month").GetInt32();
            var gross = Value<decimal>(item, "Gross", "Status", "Source", "Note", $"{month}/gross");
            if (gross is not null) schedule = schedule.WithException(month, gross);
        }
        var baseGross = Value<decimal>(root, "Gross", "Status", "Source", "Note", "base");
        if (baseGross is not null) schedule = schedule.ApplyPeriod(root.GetProperty("FromMonth").GetInt32(), root.GetProperty("ToMonth").GetInt32(), baseGross);
        var index = 0;
        foreach (var period in root.GetProperty("Periods").EnumerateArray())
        {
            var gross = Value<decimal>(period, "Gross", "Status", "Source", "Note", $"period/{index++}") ?? throw new JsonException("Missing period gross.");
            schedule = schedule.ApplyPeriod(period.GetProperty("FromMonth").GetInt32(), period.GetProperty("ToMonth").GetInt32(), gross);
        }
        var months = exceptions.Select(item =>
        {
            var month = item.GetProperty("Month").GetInt32();
            return new ForecastMonth(month, schedule.Months[month],
                Value<decimal>(item, "Insurance", "InsuranceStatus", "InsuranceSource", "InsuranceNote", $"{month}/insurance"),
                Value<int>(item, "Dependents", "DependentsStatus", "DependentsSource", "DependentsNote", $"{month}/dependents"),
                Value<decimal>(item, "Withheld", "WithheldStatus", "WithheldSource", "WithheldNote", $"{month}/withheld"));
        }).OrderBy(m => m.Month).ToArray();
        var dependentPeriods = new List<ForecastDependentPeriod>();
        index = 0;
        if (root.TryGetProperty("DependentPeriods", out var periods) && periods.ValueKind == JsonValueKind.Array)
            foreach (var period in periods.EnumerateArray())
            {
                var count = Value<int>(period, "Count", "Status", "Source", "Note", $"dependent-period/{index++}");
                if (count is not null) dependentPeriods.Add(new(period.GetProperty("FromMonth").GetInt32(), period.GetProperty("ToMonth").GetInt32(), count));
            }
        months = ForecastDependentPeriod.Apply(months, dependentPeriods).ToArray();
        var bonuses = new List<ForecastBonus>();
        index = 0;
        if (root.TryGetProperty("Bonuses", out var bonusItems) && bonusItems.ValueKind == JsonValueKind.Array)
            foreach (var bonus in bonusItems.EnumerateArray())
            {
                var key = $"bonus/{index++}";
                var amount = Value<decimal>(bonus, "Amount", "Status", "Source", "Note", key);
                if (amount is not null) bonuses.Add(new(key, bonus.GetProperty("Month").GetInt32(), amount, bonus.GetProperty("IncludedInGross").GetBoolean()));
            }
        return new(2026, months, bonuses.ToArray());
    }

    public static string Encode(ForecastAnnualInput input) => JsonSerializer.Serialize(new { InputSchemaVersion = 1, Input = input });
    private static string? Text(JsonElement item, string name) => item.TryGetProperty(name, out var value) && value.ValueKind != JsonValueKind.Null ? value.GetString() : null;
}
