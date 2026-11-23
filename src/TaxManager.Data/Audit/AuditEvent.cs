namespace TaxManager.Data.Audit;

public sealed class AuditEvent
{
    public Guid Id { get; internal set; }
    public Guid OperationId { get; internal set; }
    public int Ordinal { get; internal set; }
    public Guid ProfileId { get; internal set; }
    public string Entity { get; internal set; } = "";
    public string EntityKey { get; internal set; } = "";
    public string Action { get; internal set; } = "";
    public DateTimeOffset TimestampUtc { get; internal set; }
    public byte[] Payload { get; internal set; } = [];
}
