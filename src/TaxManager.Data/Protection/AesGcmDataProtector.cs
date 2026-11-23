using System.Security.Cryptography;
using System.Text;

namespace TaxManager.Data.Protection;

/// <summary>Authenticated encryption using a caller-supplied 256-bit key.</summary>
public sealed class AesGcmDataProtector : IDisposable
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int MaxPayloadSize = 4 * 1024 * 1024;
    private readonly byte[] key;
    private readonly byte[] keyId;
    private bool disposed;

    public AesGcmDataProtector(string keyId, ReadOnlySpan<byte> key)
    {
        if (string.IsNullOrWhiteSpace(keyId) || Encoding.UTF8.GetByteCount(keyId) > 255 || key.Length != 32)
            throw new DataProtectionException("A key identifier and a 256-bit key are required.");
        this.keyId = Encoding.UTF8.GetBytes(keyId);
        this.key = key.ToArray();
    }

    public byte[] Protect(ReadOnlySpan<byte> plaintext, ProtectionContext context)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(context);
        if (plaintext.Length > MaxPayloadSize) throw new DataProtectionException("Protected payload exceeds the supported size.");
        var header = new byte[3 + keyId.Length];
        header[0] = (byte)'T';
        header[1] = 1;
        header[2] = (byte)keyId.Length;
        keyId.CopyTo(header, 3);
        var aad = Combine(header, context.Encode());
        var result = new byte[header.Length + NonceSize + plaintext.Length + TagSize];
        header.CopyTo(result, 0);
        var nonce = result.AsSpan(header.Length, NonceSize);
        RandomNumberGenerator.Fill(nonce);
        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintext, result.AsSpan(header.Length + NonceSize, plaintext.Length),
            result.AsSpan(result.Length - TagSize), aad);
        return result;
    }

    public byte[] Unprotect(ReadOnlySpan<byte> envelope, ProtectionContext context)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(context);
        if (envelope.Length > MaxPayloadSize + 3 + 255 + NonceSize + TagSize ||
            envelope.Length < 3 + NonceSize + TagSize || envelope[0] != (byte)'T' || envelope[1] != 1)
            throw new DataProtectionException("Protected data is invalid or unsupported.");
        var headerLength = 3 + envelope[2];
        if (envelope.Length < headerLength + NonceSize + TagSize ||
            !envelope.Slice(3, envelope[2]).SequenceEqual(keyId))
            throw new DataProtectionException("Protected data cannot be decrypted with the configured key.");
        var aad = Combine(envelope[..headerLength], context.Encode());
        var plaintext = new byte[envelope.Length - headerLength - NonceSize - TagSize];
        try
        {
            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(envelope.Slice(headerLength, NonceSize),
                envelope.Slice(headerLength + NonceSize, plaintext.Length), envelope[^TagSize..], plaintext, aad);
            return plaintext;
        }
        catch (CryptographicException)
        {
            CryptographicOperations.ZeroMemory(plaintext);
            throw new DataProtectionException("Protected data authentication failed.");
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        CryptographicOperations.ZeroMemory(key);
        disposed = true;
    }

    private static byte[] Combine(ReadOnlySpan<byte> header, ReadOnlySpan<byte> context)
    {
        var result = new byte[header.Length + context.Length];
        header.CopyTo(result);
        context.CopyTo(result.AsSpan(header.Length));
        return result;
    }
}
