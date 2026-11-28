using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TaxManager.Web.Accounts;

namespace TaxManager.Web.Pages.Accounts;

public sealed class IndexModel(SignInManager<LocalUser> signIn, LocalDataUse dataUse) : PageModel
{
    public DataUseAcceptance? Acceptance { get; private set; }
    public async Task OnGetAsync() => Acceptance = await dataUse.ReadAsync(User);
    public async Task<IActionResult> OnPostLogoutAsync()
    {
        await signIn.SignOutAsync();
        return RedirectToPage("/Accounts/Login");
    }
}
