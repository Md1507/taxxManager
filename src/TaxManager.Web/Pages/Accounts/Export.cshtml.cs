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
    AccountDataExport export, TimeProvider clock) : PageModel
{
    [BindProperty, Required(ErrorMessage = "Nhập mật khẩu hiện tại để tải dữ liệu."), StringLength(128, ErrorMessage = "Mật khẩu tối đa 128 ký tự.")]
    public string? Password { get; set; }

    public void OnGet() { }
    public async Task<IActionResult> OnPostAsync(CancellationToken token)
    {
        var password = Password;
        Password = null;
        if (ModelState.TryGetValue(nameof(Password), out var state)) state.RawValue = state.AttemptedValue = null;
        if (!ModelState.IsValid) return Page();
        var user = await users.GetUserAsync(User);
        if (user is null) return Challenge();
        var check = await signIn.CheckPasswordSignInAsync(user, password!, lockoutOnFailure: true);
        if (!check.Succeeded)
        {
            ModelState.AddModelError(nameof(Password), "Chưa xác nhận được mật khẩu. Nếu đã thử nhiều lần, chờ 5 phút rồi thử lại.");
            return Page();
        }
        try
        {
            var bytes = await export.CreateAsync(User, token);
            Response.Headers.XContentTypeOptions = "nosniff";
            return File(bytes, "application/json", $"taxmanager-account-{clock.GetUtcNow():yyyyMMdd-HHmmss}.json");
        }
        catch (Exception ex) when (BackupFailure.IsExpected(ex))
        {
            Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            ModelState.AddModelError(nameof(Password), "Chưa tạo được tệp dữ liệu. Giữ nguyên kho và khóa bảo vệ, kiểm tra lại rồi thử tải.");
            return Page();
        }
    }
}
