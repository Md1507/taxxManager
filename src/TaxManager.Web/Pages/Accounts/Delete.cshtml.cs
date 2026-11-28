using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TaxManager.Web.Accounts;
using TaxManager.Web.Backup;

namespace TaxManager.Web.Pages.Accounts;

[EnableRateLimiting("account-entry")]
public sealed class DeleteModel(UserManager<LocalUser> users, SignInManager<LocalUser> signIn,
    AccountDeletion deletion, GoogleAccountConfirmation googleConfirmation, GoogleSignIn google) : PageModel
{
    [BindProperty, Required(ErrorMessage = "Nhập mật khẩu hiện tại để xác nhận xóa."), StringLength(128, ErrorMessage = "Mật khẩu tối đa 128 ký tự.")]
    public string? Password { get; set; }
    [BindProperty] public bool ConfirmDeletion { get; set; }
    public bool Pending { get; private set; }
    public bool HasPassword { get; private set; }
    public bool GoogleConfirmed { get; private set; }
    public bool GoogleEnabled => google.Enabled;

    public async Task OnGetAsync()
    {
        var user = await users.GetUserAsync(User);
        Pending = user is not null && await deletion.IsPendingAsync(user);
        if (user is not null) await ReadAsync(user);
    }

    public async Task<IActionResult> OnPostGoogleAsync()
    {
        var user = await users.GetUserAsync(User);
        if (user is null) return Challenge();
        if (!google.Enabled) return NotFound();
        return await googleConfirmation.StartAsync(user, "delete", Url);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var password = Password;
        Password = null;
        if (ModelState.TryGetValue(nameof(Password), out var state)) state.RawValue = state.AttemptedValue = null;
        var user = await users.GetUserAsync(User);
        if (user is null) return Challenge();
        Pending = await deletion.IsPendingAsync(user);
        await ReadAsync(user);
        if (!HasPassword) ModelState.Remove(nameof(Password));
        if (!HasPassword && !GoogleConfirmed)
        {
            ModelState.Clear();
            ModelState.AddModelError(string.Empty, "Xác nhận lại bằng Google trước khi xóa tài khoản.");
            return Page();
        }
        if (!ConfirmDeletion) ModelState.AddModelError(nameof(ConfirmDeletion), "Đánh dấu xác nhận sau khi đọc phạm vi xóa.");
        if (!ModelState.IsValid) return Page();
        var confirmed = HasPassword
            ? (await signIn.CheckPasswordSignInAsync(user, password!, lockoutOnFailure: true)).Succeeded
            : GoogleConfirmed;
        if (!confirmed)
        {
            ModelState.AddModelError(HasPassword ? nameof(Password) : string.Empty, HasPassword
                ? "Chưa xác nhận được mật khẩu. Nếu đã thử nhiều lần, chờ 5 phút rồi thử lại."
                : "Xác nhận lại bằng Google trước khi xóa tài khoản.");
            return Page();
        }
        if (!HasPassword) { await googleConfirmation.ConsumeAsync(user, "delete"); GoogleConfirmed = false; }
        try
        {
            // Once intent is committed, cleanup is independent of a browser disconnect.
            await deletion.DeleteAsync(user);
            await signIn.SignOutAsync();
            Response.Cookies.Delete("taxmanager.workspace", new CookieOptions { Path = "/", Secure = true });
            return RedirectToPage("/Accounts/Login", new { deleted = true });
        }
        catch (Exception ex) when (BackupFailure.IsExpected(ex) || ex is DbUpdateException)
        {
            Pending = await deletion.IsPendingAsync(user);
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            ModelState.AddModelError(string.Empty, Pending
                ? "Chưa xóa xong dữ liệu. Hồ sơ đã bị khóa truy cập; đóng ứng dụng đang dùng tệp rồi thử xóa tiếp."
                : "Chưa thể bắt đầu xóa. Dữ liệu vẫn được giữ; kiểm tra kho thử trước khi thử lại.");
            return Page();
        }
    }
    private async Task ReadAsync(LocalUser user)
    {
        HasPassword = await users.HasPasswordAsync(user);
        GoogleConfirmed = !HasPassword && await googleConfirmation.IsConfirmedAsync(user, "delete");
    }
}
