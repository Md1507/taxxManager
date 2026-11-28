using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TaxManager.Web.Accounts;

namespace TaxManager.Web.Pages.Accounts;

public sealed class DataUseModel(UserManager<LocalUser> users, LocalIdentityDb registry, LocalDataUse dataUse) : PageModel
{
    [BindProperty] public bool AcceptDataUse { get; set; }
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }
    public DataUseAcceptance? Acceptance { get; private set; }

    public async Task OnGetAsync() => Acceptance = await dataUse.ReadAsync(User);

    public async Task<IActionResult> OnPostAsync()
    {
        if (!AcceptDataUse) ModelState.AddModelError(nameof(AcceptDataUse), "Xác nhận sau khi đọc nội dung để tiếp tục, hoặc đăng xuất.");
        if (!ModelState.IsValid) return Page();
        await using var transaction = await registry.Database.BeginTransactionAsync();
        var user = await users.GetUserAsync(User);
        if (user is null) return Challenge();
        var result = await dataUse.AcceptAsync(user);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Chưa lưu được xác nhận. Tải lại trang rồi thử lại.");
            return Page();
        }
        await transaction.CommitAsync();
        return LocalRedirect(LocalDataUse.Destination(ReturnUrl, Url));
    }
}
