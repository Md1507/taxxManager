using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TaxManager.Data.Forecasting;
using TaxManager.Data.Protection;
using TaxManager.Web.Demo;

namespace TaxManager.Web.Pages;

public sealed class ForecastMonthModel(TaxManager.Web.Backup.SelectedDatabase database) : PageModel
{
    public ForecastMonthView? Month { get; private set; }
    public Guid ProfileId { get; private set; }
    public string? Error { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id, int revision, int month, CancellationToken token)
    {
        if (!ModelState.IsValid || id == Guid.Empty || revision < 1 || month is < 1 or > 12)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            Error = "Đường dẫn thiếu bản lưu hoặc tháng không hợp lệ. Hãy mở lại từ bảng 12 tháng.";
            return Page();
        }
        ProfileId = id;
        Response.Headers.CacheControl = "no-store";
        try
        {
            await using var context = database.Open();
            Month = await new ForecastMonthService(context).ReadAsync(id, revision, month, token);
            if (Month is null)
            {
                Response.StatusCode = StatusCodes.Status404NotFound;
                Error = "Không tìm thấy hồ sơ này. Hồ sơ có thể đã bị xóa; hãy trở lại danh sách.";
            }
        }
        catch (ForecastRevisionConflictException)
        {
            Response.StatusCode = StatusCodes.Status409Conflict;
            Error = "Hồ sơ đã thay đổi. Mở lại hồ sơ rồi chọn tháng để xem bản lưu mới nhất.";
        }
        catch (Exception ex) when (ex is DataProtectionException or InvalidOperationException or ArgumentException
            or JsonException or KeyNotFoundException or OverflowException or NotSupportedException or Microsoft.Data.Sqlite.SqliteException)
        {
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            Error = "Không thể đọc hoặc tính tháng này. Kiểm tra khóa bảo vệ, dữ liệu và bộ luật đã lưu rồi mở lại hồ sơ.";
        }
        return Page();
    }
}
