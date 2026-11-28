using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using TaxManager.Web.Accounts;

namespace TaxManager.Web.Pages.Accounts;

[AllowAnonymous, EnableRateLimiting("account-entry")]
public sealed class ResetPasswordModel(EmailRecoverySettings settings, EmailRecovery recovery, SignInManager<LocalUser> signIn) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? UserId { get; set; }
    [BindProperty(SupportsGet = true)] public string? RequestId { get; set; }
    [BindProperty(SupportsGet = true)] public string? Code { get; set; }
    [BindProperty, Required(ErrorMessage = "Nhập mật khẩu mới."), StringLength(128, MinimumLength = 8, ErrorMessage = "Dùng mật khẩu từ 8 đến 128 ký tự.")]
    public string? Password { get; set; }
    [BindProperty, Required(ErrorMessage = "Nhập lại mật khẩu mới."), Compare(nameof(Password), ErrorMessage = "Hai mật khẩu chưa khớp. Nhập lại để xác nhận.")]
    public string? ConfirmPassword { get; set; }
    public bool Ready { get; private set; }
    public async Task<IActionResult> OnGetAsync()
    {
        Response.Headers["Referrer-Policy"] = "no-referrer";
        if (!settings.Enabled) return NotFound();
        Ready = await recovery.CanResetAsync(UserId ?? "", RequestId ?? "", Code ?? "");
        return Page();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        Response.Headers["Referrer-Policy"] = "no-referrer";
        if (!settings.Enabled) return NotFound();
        var password = Password;
        Password = ConfirmPassword = null;
        foreach (var key in new[] { nameof(Password), nameof(ConfirmPassword) })
            if (ModelState.TryGetValue(key, out var state)) state.RawValue = state.AttemptedValue = null;
        Ready = await recovery.CanResetAsync(UserId ?? "", RequestId ?? "", Code ?? "");
        if (!Ready || !ModelState.IsValid) return Page();
        if (await recovery.ResetAsync(UserId ?? "", RequestId ?? "", Code ?? "", password!))
        {
            await signIn.SignOutAsync();
            return RedirectToPage("/Accounts/Login", new { reset = true });
        }
        ModelState.AddModelError(nameof(Password), "Chưa đặt lại được mật khẩu. Dùng 8–128 ký tự và thử lại; nếu liên kết đã hết hạn, yêu cầu thư mới.");
        return Page();
    }
}
