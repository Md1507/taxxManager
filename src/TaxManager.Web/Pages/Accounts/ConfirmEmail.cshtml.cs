using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using TaxManager.Web.Accounts;

namespace TaxManager.Web.Pages.Accounts;

[AllowAnonymous, EnableRateLimiting("account-entry")]
public sealed class ConfirmEmailModel(EmailRecoverySettings settings, EmailRecovery recovery, SignInManager<LocalUser> signIn) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? UserId { get; set; }
    [BindProperty(SupportsGet = true)] public string? RequestId { get; set; }
    [BindProperty(SupportsGet = true)] public string? Code { get; set; }
    public bool Ready { get; private set; }
    public async Task<IActionResult> OnGetAsync()
    {
        Response.Headers["Referrer-Policy"] = "no-referrer";
        if (!settings.Enabled) return NotFound();
        Ready = await recovery.CanConfirmAsync(UserId ?? "", RequestId ?? "", Code ?? "");
        return Page();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        Response.Headers["Referrer-Policy"] = "no-referrer";
        if (!settings.Enabled) return NotFound();
        if (await recovery.ConfirmAsync(UserId ?? "", RequestId ?? "", Code ?? ""))
        {
            await signIn.SignOutAsync();
            return RedirectToPage("/Accounts/Login", new { emailConfirmed = true });
        }
        return Page();
    }
}
