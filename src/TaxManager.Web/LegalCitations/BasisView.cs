using TaxManager.Core;
using TaxManager.Data.CalculationHistory;

namespace TaxManager.Web.LegalCitations;

public sealed record BasisView(string Label, CalculationSnapshot Snapshot, LegalBasis Basis)
{
    public Uri? SafeUrl => SafeSourceUrl(Basis);

    public static Uri? SafeSourceUrl(LegalBasis basis) => Uri.TryCreate(basis.SourceUrl, UriKind.Absolute, out var uri) &&
        uri.Scheme == Uri.UriSchemeHttps && string.IsNullOrEmpty(uri.UserInfo) &&
        !uri.IsLoopback && uri.HostNameType == UriHostNameType.Dns ? uri : null;
}
