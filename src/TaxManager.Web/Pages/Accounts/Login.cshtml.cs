using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using TaxManager.Web.Accounts;

namespace TaxManager.Web.Pages.Accounts;

[AllowAnonymous, EnableRateLimiting("account-entry")]
public sealed class LoginModel(SignInManager<LocalUser> signIn) : PageModel
{
    [BindProperty, Required(ErrorMessage = "Nhập tên đăng nhập."), StringLength(64, ErrorMessage = "Tên đăng nhập tối đa 64 ký tự.")]
    public string? Username { get; set; }
    [BindProperty, Required(ErrorMessage = "Nhập mật khẩu."), StringLength(128, ErrorMessage = "Mật khẩu tối đa 128 ký tự.")]
    public string? Password { get; set; }
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }
    [BindProperty(SupportsGet = true)] public bool Created { get; set; }
    [BindProperty(SupportsGet = true)] public bool Deleted { get; set; }
    [BindProperty(SupportsGet = true)] public bool Reset { get; set; }
    [BindProperty(SupportsGet = true)] public bool EmailConfirmed { get; set; }
    [BindProperty(SupportsGet = true)] public bool EmailRemoved { get; set; }

    public IActionResult OnGet() => User.Identity?.IsAuthenticated == true ? LocalRedirect("/ho-so") : Page();
    public async Task<IActionResult> OnPostAsync()
    {
        if (ModelState.IsValid)
        {
            var result = await signIn.PasswordSignInAsync(Username!, Password!, isPersistent: false, lockoutOnFailure: true);
            if (result.Succeeded) return LocalRedirect(Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : "/ho-so");
            ModelState.AddModelError(string.Empty, "Chưa đăng nhập được. Kiểm tra tên và mật khẩu; nếu đã thử nhiều lần, chờ 5 phút rồi thử lại.");
        }
        // Never return credentials in a failed form or URL.
        Password = null;
        if (ModelState.TryGetValue(nameof(Password), out var state)) state.RawValue = state.AttemptedValue = null;
        return Page();
    }
}
