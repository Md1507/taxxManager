namespace TaxManager.Data.Audit;

/// <summary>Caller supplied metadata, not an authenticated identity. Supply a stable OperationId for reconciliation.</summary>
public sealed record AuditWriteContext(string Actor, string Context, string? Correlation = null, Guid? OperationId = null);
