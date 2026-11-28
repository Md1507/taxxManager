using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;

namespace TaxManager.Web.Accounts;

// Passwordless accounts confirm sensitive actions with their existing Google credential.
// The external ticket binds both the current user and the requested operation.
public sealed class GoogleAccountConfirmation(GoogleSignIn google, UserManager<LocalUser> users,
    SignInManager<LocalUser> signIn, IHttpContextAccessor accessor, GoogleConfirmationTickets tickets)
{
    private const string TicketKey = "taxmanager.google.confirmation";
    public async Task<bool> IsConfirmedAsync(LocalUser user, string purpose)
    {
        if (!google.Enabled || await users.IsLockedOutAsync(user)) return false;
        var info = await signIn.GetExternalLoginInfoAsync(user.Id);
        return info?.LoginProvider == GoogleSignIn.Scheme &&
            info.AuthenticationProperties?.Items.TryGetValue(GoogleSignIn.FlowKey, out var flow) == true &&
            flow == "confirm-" + purpose &&
            info.AuthenticationProperties.Items.TryGetValue(TicketKey, out var ticket) && tickets.Matches(user.Id, purpose, ticket) &&
            (await users.FindByLoginAsync(info.LoginProvider, info.ProviderKey))?.Id == user.Id;
    }

    public async Task<ChallengeResult> StartAsync(LocalUser user, string purpose, IUrlHelper url)
    {
        if (!google.Enabled || purpose is not ("export" or "delete" or "email"))
            throw new InvalidOperationException("Google confirmation is unavailable.");
        await accessor.HttpContext!.SignOutAsync(IdentityConstants.ExternalScheme);
        var properties = signIn.ConfigureExternalAuthenticationProperties(GoogleSignIn.Scheme,
            url.Page("/Accounts/GoogleResult")!, user.Id);
        properties.Items[GoogleSignIn.FlowKey] = "confirm-" + purpose;
        properties.Items[TicketKey] = tickets.Issue(user.Id, purpose);
        properties.SetParameter("prompt", "select_account");
        return new ChallengeResult(GoogleSignIn.Scheme, properties);
    }

    public Task ConsumeAsync(LocalUser user, string purpose)
    {
        tickets.Consume(user.Id, purpose);
        return accessor.HttpContext!.SignOutAsync(IdentityConstants.ExternalScheme);
    }
}

// Requests already hold the account gate. Consuming server state also rejects a saved-cookie replay.
public sealed class GoogleConfirmationTickets(TimeProvider clock)
{
    private readonly ConcurrentDictionary<(string User, string Purpose), (string Nonce, DateTimeOffset Expires)> pending = new();
    public string Issue(string user, string purpose)
    {
        var nonce = Guid.NewGuid().ToString("N");
        pending[(user, purpose)] = (nonce, clock.GetUtcNow().AddMinutes(5));
        return nonce;
    }
    public bool Matches(string user, string purpose, string? nonce) => pending.TryGetValue((user, purpose), out var ticket) &&
        ticket.Nonce == nonce && ticket.Expires > clock.GetUtcNow();
    public void Consume(string user, string purpose) => pending.TryRemove((user, purpose), out _);
}
