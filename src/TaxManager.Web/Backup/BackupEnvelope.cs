using System.Buffers.Binary;
using System.Security.Cryptography;

namespace TaxManager.Web.Backup;

/// <summary>Bounded, versioned password envelope. No paths or archive extraction.</summary>
public static class BackupEnvelope
{
    public const int MaxDatabaseBytes = 16 * 1024 * 1024;
    public const int MaxKeyBytes = 16 * 1024;
    public const int MaxFileBytes = 17 * 1024 * 1024;
    private const int HeaderSize = 36;
    private static ReadOnlySpan<byte> Magic => "TAXBK001"u8;

    public static bool ValidPassword(string? password) => password is { Length: >= 12 and <= 128 };

    public static byte[] Seal(ReadOnlySpan<byte> database, ReadOnlySpan<byte> keys, string password, DateTimeOffset created)
    {
        CheckPassword(password);
        if (database.Length is <= 0 or > MaxDatabaseBytes || keys.Length is <= 0 or > MaxKeyBytes)
            throw new InvalidDataException("Backup size is unsupported.");
        var plain = new byte[16 + keys.Length + database.Length];
        var result = new byte[HeaderSize + plain.Length + 16];
        Magic.CopyTo(result);
        RandomNumberGenerator.Fill(result.AsSpan(8, 16));
        RandomNumberGenerator.Fill(result.AsSpan(24, 12));
        var key = Derive(password, result.AsSpan(8, 16));
        try
        {
            BinaryPrimitives.WriteInt64LittleEndian(plain, created.UtcTicks);
            BinaryPrimitives.WriteInt32LittleEndian(plain.AsSpan(8), keys.Length);
            BinaryPrimitives.WriteInt32LittleEndian(plain.AsSpan(12), database.Length);
            keys.CopyTo(plain.AsSpan(16));
            database.CopyTo(plain.AsSpan(16 + keys.Length));
            using var aes = new AesGcm(key, 16);
            aes.Encrypt(result.AsSpan(24, 12), plain, result.AsSpan(HeaderSize, plain.Length), result.AsSpan(HeaderSize + plain.Length, 16), result.AsSpan(0, HeaderSize));
            return result;
        }
        finally { CryptographicOperations.ZeroMemory(key); CryptographicOperations.ZeroMemory(plain); }
    }

    public static BackupContents Open(ReadOnlySpan<byte> file, string password)
    {
        CheckPassword(password);
        if (file.Length < HeaderSize + 33 || file.Length > MaxFileBytes || !file[..8].SequenceEqual(Magic))
            throw new InvalidDataException("Unsupported backup envelope.");
        var plain = new byte[file.Length - HeaderSize - 16];
        var key = Derive(password, file.Slice(8, 16));
        try
        {
            using var aes = new AesGcm(key, 16);
            aes.Decrypt(file.Slice(24, 12), file.Slice(HeaderSize, plain.Length), file[^16..], plain, file[..HeaderSize]);
            var keySize = BinaryPrimitives.ReadInt32LittleEndian(plain.AsSpan(8));
            var dbSize = BinaryPrimitives.ReadInt32LittleEndian(plain.AsSpan(12));
            if (keySize is <= 0 or > MaxKeyBytes || dbSize is <= 0 or > MaxDatabaseBytes || (long)keySize + dbSize + 16 != plain.Length)
                throw new InvalidDataException("Invalid backup members.");
            var created = new DateTimeOffset(BinaryPrimitives.ReadInt64LittleEndian(plain), TimeSpan.Zero);
            return new(created, plain.AsSpan(16, keySize).ToArray(), plain.AsSpan(16 + keySize, dbSize).ToArray());
        }
        finally { CryptographicOperations.ZeroMemory(key); CryptographicOperations.ZeroMemory(plain); }
    }

    private static byte[] Derive(string password, ReadOnlySpan<byte> salt) =>
        Rfc2898DeriveBytes.Pbkdf2(password.AsSpan(), salt, 600_000, HashAlgorithmName.SHA256, 32);
    private static void CheckPassword(string password)
    {
        if (!ValidPassword(password)) throw new InvalidDataException("Password must contain 12–128 characters.");
    }
}

public sealed record BackupContents(DateTimeOffset CreatedAt, byte[] Keys, byte[] Database) : IDisposable
{
    public void Dispose() { CryptographicOperations.ZeroMemory(Keys); CryptographicOperations.ZeroMemory(Database); }
}
