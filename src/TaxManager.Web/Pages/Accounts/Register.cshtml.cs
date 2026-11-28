using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using TaxManager.Web.Accounts;
using TaxManager.Web.Backup;
using TaxManager.Web.Demo;

namespace TaxManager.Web.Pages.Accounts;

[AllowAnonymous, EnableRateLimiting("account-entry")]
public sealed class RegisterModel(UserManager<LocalUser> users, LocalIdentityDb registry, LocalDataUse dataUse) : PageModel
{
    [BindProperty, Required(ErrorMessage = "Nhập tên đăng nhập."), RegularExpression("[A-Za-z0-9._-]{3,64}", ErrorMessage = "Dùng 3–64 chữ cái không dấu, chữ số, dấu chấm, gạch dưới hoặc gạch ngang.")]
    public string? Username { get; set; }
    [BindProperty, Required(ErrorMessage = "Nhập mật khẩu."), StringLength(128, MinimumLength = 8, ErrorMessage = "Dùng mật khẩu từ 8 đến 128 ký tự.")]
    public string? Password { get; set; }
    [BindProperty, Required(ErrorMessage = "Nhập lại mật khẩu."), Compare(nameof(Password), ErrorMessage = "Hai mật khẩu chưa khớp. Nhập lại để xác nhận.")]
    public string? ConfirmPassword { get; set; }
    [BindProperty] public bool AcceptDataUse { get; set; }

    public IActionResult OnGet() => User.Identity?.IsAuthenticated == true ? LocalRedirect("/ho-so") : Page();
    public async Task<IActionResult> OnPostAsync()
    {
        if (!AcceptDataUse) ModelState.AddModelError(nameof(AcceptDataUse), "Đọc và xác nhận nội dung sử dụng dữ liệu thử trước khi tạo tài khoản.");
        if (ModelState.IsValid)
        {
            using var store = new SyntheticDatabase(Directory.GetCurrentDirectory());
            await using var transaction = await registry.Database.BeginTransactionAsync();
            var user = new LocalUser { UserName = Username, StoreRoot = store.RootPath };
            var result = await users.CreateAsync(user, Password!);
            if (result.Succeeded) result = await dataUse.AcceptAsync(user);
            if (result.Succeeded)
            {
                await transaction.CommitAsync();
                return RedirectToPage("/Accounts/Login", new { created = true });
            }
            await transaction.RollbackAsync();
            // Only a definite Identity failure permits cleanup. Unknown commit outcomes retain the store.
            store.Dispose();
            BackupStore.DeleteRoot(store.RootPath);
            ModelState.AddModelError(nameof(Username), "Chưa tạo được tài khoản với tên này. Thử một tên khác và mật khẩu đủ dài.");
        }
        Password = ConfirmPassword = null;
        // Keep validation messages but discard the submitted password values.
        foreach (var key in new[] { nameof(Password), nameof(ConfirmPassword) })
            if (ModelState.TryGetValue(key, out var state)) state.RawValue = state.AttemptedValue = null;
        return Page();
    }
}
