using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using TaxManager.Web.Accounts;
using TaxManager.Web.Backup;

namespace TaxManager.Web.Pages.Accounts;

[EnableRateLimiting("account-entry")]
public sealed class ExportModel(UserManager<LocalUser> users, SignInManager<LocalUser> signIn,
    AccountDataExport export, TimeProvider clock, GoogleAccountConfirmation googleConfirmation, GoogleSignIn google) : PageModel
{
    [BindProperty, Required(ErrorMessage = "Nhập mật khẩu hiện tại để tải dữ liệu."), StringLength(128, ErrorMessage = "Mật khẩu tối đa 128 ký tự.")]
    public string? Password { get; set; }
    public bool HasPassword { get; private set; }
    public bool GoogleConfirmed { get; private set; }
    public bool GoogleEnabled => google.Enabled;

    public async Task OnGetAsync() => await ReadAsync();
    public async Task<IActionResult> OnPostGoogleAsync()
    {
        var user = await users.GetUserAsync(User);
        if (user is null) return Challenge();
        if (!google.Enabled) return NotFound();
        return await googleConfirmation.StartAsync(user, "export", Url);
    }
    public async Task<IActionResult> OnPostAsync(CancellationToken token)
    {
        var password = Password;
        Password = null;
        if (ModelState.TryGetValue(nameof(Password), out var state)) state.RawValue = state.AttemptedValue = null;
        var user = await users.GetUserAsync(User);
        if (user is null) return Challenge();
        await ReadAsync();
        if (!HasPassword) ModelState.Remove(nameof(Password));
        if (!ModelState.IsValid) return Page();
        var confirmed = HasPassword
            ? (await signIn.CheckPasswordSignInAsync(user, password!, lockoutOnFailure: true)).Succeeded
            : GoogleConfirmed;
        if (!confirmed)
        {
            ModelState.AddModelError(HasPassword ? nameof(Password) : string.Empty, HasPassword
                ? "Chưa xác nhận được mật khẩu. Nếu đã thử nhiều lần, chờ 5 phút rồi thử lại."
                : "Xác nhận lại bằng Google trước khi tải dữ liệu.");
            return Page();
        }
        if (!HasPassword) { await googleConfirmation.ConsumeAsync(user, "export"); GoogleConfirmed = false; }
        try
        {
            var bytes = await export.CreateAsync(User, token);
            Response.Headers.XContentTypeOptions = "nosniff";
            return File(bytes, "application/json", $"taxmanager-account-{clock.GetUtcNow():yyyyMMdd-HHmmss}.json");
        }
        catch (Exception ex) when (BackupFailure.IsExpected(ex))
        {
            Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            ModelState.AddModelError(HasPassword ? nameof(Password) : string.Empty, "Chưa tạo được tệp dữ liệu. Giữ nguyên kho và khóa bảo vệ, kiểm tra lại rồi thử tải.");
            return Page();
        }
    }
    private async Task ReadAsync()
    {
        var user = (await users.GetUserAsync(User))!;
        HasPassword = await users.HasPasswordAsync(user);
        GoogleConfirmed = !HasPassword && await googleConfirmation.IsConfirmedAsync(user, "export");
    }
}
