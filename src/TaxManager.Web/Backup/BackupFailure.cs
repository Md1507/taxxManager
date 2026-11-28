using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using TaxManager.Data.Protection;

namespace TaxManager.Web.Backup;

internal static class BackupFailure
{
    // A boundary response deliberately omits filenames, values, key IDs and exception details.
    internal static bool IsExpected(Exception ex) => ex is InvalidDataException or IOException or UnauthorizedAccessException
        or CryptographicException or DataProtectionException or SqliteException or JsonException or InvalidOperationException
        or ArgumentException or KeyNotFoundException or NotSupportedException or OverflowException or FormatException;
}
