using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TaxManager.Data.Forecasting;
using TaxManager.Web.Backup;

namespace TaxManager.Web.Pages;

[RequestSizeLimit(18 * 1024 * 1024)]
[RequestFormLimits(MultipartBodyLengthLimit = 18 * 1024 * 1024)]
public sealed class RestoreModel(SelectedDatabase selected, BackupWorkspaces workspaces, BackupStore backups, TimeProvider clock) : PageModel
{
    public bool IsRestored => selected.IsRestored;
    public void OnGet() => Response.Headers.CacheControl = "no-store";
    public async Task<IActionResult> OnPostAsync(IFormFile? upload, string? password, CancellationToken token)
    {
        Response.Headers.CacheControl = "no-store";
        ModelState.Remove(nameof(password));
        if (IsRestored) ModelState.AddModelError(string.Empty, "Quay lại kho ban đầu trước khi mở một bản sao khác.");
        if (upload is null || upload.Length is <= 0 or > BackupEnvelope.MaxFileBytes)
            ModelState.AddModelError("upload", "Chọn tệp .taxbackup có dung lượng tối đa 17 MiB.");
        if (!BackupEnvelope.ValidPassword(password)) ModelState.AddModelError("password", "Nhập mật khẩu bản sao, từ 12 đến 128 ký tự.");
        if (!ModelState.IsValid) return Page();
        TaxManager.Web.Demo.SyntheticDatabase? staged = null;
        try
        {
            workspaces.Cancel(selected.State);
            using var buffer = new MemoryStream();
            await upload!.CopyToAsync(buffer, token);
            if (buffer.Length != upload.Length || buffer.Length > BackupEnvelope.MaxFileBytes) throw new InvalidDataException();
            var imported = await backups.ImportAsync(buffer.ToArray(), password!, token);
            staged = imported.Store;
            await using var current = selected.Open();
            var ids = (await new ForecastStore(current).ListAsync(token)).Select(d => d.Id)
                .Concat(await current.UserProfiles.AsNoTracking().Select(p => p.Id).ToArrayAsync(token)).ToHashSet();
            selected.State.Preview = new(Guid.NewGuid().ToString("N"), clock.GetUtcNow().AddMinutes(10), staged, imported.Summary,
                imported.Summary.ProfileIds.Count(ids.Contains), BackupStore.Fingerprint(staged));
            staged = null; // Registry now owns cleanup or activation.
            return RedirectToPage("/RestorePreview");
        }
        catch (Exception ex) when (BackupFailure.IsExpected(ex))
        {
            Response.StatusCode = 422;
            ModelState.AddModelError(string.Empty, "Không thể mở bản sao: mật khẩu sai, tệp hỏng hoặc phiên bản dữ liệu chưa được hỗ trợ. Chọn lại tệp và nhập lại mật khẩu.");
            return Page();
        }
        finally
        {
            if (staged is not null) { staged.Dispose(); BackupStore.DeleteRoot(staged.RootPath); }
        }
    }
}
