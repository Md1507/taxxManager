using System.Text.Json;
using TaxManager.Data.TaxRules;

namespace TaxManager.Data.Protection;

// Plaintext is never mapped by EF. Replacing ciphertext (including Reload) invalidates the cache.
internal sealed class ProtectedValue<T>
{
    private T? value;
    private bool initialized;
    private byte[] cipher = [];
    private Func<byte[], byte[]>? decrypt;
    private Action? changed;

    internal ProtectedValue() { }
    internal ProtectedValue(T? initial) { value = initial; initialized = true; }
    internal byte[] Cipher
    {
        get => cipher;
        set { cipher = value ?? []; initialized = false; this.value = default; }
    }
    internal bool Initialized => initialized || cipher.Length > 0;
    internal T? Value
    {
        get
        {
            if (!initialized)
            {
                if (decrypt is null || cipher.Length == 0)
                    throw new DataProtectionException("Protected field is missing or has not been hydrated.");
                try
                {
                    var bytes = decrypt(cipher);
                    if (typeof(T) == typeof(decimal))
                    {
                        using var json = JsonDocument.Parse(bytes);
                        if (json.RootElement.ValueKind != JsonValueKind.Number)
                            throw new JsonException();
                        value = (T)(object)TaxRuleStorageValidator.ParseExactDecimal(json.RootElement.GetRawText());
                    }
                    else value = JsonSerializer.Deserialize<T>(bytes);
                }
                catch (Exception e) when (e is JsonException or InvalidOperationException or FormatException or OverflowException)
                { throw new DataProtectionException("Protected field has an invalid payload."); }
                initialized = true;
            }
            return value;
        }
        set { this.value = value; initialized = true; changed?.Invoke(); }
    }
    internal void Bind(Func<byte[], byte[]> decoder, Action notify)
    { decrypt = decoder; changed = notify; }
    internal void Invalidate() { if (cipher.Length > 0) { initialized = false; value = default; } }
}
