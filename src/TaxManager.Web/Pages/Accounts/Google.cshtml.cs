using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using TaxManager.Web.Accounts;

namespace TaxManager.Web.Pages.Accounts;

[EnableRateLimiting("account-entry")]
public sealed class GoogleModel(GoogleSignIn google, UserManager<LocalUser> users, SignInManager<LocalUser> signIn) : PageModel
{
    [BindProperty, Required(ErrorMessage = "Nhập mật khẩu hiện tại để xác nhận."), StringLength(128)]
    public string? Password { get; set; }
    public bool Linked { get; private set; }
    public bool ReadyToLink { get; private set; }
    public bool HasPassword { get; private set; }
    [BindProperty(SupportsGet = true)] public bool Changed { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!google.Enabled) return NotFound();
        await ReadAsync();
        return Page();
    }
    public async Task<IActionResult> OnPostStartAsync()
    {
        if (!google.Enabled) return NotFound();
        var user = await users.GetUserAsync(User);
        if (user is null) return Challenge();
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        var properties = signIn.ConfigureExternalAuthenticationProperties(GoogleSignIn.Scheme,
            Url.Page("/Accounts/GoogleResult")!, user.Id);
        properties.Items[GoogleSignIn.FlowKey] = "link";
        properties.SetParameter("prompt", "select_account");
        return new ChallengeResult(GoogleSignIn.Scheme, properties);
    }
    public async Task<IActionResult> OnPostCompleteAsync() => await ChangeAsync(remove: false);
    public async Task<IActionResult> OnPostRemoveAsync() => await ChangeAsync(remove: true);
    public async Task<IActionResult> OnPostCancelAsync()
    {
        if (!google.Enabled) return NotFound();
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        return RedirectToPage("/Accounts/Google");
    }
    private async Task<IActionResult> ChangeAsync(bool remove)
    {
        if (!google.Enabled) return NotFound();
        var password = Password;
        Password = null;
        if (ModelState.TryGetValue(nameof(Password), out var state)) state.RawValue = state.AttemptedValue = null;
        var user = await users.GetUserAsync(User);
        if (user is null) return Challenge();
        await ReadAsync();
        if (!remove && !ReadyToLink)
        {
            ModelState.Clear();
            ModelState.AddModelError(string.Empty, "Xác nhận Google đã hết hạn hoặc không thuộc tài khoản này. Chọn lại Google để tiếp tục.");
            return Page();
        }
        if (!remove) ModelState.Remove(nameof(Password));
        if (!ModelState.IsValid) return Page();
        if (remove && !HasPassword)
        {
            ModelState.AddModelError(string.Empty, "Google là cách đăng nhập duy nhất. Cần thêm cách đăng nhập khác trước khi gỡ liên kết.");
            return Page();
        }
        if (remove && !(await signIn.CheckPasswordSignInAsync(user, password!, lockoutOnFailure: true)).Succeeded)
        {
            ModelState.AddModelError(nameof(Password), "Chưa xác nhận được mật khẩu. Nếu đã thử nhiều lần, chờ 5 phút rồi thử lại.");
            return Page();
        }
        IdentityResult result;
        if (remove)
        {
            var login = (await users.GetLoginsAsync(user)).SingleOrDefault(login => login.LoginProvider == GoogleSignIn.Scheme);
            if (login is null) return RedirectToPage("/Accounts/Google");
            result = await users.RemoveLoginAsync(user, login.LoginProvider, login.ProviderKey);
        }
        else
        {
            var info = await signIn.GetExternalLoginInfoAsync(user.Id);
            if (!ReadyToLink || info is null || Linked)
            {
                ModelState.AddModelError(string.Empty, "Chưa có xác nhận Google hợp lệ. Chọn lại Google để tiếp tục.");
                return Page();
            }
            result = await users.AddLoginAsync(user, info);
        }
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Chưa thay đổi được liên kết. Google này có thể đã thuộc tài khoản khác; chọn lại hoặc giữ cách đăng nhập hiện tại.");
            return Page();
        }
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        if (remove)
        {
            await signIn.SignOutAsync();
            return RedirectToPage("/Accounts/Login");
        }
        return RedirectToPage("/Accounts/Google", new { changed = true });
    }
    private async Task ReadAsync()
    {
        var user = (await users.GetUserAsync(User))!;
        HasPassword = await users.HasPasswordAsync(user);
        Linked = (await users.GetLoginsAsync(user)).Any(login => login.LoginProvider == GoogleSignIn.Scheme);
        var info = await signIn.GetExternalLoginInfoAsync(user.Id);
        ReadyToLink = info?.LoginProvider == GoogleSignIn.Scheme &&
            info.AuthenticationProperties?.Items.TryGetValue(GoogleSignIn.FlowKey, out var flow) == true && flow == "link";
    }
}
