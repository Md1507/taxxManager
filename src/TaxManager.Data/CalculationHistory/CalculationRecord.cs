using System.ComponentModel.DataAnnotations.Schema;

namespace TaxManager.Data.CalculationHistory;

/// <summary>Application append-only; ProfileId is an intentional retention relation without a cascade FK.</summary>
public sealed class CalculationRecord
{
    public Guid Id { get; private set; }
    public Guid ProfileId { get; private set; }
    public int Year { get; private set; }
    public int Month { get; private set; }
    public int SchemaVersion { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    private byte[] payload = [];
    public byte[] PayloadEncrypted { get => payload.ToArray(); private set { payload = value.ToArray(); pending = null; } }
    private CalculationSnapshot? pending;
    private Func<CalculationSnapshot>? read;
    [NotMapped] public CalculationSnapshot Snapshot => pending ?? read?.Invoke()
        ?? throw new InvalidOperationException("History must be read through a protected context.");

    private CalculationRecord() { }
    internal CalculationRecord(CalculationSnapshot snapshot)
    {
        Id = snapshot.HistoryId; ProfileId = snapshot.ProfileId; Year = snapshot.Year; Month = snapshot.Month;
        SchemaVersion = snapshot.SchemaVersion; CreatedAtUtc = snapshot.CreatedAtUtc; pending = snapshot;
    }
    internal void SetPayload(byte[] bytes) => payload = bytes.ToArray();
    internal void Bind(Func<CalculationSnapshot> reader) => read = reader;
    internal void AcceptPayload() => pending = null;
}
