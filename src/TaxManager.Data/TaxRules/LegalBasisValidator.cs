using System.Globalization;
using TaxManager.Core;

namespace TaxManager.Data.TaxRules;

/// <summary>Checks the technical source requirements for an official tax rule.</summary>
public static class LegalBasisValidator
{
    public static void ValidateOfficial(LegalBasis basis)
    {
        ArgumentNullException.ThrowIfNull(basis);
        if (basis.Status != VerificationStatus.Verified ||
            string.IsNullOrWhiteSpace(basis.Id) ||
            string.IsNullOrWhiteSpace(basis.Version) ||
            basis.VerifiedOn is null ||
            !Uri.TryCreate(basis.SourceUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            !(uri.Host.Equals("vbpl.vn", StringComparison.OrdinalIgnoreCase) ||
              uri.Host.EndsWith(".vbpl.vn", StringComparison.OrdinalIgnoreCase)) ||
            !IsDocumentPath(uri.AbsolutePath) ||
            !HasSingleItemId(uri.Query))
        {
            throw new ArgumentException("Official legal basis requires a verified vbpl.vn document detail URL with a positive ItemID, ID, version and verification date.", nameof(basis));
        }
    }

    private static bool IsDocumentPath(string path)
    {
        return path.EndsWith("/Pages/vbpq-toanvan.aspx", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith("/Pages/vbpq-chitiet.aspx", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasSingleItemId(string query)
    {
        var values = query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .Where(parts => Uri.UnescapeDataString(parts[0]).Equals("ItemID", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return values.Length == 1 && values[0].Length == 2 &&
               long.TryParse(Uri.UnescapeDataString(values[0][1]), NumberStyles.None,
                   CultureInfo.InvariantCulture, out var id) && id > 0;
    }
}
