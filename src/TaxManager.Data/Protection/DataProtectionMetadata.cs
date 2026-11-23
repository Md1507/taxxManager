namespace TaxManager.Data.Protection;

public sealed class DataProtectionMetadata
{
    public int Id { get; internal set; } = 1;
    public Guid DatabaseId { get; internal set; }
    public byte[] KeyCheck { get; internal set; } = [];
}
