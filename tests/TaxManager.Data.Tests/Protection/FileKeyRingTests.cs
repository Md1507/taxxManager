using System.Security.Cryptography;
using System.Text.Json;
using TaxManager.Data.Protection;
using Xunit;

namespace TaxManager.Data.Tests.Protection;

public sealed class FileKeyRingTests
{
    [Fact]
    public void ReopenAndRotationRetainOldCiphertextAndRejectMissingKeys()
    {
        var path = Path.Combine(Path.GetTempPath(), $"taxmanager-key-{Guid.NewGuid():N}.json");
        var first = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var second = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var context = new ProtectionContext(Guid.NewGuid(), Guid.NewGuid(), "Income", "row", "Gross");
        var separate = Path.Combine(Path.GetTempPath(), $"taxmanager-other-{Guid.NewGuid():N}");
        try
        {
            File.WriteAllText(path, JsonSerializer.Serialize(new { activeKeyId = "old", keys = new Dictionary<string, string> { ["old"] = first } }));
            byte[] cipher;
            using (var ring = FileKeyRing.Load(path, separate, separate)) cipher = ring.Protect([1, 2, 3], context);
            File.WriteAllText(path, JsonSerializer.Serialize(new { activeKeyId = "new", keys = new Dictionary<string, string> { ["old"] = first, ["new"] = second } }));
            using (var ring = FileKeyRing.Load(path, separate, separate)) Assert.Equal(new byte[] { 1, 2, 3 }, ring.Unprotect(cipher, context));
            File.WriteAllText(path, JsonSerializer.Serialize(new { activeKeyId = "new", keys = new Dictionary<string, string> { ["new"] = second } }));
            using (var ring = FileKeyRing.Load(path, separate, separate)) Assert.Throws<DataProtectionException>(() => ring.Unprotect(cipher, context));
            Assert.Throws<DataProtectionException>(() => FileKeyRing.Load(path, Path.GetTempPath(), separate));
        }
        finally { File.Delete(path); }
        Assert.Throws<DataProtectionException>(() => FileKeyRing.Load(path, separate, separate));
    }
}
