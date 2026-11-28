using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TaxManager.Web.Accounts;

namespace TaxManager.Web.Pages.Accounts;

public sealed class IndexModel(SignInManager<LocalUser> signIn, LocalDataUse dataUse,
    UserManager<LocalUser> users, EmailRecovery recovery) : PageModel
{
    public DataUseAcceptance? Acceptance { get; private set; }
    public RecoveryEmailState EmailState { get; private set; } = new(null, null);
    public async Task OnGetAsync()
    {
        Acceptance = await dataUse.ReadAsync(User);
        EmailState = await recovery.ReadAsync((await users.GetUserAsync(User))!);
    }
    public async Task<IActionResult> OnPostLogoutAsync()
    {
        await signIn.SignOutAsync();
        return RedirectToPage("/Accounts/Login");
    }
}
