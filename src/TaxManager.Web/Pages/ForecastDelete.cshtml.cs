using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TaxManager.Data.Forecasting;
using TaxManager.Data.Protection;
using TaxManager.Web.Demo;

namespace TaxManager.Web.Pages;

public sealed class ForecastDeleteModel(TaxManager.Web.Backup.SelectedDatabase database) : PageModel
{
    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    [BindProperty(SupportsGet = true)] public int Revision { get; set; }
    public int? Year { get; private set; }
    public int SavedRevision { get; private set; }
    public decimal? MinimumGross { get; private set; }
    public decimal? MaximumGross { get; private set; }
    public int MonthsWithGross { get; private set; }
    public bool CanDelete { get; private set; }

    public async Task OnGetAsync()
    {
        try { CanDelete = await LoadTargetAsync(resolveMissingRevision: true); }
        catch (Exception ex) when (IsReadFailure(ex))
        { ModelState.AddModelError(string.Empty, "Chưa mở được hồ sơ để xác nhận. Kiểm tra dữ liệu và khóa bảo vệ rồi thử lại."); }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            if (!await LoadTargetAsync(resolveMissingRevision: false)) return Page();
            await using var context = database.Open();
            await new ForecastStore(context).DeleteAsync(Id, Revision);
            return RedirectToPage("/ForecastProfiles");
        }
        catch (Exception ex) when (IsReadFailure(ex))
        {
            ModelState.AddModelError(string.Empty, "Không thể xóa hồ sơ; dữ liệu có thể đã thay đổi hoặc khóa bảo vệ không hợp lệ.");
            return Page();
        }
    }

    private async Task<bool> LoadTargetAsync(bool resolveMissingRevision)
    {
        if (!ModelState.IsValid) return false;
        await using var context = database.Open();
        var document = await new ForecastStore(context).ReadAsync(Id);
        if (document is null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            ModelState.AddModelError(string.Empty, "Không tìm thấy hồ sơ. Hồ sơ có thể đã được xóa.");
            return false;
        }
        var input = ForecastDraftReader.Read(document.Payload!);
        Year = input.Year;
        SavedRevision = document.Revision;
        var gross = input.Months.Where(m => m.Gross is not null).Select(m => m.Gross!.Value).ToArray();
        MonthsWithGross = gross.Length;
        MinimumGross = gross.Length == 0 ? null : gross.Min();
        MaximumGross = gross.Length == 0 ? null : gross.Max();
        if (resolveMissingRevision && Revision == 0)
        {
            Revision = document.Revision;
            ModelState.Remove(nameof(Revision));
        }
        if (Revision != document.Revision)
        {
            ModelState.AddModelError(string.Empty, "Hồ sơ đã có thay đổi. Mở lại hồ sơ để kiểm tra trước khi xóa.");
            return false;
        }
        return true;
    }

    private static bool IsReadFailure(Exception ex) => ex is DataProtectionException or InvalidOperationException
        or DbUpdateException or Microsoft.Data.Sqlite.SqliteException or JsonException or ArgumentException or KeyNotFoundException;
}
