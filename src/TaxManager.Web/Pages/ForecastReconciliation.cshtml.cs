using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TaxManager.Core.Forecasting;
using TaxManager.Data.Forecasting;
using TaxManager.Data.Protection;
using TaxManager.Web.Backup;

namespace TaxManager.Web.Pages;

public enum ReconciliationSource { Company, ETax, Other }

public sealed class ForecastReconciliationModel(SelectedDatabase database, TimeProvider clock) : PageModel
{
    public ReconciliationProfile? Profile { get; private set; }
    public ReconciliationTotal? Result { get; private set; }
    public DateTimeOffset? EnteredAt { get; private set; }
    public Guid ProfileId { get; private set; }
    public string? Error { get; private set; }
    [BindProperty] public int? Revision { get; set; }
    [BindProperty, Required(ErrorMessage = "Chọn nguồn số liệu."), EnumDataType(typeof(ReconciliationSource), ErrorMessage = "Nguồn số liệu không hợp lệ.")]
    public ReconciliationSource? Source { get; set; }
    [BindProperty, Required(ErrorMessage = "Ghi tên tài liệu hoặc mô tả nguồn."), StringLength(200, ErrorMessage = "Mô tả nguồn tối đa 200 ký tự.")]
    public string? SourceName { get; set; }
    [BindProperty, Required(ErrorMessage = "Nhập năm của số nguồn."), Range(1, 9999, ErrorMessage = "Năm phải từ 1 đến 9999.")]
    public int? SourceYear { get; set; }
    [BindProperty, Required(ErrorMessage = "Nhập tháng đầu kỳ."), Range(1, 12, ErrorMessage = "Tháng đầu phải từ 1 đến 12.")]
    public int? FromMonth { get; set; }
    [BindProperty, Required(ErrorMessage = "Nhập tháng cuối kỳ."), Range(1, 12, ErrorMessage = "Tháng cuối phải từ 1 đến 12.")]
    public int? ToMonth { get; set; }
    [BindProperty, Required(ErrorMessage = "Chọn chỉ tiêu cùng nghĩa với số nguồn."), EnumDataType(typeof(ReconciliationMetric), ErrorMessage = "Chỉ tiêu không hợp lệ.")]
    public ReconciliationMetric? Metric { get; set; }
    [BindProperty, Required(ErrorMessage = "Nhập số tiền từ nguồn; nhập 0 nếu nguồn ghi bằng 0.")]
    [ModelBinder(BinderType = typeof(ForecastAmountBinder))] public decimal? Amount { get; set; }
    [BindProperty, Required(ErrorMessage = "Chọn trạng thái số nguồn."), EnumDataType(typeof(ForecastStatus), ErrorMessage = "Trạng thái số nguồn không hợp lệ.")]
    public ForecastStatus? Status { get; set; }
    [BindProperty, StringLength(2000, ErrorMessage = "Ghi chú tối đa 2000 ký tự.")] public string? Note { get; set; }

    public static string MetricLabel(ReconciliationMetric metric) => metric switch {
        ReconciliationMetric.Gross => "Gross đã nhập",
        ReconciliationMetric.Insurance => "Bảo hiểm nhập tay",
        _ => "Thuế đã khấu trừ"
    };
    public static string SourceLabel(ReconciliationSource source) => source switch {
        ReconciliationSource.Company => "Công ty", ReconciliationSource.ETax => "eTax", _ => "Khác"
    };
    public decimal? Difference => Result?.Amount is { } baseline && Amount is { } source ? source - baseline : null;

    public Task<IActionResult> OnGetAsync(Guid id, int? revision, CancellationToken token)
    {
        Revision = revision;
        return LoadAsync(id, false, token);
    }
    public Task<IActionResult> OnPostAsync(Guid id, CancellationToken token) => LoadAsync(id, true, token);

    private async Task<IActionResult> LoadAsync(Guid id, bool post, CancellationToken token)
    {
        Response.Headers.CacheControl = "no-store";
        ProfileId = id;
        if (id == Guid.Empty || Revision is < 1 || post && Revision is null ||
            ModelState[nameof(Revision)]?.Errors.Count > 0 || ModelState["id"]?.Errors.Count > 0)
            return Failure(400, "Đường dẫn hoặc phiên bản không hợp lệ. Mở lại từ hồ sơ đã lưu.");
        if (post && FromMonth > ToMonth) ModelState.AddModelError(nameof(ToMonth), "Tháng cuối phải bằng hoặc sau tháng đầu.");
        if (post && Status == ForecastStatus.Assumed && string.IsNullOrWhiteSpace(Note))
            ModelState.AddModelError(nameof(Note), "Ghi cách hình thành số giả định.");
        try
        {
            await using var context = database.Open();
            Profile = await new ForecastReconciliationService(context).ReadAsync(id, Revision, token);
            if (Profile is null) return Failure(404, "Không tìm thấy hồ sơ. Hồ sơ có thể đã bị xóa.");
            Revision = Profile.Revision;
            if (!post) SourceYear = Profile.Input.Year;
            else if (ModelState.IsValid)
            {
                Result = Profile.Summarize(Metric!.Value, SourceYear!.Value, FromMonth!.Value, ToMonth!.Value);
                EnteredAt = clock.GetUtcNow();
            }
        }
        catch (ForecastRevisionConflictException)
        { return Failure(409, "Hồ sơ đã thay đổi. Mở bản lưu mới nhất rồi đối chiếu lại."); }
        catch (Exception ex) when (ex is DataProtectionException or JsonException or InvalidOperationException
            or ArgumentException or KeyNotFoundException or OverflowException or NotSupportedException or Microsoft.Data.Sqlite.SqliteException)
        { return Failure(503, "Không thể đọc số liệu hồ sơ. Kiểm tra khóa bảo vệ và dữ liệu đã lưu rồi mở lại."); }
        return Page();
    }
    private PageResult Failure(int status, string message)
    {
        Profile = null;
        Result = null;
        Response.StatusCode = status;
        Error = message;
        return Page();
    }
}
