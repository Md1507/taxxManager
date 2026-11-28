using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TaxManager.Web.Backup;

namespace TaxManager.Web.Pages;

public sealed class RestorePreviewModel(SelectedDatabase selected, BackupWorkspaces workspaces, TimeProvider clock) : PageModel
{
    public RestorePreview? Preview { get; private set; }
    public IActionResult OnGet()
    {
        Response.Headers.CacheControl = "no-store";
        return Load() ? Page() : Expired();
    }
    public IActionResult OnPostAsync(string? handle)
    {
        Response.Headers.CacheControl = "no-store";
        if (!Load() || handle != Preview!.Handle) return Expired();
        try
        {
            if (BackupStore.Fingerprint(Preview.Store) != Preview.Fingerprint)
            {
                workspaces.Cancel(selected.State);
                return Expired();
            }
            workspaces.Accept(selected.State);
            return RedirectToPage("/ForecastProfiles");
        }
        catch (Exception ex) when (BackupFailure.IsExpected(ex))
        {
            ModelState.AddModelError(string.Empty, "Chưa thể mở thêm kho khôi phục trong phiên chạy này. Hãy hủy và khởi động lại ứng dụng rồi thử lại.");
            Response.StatusCode = 409;
            return Page();
        }
    }
    public IActionResult OnPostCancel(string? handle)
    {
        if (!Load() || handle != Preview!.Handle) return Expired();
        workspaces.Cancel(selected.State);
        return RedirectToPage("/Restore");
    }
    private bool Load()
    {
        Preview = selected.State.Preview;
        if (Preview is null) return false;
        if (Preview.ExpiresAt > clock.GetUtcNow()) return true;
        workspaces.Cancel(selected.State);
        Preview = null;
        return false;
    }
    private PageResult Expired()
    {
        Preview = null;
        Response.StatusCode = 410;
        Response.Headers.CacheControl = "no-store";
        return Page();
    }
}
