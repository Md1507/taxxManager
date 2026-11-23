using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace TaxManager.Data.Audit;

internal sealed record AuditChange(EntityEntry Entry, JsonElement? Before, JsonElement? After);
