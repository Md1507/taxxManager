using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TaxManager.Data.Forecasting;
using TaxManager.Data.Protection;
using TaxManager.Web.Demo;

namespace TaxManager.Web.Pages;

public sealed class ForecastDeleteModel(SyntheticDatabase database) : PageModel
{
    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    [BindProperty(SupportsGet = true)] public int Revision { get; set; }
    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            await using var context = database.Open();
            await new ForecastStore(context).DeleteAsync(Id, Revision);
            return RedirectToPage("/ForecastProfiles");
        }
        catch (Exception ex) when (ex is DataProtectionException or InvalidOperationException or DbUpdateException or Microsoft.Data.Sqlite.SqliteException)
        {
            ModelState.AddModelError(string.Empty, "Không thể xóa hồ sơ; dữ liệu có thể đã thay đổi hoặc khóa bảo vệ không hợp lệ.");
            return Page();
        }
    }
}
