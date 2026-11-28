using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using TaxManager.Web.Accounts;

namespace TaxManager.Web.Pages.Accounts;

[AllowAnonymous, EnableRateLimiting("account-entry")]
public sealed class GoogleEntryModel(GoogleSignIn google, SignInManager<LocalUser> signIn) : PageModel
{
    public IActionResult OnGet() => NotFound();
    public async Task<IActionResult> OnPostAsync(string? returnUrl)
    {
        if (!google.Enabled) return NotFound();
        if (User.Identity?.IsAuthenticated == true) return RedirectToPage("/Accounts/Index");
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        var properties = signIn.ConfigureExternalAuthenticationProperties(GoogleSignIn.Scheme,
            Url.Page("/Accounts/GoogleResult", new { returnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null })!);
        properties.Items[GoogleSignIn.FlowKey] = "login";
        properties.SetParameter("prompt", "select_account");
        return new ChallengeResult(GoogleSignIn.Scheme, properties);
    }
}
