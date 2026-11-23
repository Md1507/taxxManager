using System.Security.Cryptography;
using System.Text.Json;

namespace TaxManager.Data.Protection;

/// <summary>A read-only, operator-provisioned key ring. This class never creates keys.</summary>
public sealed class FileKeyRing : IDisposable
{
    private readonly Dictionary<string, byte[]> keys;
    private readonly string activeKeyId;
    private bool disposed;

    private FileKeyRing(string activeKeyId, Dictionary<string, byte[]> keys)
    {
        this.activeKeyId = activeKeyId;
        this.keys = keys;
    }

    // JSON format: { "activeKeyId": "id", "keys": { "id": "base64-encoded 32 bytes" } }.
    public static FileKeyRing Load(string path, string repositoryDirectory, string databaseDirectory)
    {
        var keys = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        try
        {
            if (!Path.IsPathFullyQualified(path) || !Path.IsPathFullyQualified(repositoryDirectory) ||
                !Path.IsPathFullyQualified(databaseDirectory))
                throw new DataProtectionException("Key ring and protected directories require absolute paths.");
            var fullPath = Path.GetFullPath(path);
            // Reject links in the key path: lexical path checks alone cannot enforce separation.
            for (FileSystemInfo? part = new FileInfo(fullPath); part is not null; part = part switch
                 { FileInfo file => file.Directory, DirectoryInfo directory => directory.Parent, _ => null })
            {
                if ((part.Attributes & FileAttributes.ReparsePoint) != 0)
                    throw new DataProtectionException("Key ring path must not contain links.");
            }
            foreach (var directory in new[] { repositoryDirectory, databaseDirectory })
            {
                var relative = Path.GetRelativePath(Path.GetFullPath(directory), fullPath);
                if (relative == "." || (!Path.IsPathRooted(relative) && relative != ".." &&
                                         !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)))
                    throw new DataProtectionException("Key ring must be outside protected directories.");
            }

            using var document = JsonDocument.Parse(File.ReadAllBytes(fullPath));
            var root = document.RootElement;
            var active = root.GetProperty("activeKeyId").GetString();
            foreach (var item in root.GetProperty("keys").EnumerateObject())
            {
                if (string.IsNullOrWhiteSpace(item.Name) || System.Text.Encoding.UTF8.GetByteCount(item.Name) > 255 ||
                    keys.ContainsKey(item.Name))
                    throw new DataProtectionException("Key ring contains an invalid or duplicate key identifier.");
                var key = Convert.FromBase64String(item.Value.GetString() ?? "");
                if (key.Length != 32)
                {
                    CryptographicOperations.ZeroMemory(key);
                    throw new DataProtectionException("Key ring contains an invalid key.");
                }
                keys.Add(item.Name, key);
            }
            if (string.IsNullOrWhiteSpace(active) || !keys.ContainsKey(active))
                throw new DataProtectionException("Key ring active key is missing.");
            return new FileKeyRing(active, keys);
        }
        catch (Exception ex) when (ex is not DataProtectionException)
        {
            foreach (var key in keys.Values) CryptographicOperations.ZeroMemory(key);
            throw new DataProtectionException("Key ring could not be loaded or is invalid.");
        }
        catch
        {
            foreach (var key in keys.Values) CryptographicOperations.ZeroMemory(key);
            throw;
        }
    }

    public byte[] Protect(ReadOnlySpan<byte> plaintext, ProtectionContext context)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        using var protector = new AesGcmDataProtector(activeKeyId, keys[activeKeyId]);
        return protector.Protect(plaintext, context);
    }

    public byte[] Unprotect(ReadOnlySpan<byte> envelope, ProtectionContext context)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (envelope.Length < 3 || envelope[0] != (byte)'T' || envelope[1] != 1 ||
            envelope[2] == 0 || envelope.Length < 3 + envelope[2])
            throw new DataProtectionException("Protected data is invalid or unsupported.");
        string id;
        try { id = new System.Text.UTF8Encoding(false, true).GetString(envelope.Slice(3, envelope[2])); }
        catch (System.Text.DecoderFallbackException) { throw new DataProtectionException("Protected data is invalid or unsupported."); }
        if (!keys.TryGetValue(id, out var key))
            throw new DataProtectionException("Protected data cannot be decrypted with the configured key ring.");
        using var protector = new AesGcmDataProtector(id, key);
        return protector.Unprotect(envelope, context);
    }

    public void Dispose()
    {
        if (disposed) return;
        foreach (var key in keys.Values) CryptographicOperations.ZeroMemory(key);
        disposed = true;
    }
}
