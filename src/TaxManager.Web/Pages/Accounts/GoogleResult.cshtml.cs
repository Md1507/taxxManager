using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TaxManager.Web.Accounts;
using TaxManager.Web.Demo;
using TaxManager.Web.Backup;

namespace TaxManager.Web.Pages.Accounts;

[AllowAnonymous, EnableRateLimiting("account-entry")]
public sealed class GoogleResultModel(GoogleSignIn google, SignInManager<LocalUser> signIn,
    LocalIdentityDb registry, LocalAccountStores stores, UserManager<LocalUser> users, LocalDataUse dataUse) : PageModel
{
    public bool ContinueLink { get; private set; }
    public string? ConfirmationPage { get; private set; }
    public bool CreateAccount { get; private set; }
    [BindProperty] public bool AcceptDataUse { get; set; }
    public string Message { get; private set; } = "Chưa hoàn tất đăng nhập Google. Thử lại từ trang đăng nhập.";
    public async Task<IActionResult> OnGetAsync(bool failed = false, string? returnUrl = null)
    {
        if (!google.Enabled) return NotFound();
        if (failed) { await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme); return Page(); }
        var info = await signIn.GetExternalLoginInfoAsync();
        if (info?.LoginProvider != GoogleSignIn.Scheme) return Page();
        info.AuthenticationProperties!.Items.TryGetValue(GoogleSignIn.FlowKey, out var flow);
        if (flow == "link") { ContinueLink = true; return Page(); }
        if (flow is "confirm-export" or "confirm-delete" or "confirm-email")
        {
            ConfirmationPage = flow switch { "confirm-export" => "/Accounts/Export", "confirm-delete" => "/Accounts/Delete", _ => "/Accounts/Email" };
            return Page();
        }
        try
        {
            if (flow != "login") return Page();
            if (User.Identity?.IsAuthenticated == true) return RedirectToPage("/Accounts/Index");
            var id = await registry.UserLogins.AsNoTracking().Where(login => login.LoginProvider == GoogleSignIn.Scheme &&
                login.ProviderKey == info.ProviderKey).Select(login => login.UserId).SingleOrDefaultAsync();
            if (id is null)
            {
                CreateAccount = true;
                return Page();
            }
            var gate = stores.Gate(id);
            await gate.WaitAsync(HttpContext.RequestAborted);
            try
            {
                var result = await signIn.ExternalLoginSignInAsync(GoogleSignIn.Scheme, info.ProviderKey, isPersistent: false);
                if (!result.Succeeded)
                {
                    Message = "Chưa đăng nhập được. Nếu tài khoản đang tạm khóa, chờ 5 phút rồi thử lại.";
                    return Page();
                }
                return LocalRedirect(LocalDataUse.Destination(returnUrl, Url));
            }
            finally { gate.Release(); }
        }
        finally { if (!CreateAccount) await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme); }
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (!google.Enabled) return NotFound();
        if (User.Identity?.IsAuthenticated == true) return RedirectToPage("/Accounts/Index");
        var info = await signIn.GetExternalLoginInfoAsync();
        if (info?.LoginProvider != GoogleSignIn.Scheme ||
            !info.AuthenticationProperties!.Items.TryGetValue(GoogleSignIn.FlowKey, out var flow) || flow != "login")
            return Page();
        CreateAccount = true;
        if (!AcceptDataUse) ModelState.AddModelError(nameof(AcceptDataUse), "Đọc và xác nhận nội dung sử dụng dữ liệu thử trước khi tạo tài khoản.");
        if (!ModelState.IsValid) return Page();
        // Existing mappings always win. Never attach a Google subject by name or email.
        if (await users.FindByLoginAsync(info.LoginProvider, info.ProviderKey) is not null)
            return RedirectToPage("/Accounts/GoogleResult");
        using var store = new SyntheticDatabase(Directory.GetCurrentDirectory());
        await using var transaction = await registry.Database.BeginTransactionAsync();
        var user = new LocalUser { UserName = "google-" + Guid.NewGuid().ToString("N")[..12], StoreRoot = store.RootPath };
        var result = await users.CreateAsync(user);
        if (result.Succeeded) result = await users.AddLoginAsync(user, info);
        if (result.Succeeded) result = await dataUse.AcceptAsync(user);
        if (!result.Succeeded)
        {
            await transaction.RollbackAsync();
            store.Dispose();
            BackupStore.DeleteRoot(store.RootPath);
            ModelState.AddModelError(string.Empty, "Chưa tạo được tài khoản. Thử đăng nhập lại bằng Google.");
            return Page();
        }
        // Unknown commit outcomes retain the store, just like local registration.
        await transaction.CommitAsync();
        await signIn.SignInAsync(user, isPersistent: false);
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        return LocalRedirect("/ho-so");
    }
}
