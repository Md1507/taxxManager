using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TaxManager.Web.Backup;

namespace TaxManager.Web.Pages;

public sealed class BackupModel(SelectedDatabase selected, BackupStore backups, TimeProvider clock) : PageModel
{
    public bool IsRestored => selected.IsRestored;
    public void OnGet() => Response.Headers.CacheControl = "no-store";

    public async Task<IActionResult> OnPostAsync(string? password, string? confirmation, CancellationToken token)
    {
        Response.Headers.CacheControl = "no-store";
        // Never echo attempted secrets through ModelState or the password fields.
        ModelState.Remove(nameof(password));
        ModelState.Remove(nameof(confirmation));
        if (!BackupEnvelope.ValidPassword(password)) ModelState.AddModelError("password", "Dùng mật khẩu từ 12 đến 128 ký tự.");
        if (password != confirmation) ModelState.AddModelError("confirmation", "Hai mật khẩu chưa khớp. Hãy nhập lại.");
        if (!ModelState.IsValid) return Page();
        try
        {
            var bytes = await backups.ExportAsync(selected.Database, password!, clock.GetUtcNow(), token);
            return File(bytes, "application/octet-stream", $"taxmanager-{clock.GetUtcNow():yyyyMMdd-HHmmss}.taxbackup");
        }
        catch (Exception ex) when (BackupFailure.IsExpected(ex))
        {
            Response.StatusCode = 422;
            ModelState.AddModelError(string.Empty, "Không thể tạo bản sao. Kiểm tra khóa bảo vệ và dữ liệu của kho rồi thử lại.");
            return Page();
        }
    }
    public IActionResult OnPostReturn()
    {
        selected.State.Active = null;
        selected.State.Generation++;
        return RedirectToPage("/ForecastProfiles");
    }
}
