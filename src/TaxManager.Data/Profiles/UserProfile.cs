using System.ComponentModel.DataAnnotations.Schema;
using TaxManager.Data.Protection;

namespace TaxManager.Data.Profiles;

public sealed class UserProfile
{
    private Guid id = Guid.NewGuid();
    internal readonly ProtectedValue<string> Name = new(null);
    public Guid Id
    {
        get => id;
        set => id = value != Guid.Empty ? value : throw new ArgumentException("Profile ID must be nonempty.", nameof(value));
    }
    [NotMapped] public string? DisplayName { get => Name.Value; set => Name.Value = value; }
    public byte[] DisplayNameEncrypted { get => Name.Cipher; internal set => Name.Cipher = value; }
}
