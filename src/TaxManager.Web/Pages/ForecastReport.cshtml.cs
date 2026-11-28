using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TaxManager.Data.Protection;
using TaxManager.Web.Demo;
using TaxManager.Web.Reporting;

namespace TaxManager.Web.Pages;

public sealed class ForecastReportModel(SyntheticDatabase database) : PageModel
{
    public ForecastReport? Report { get; private set; }
    public string? Error { get; private set; }
    public Task<IActionResult> OnGetAsync(Guid id, int? revision, Guid? record, CancellationToken token) => LoadAsync(id, revision, record, false, token);
    public Task<IActionResult> OnGetCsvAsync(Guid id, int? revision, Guid? record, CancellationToken token) => LoadAsync(id, revision, record, true, token);

    private async Task<IActionResult> LoadAsync(Guid id, int? revision, Guid? record, bool csv, CancellationToken token)
    {
        Response.Headers.CacheControl = "no-store";
        if (!ModelState.IsValid || id == Guid.Empty || revision is < 1)
            return Failure(400, "Đường dẫn báo cáo không hợp lệ. Hãy mở lại từ hồ sơ đã lưu.");
        try
        {
            await using var context = database.Open();
            Report = await ForecastReportReader.ReadAsync(context, id, revision, record, token);
            if (Report is null) return Failure(404, "Không tìm thấy hồ sơ, phiên bản hoặc kết quả năm này. Hãy trở lại danh sách.");
            if (csv) return File(ForecastReportCsv.Write(Report), "text/csv; charset=utf-8", $"taxmanager-{Report.Input.Year}-v{Report.Revision}.csv");
            return Page();
        }
        catch (Exception ex) when (ex is DataProtectionException or JsonException or InvalidOperationException
            or ArgumentException or KeyNotFoundException or OverflowException or NotSupportedException or Microsoft.Data.Sqlite.SqliteException)
        {
            Report = null;
            return Failure(503, "Không thể mở báo cáo. Kiểm tra khóa bảo vệ, dữ liệu và bản lưu rồi thử lại.");
        }
    }

    private PageResult Failure(int code, string message)
    {
        Response.StatusCode = code;
        Error = message;
        return Page();
    }
}
