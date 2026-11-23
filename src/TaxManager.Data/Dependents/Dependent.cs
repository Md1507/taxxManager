using System.ComponentModel.DataAnnotations.Schema;
using TaxManager.Data.Protection;

namespace TaxManager.Data.Dependents;

public sealed class Dependent
{
    internal readonly ProtectedValue<string> Name = new(null);
    public Guid ProfileId { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();
    [NotMapped] public string? DisplayName { get => Name.Value; set => Name.Value = value; }
    public byte[] DisplayNameEncrypted { get => Name.Cipher; internal set => Name.Cipher = value; }
}
