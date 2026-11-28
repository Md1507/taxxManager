using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using TaxManager.Web.Accounts;

namespace TaxManager.Web.Pages.Accounts;

[EnableRateLimiting("account-entry")]
public sealed class EmailModel(EmailRecoverySettings settings, EmailRecovery recovery, UserManager<LocalUser> users,
    SignInManager<LocalUser> signIn, GoogleAccountConfirmation google, GoogleSignIn googleSettings) : PageModel
{
    [BindProperty, Required(ErrorMessage = "Nhập email khôi phục."), EmailAddress(ErrorMessage = "Nhập một địa chỉ email hợp lệ."), StringLength(254)]
    public string? Email { get; set; }
    [BindProperty, StringLength(128, ErrorMessage = "Mật khẩu tối đa 128 ký tự.")]
    public string? Password { get; set; }
    public bool HasPassword { get; private set; }
    public bool GoogleConfirmed { get; private set; }
    public bool GoogleEnabled => googleSettings.Enabled;
    public RecoveryEmailState State { get; private set; } = new(null, null);
    [BindProperty(SupportsGet = true)] public bool Sent { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!settings.Enabled) return NotFound();
        await ReadAsync();
        return Page();
    }
    public async Task<IActionResult> OnPostGoogleAsync()
    {
        if (!settings.Enabled || !GoogleEnabled) return NotFound();
        return await google.StartAsync((await users.GetUserAsync(User))!, "email", Url);
    }
    public async Task<IActionResult> OnPostCancelAsync()
    {
        if (!settings.Enabled) return NotFound();
        await recovery.CancelPendingAsync((await users.GetUserAsync(User))!);
        return RedirectToPage();
    }
    public Task<IActionResult> OnPostAsync(CancellationToken token) => ChangeAsync(false, token);
    public Task<IActionResult> OnPostRemoveAsync(CancellationToken token) => ChangeAsync(true, token);
    private async Task<IActionResult> ChangeAsync(bool remove, CancellationToken token)
    {
        if (!settings.Enabled) return NotFound();
        var password = Password;
        Password = null;
        if (ModelState.TryGetValue(nameof(Password), out var field)) field.RawValue = field.AttemptedValue = null;
        await ReadAsync();
        if (!HasPassword && !GoogleConfirmed)
        {
            ModelState.Clear();
            ModelState.AddModelError(string.Empty, "Xác nhận lại bằng Google trước khi thay đổi email.");
            return Page();
        }
        if (remove) ModelState.Remove(nameof(Email));
        if (!HasPassword) ModelState.Remove(nameof(Password));
        else if (string.IsNullOrWhiteSpace(password)) ModelState.AddModelError(nameof(Password), "Nhập mật khẩu hiện tại để xác nhận.");
        if (!ModelState.IsValid) return Page();
        var user = (await users.GetUserAsync(User))!;
        var confirmed = HasPassword ? (await signIn.CheckPasswordSignInAsync(user, password!, lockoutOnFailure: true)).Succeeded : GoogleConfirmed;
        if (!confirmed)
        {
            ModelState.AddModelError(HasPassword ? nameof(Password) : string.Empty, HasPassword
                ? "Chưa xác nhận được mật khẩu. Nếu đã thử nhiều lần, chờ 5 phút rồi thử lại."
                : "Xác nhận lại bằng Google trước khi thay đổi email.");
            return Page();
        }
        if (!HasPassword) { await google.ConsumeAsync(user, "email"); GoogleConfirmed = false; }
        if (remove)
        {
            if (await recovery.RemoveEmailAsync(user))
            {
                await signIn.SignOutAsync();
                return RedirectToPage("/Accounts/Login", new { emailRemoved = true });
            }
        }
        else if (await recovery.RequestEmailAsync(user, Email!, token)) return RedirectToPage(new { sent = true });
        Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        ModelState.AddModelError(string.Empty, "Chưa thay đổi được email. Nếu vừa gửi yêu cầu, chờ một phút rồi thử lại.");
        await ReadAsync();
        return Page();
    }
    private async Task ReadAsync()
    {
        var user = (await users.GetUserAsync(User))!;
        HasPassword = await users.HasPasswordAsync(user);
        GoogleConfirmed = !HasPassword && await google.IsConfirmedAsync(user, "email");
        State = await recovery.ReadAsync(user);
    }
}
