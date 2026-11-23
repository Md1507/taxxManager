using System.Text;

namespace TaxManager.Data.Protection;

public sealed record ProtectionContext(Guid DatabaseId, Guid ProfileId, string Entity, string RowKey, string Field)
{
    internal byte[] Encode()
    {
        if (DatabaseId == Guid.Empty || ProfileId == Guid.Empty ||
            string.IsNullOrWhiteSpace(Entity) || string.IsNullOrWhiteSpace(RowKey) || string.IsNullOrWhiteSpace(Field))
            throw new DataProtectionException("Protection context is incomplete.");

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        writer.Write("TaxManager.Data.v1");
        writer.Write(DatabaseId.ToByteArray());
        writer.Write(ProfileId.ToByteArray());
        writer.Write(Entity);
        writer.Write(RowKey);
        writer.Write(Field);
        return stream.ToArray();
    }
}
