using System.Text.Json;

namespace TaxManager.Data.Audit;

public sealed record AuditPayload(string Actor, string Context, string? Correlation, JsonElement? Before, JsonElement? After);
