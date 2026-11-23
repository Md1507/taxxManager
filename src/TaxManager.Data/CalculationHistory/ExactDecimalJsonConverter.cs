using System.Text.Json;
using System.Text.Json.Serialization;
using TaxManager.Data.TaxRules;

namespace TaxManager.Data.CalculationHistory;

internal sealed class ExactDecimalJsonConverter : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        if (doc.RootElement.ValueKind != JsonValueKind.Number) throw new JsonException();
        return TaxRuleStorageValidator.ParseExactDecimal(doc.RootElement.GetRawText());
    }
    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options) => writer.WriteNumberValue(value);
}
