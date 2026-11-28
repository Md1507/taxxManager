using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace TaxManager.Web.Accounts;

public sealed record DataUseAcceptance(string Version, DateTimeOffset AcceptedAt);

/// <summary>Read durable acknowledgements, never the copy embedded in a login cookie.</summary>
public sealed class LocalDataUse(UserManager<LocalUser> users, TimeProvider clock)
{
    public const string Version = "local-demo-2026-11-28";
    public const string DisplayVersion = "28/11/2026";
    public const string ClaimType = "taxmanager.local-data-use";

    public async Task<DataUseAcceptance?> ReadAsync(ClaimsPrincipal principal)
    {
        var user = await users.GetUserAsync(principal);
        return user is null ? null : await ReadAsync(user);
    }

    public async Task<DataUseAcceptance?> ReadAsync(LocalUser user)
    {
        foreach (var claim in await users.GetClaimsAsync(user))
        {
            if (claim.Type != ClaimType) continue;
            var parts = claim.Value.Split('|');
            if (parts.Length == 2 && parts[0] == Version &&
                DateTimeOffset.TryParseExact(parts[1], "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time) &&
                time.Offset == TimeSpan.Zero && time <= clock.GetUtcNow())
                return new(Version, time);
        }
        return null;
    }

    // Caller owns a registry transaction so account creation and acceptance commit together.
    public async Task<IdentityResult> AcceptAsync(LocalUser user)
    {
        if (await ReadAsync(user) is not null) return IdentityResult.Success;
        return await users.AddClaimAsync(user, new Claim(ClaimType,
            Version + "|" + clock.GetUtcNow().ToString("O", CultureInfo.InvariantCulture)));
    }

    public static string Destination(string? returnUrl, IUrlHelper url)
    {
        if (!url.IsLocalUrl(returnUrl)) return "/ho-so";
        var destination = url.Content(returnUrl)!;
        var path = Uri.UnescapeDataString(new Uri(new Uri("https://localhost/"), destination).AbsolutePath);
        return path.StartsWith("/tai-khoan", StringComparison.OrdinalIgnoreCase) ? "/ho-so" : destination;
    }
}
