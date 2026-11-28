using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TaxManager.Data.Forecasting;
using TaxManager.Data.Protection;
using TaxManager.Web.Backup;

namespace TaxManager.Web.Pages;

public sealed class ForecastScenariosModel(SelectedDatabase database) : PageModel
{
    public ForecastScenarioView? Comparison { get; private set; }
    public Guid ProfileId { get; private set; }
    public string? Error { get; private set; }
    [BindProperty] public int? Revision { get; set; }
    [BindProperty, Required(ErrorMessage = "Nhập số tiền thưởng muốn thử, hoặc 0 để so sánh khi không thêm thưởng.")]
    [ModelBinder(BinderType = typeof(ForecastAmountBinder))] public decimal? BonusAmount { get; set; }
    [BindProperty, Required(ErrorMessage = "Nhập tháng nhận thưởng.")]
    [Range(1, 12, ErrorMessage = "Tháng nhận thưởng phải từ 1 đến 12.")] public int? BonusMonth { get; set; }

    public Task<IActionResult> OnGetAsync(Guid id, int? revision, CancellationToken token)
    {
        Revision = revision;
        return LoadAsync(id, revision, false, token);
    }

    public Task<IActionResult> OnPostAsync(Guid id, CancellationToken token) => LoadAsync(id, Revision, true, token);

    private async Task<IActionResult> LoadAsync(Guid id, int? revision, bool post, CancellationToken token)
    {
        Response.Headers.CacheControl = "no-store";
        ProfileId = id;
        if (id == Guid.Empty || revision is < 1 || post && revision is null ||
            ModelState[nameof(Revision)]?.Errors.Count > 0 || ModelState["id"]?.Errors.Count > 0)
            return Failure(400, "Đường dẫn hoặc phiên bản không hợp lệ. Hãy mở lại từ hồ sơ đã lưu.");
        var compare = post && ModelState.IsValid;
        try
        {
            await using var context = database.Open();
            Comparison = await new ForecastScenarioService(context).CompareAsync(id, revision,
                compare ? BonusAmount : null, compare ? BonusMonth : null, token);
            if (Comparison is null) return Failure(404, "Không tìm thấy hồ sơ. Hồ sơ có thể đã bị xóa.");
            Revision = Comparison.Revision;
        }
        catch (ForecastRevisionConflictException)
        {
            return Failure(409, "Hồ sơ đã thay đổi. Mở lại kịch bản từ bản lưu mới nhất rồi so sánh.");
        }
        catch (Exception ex) when (ex is DataProtectionException or JsonException or InvalidOperationException
            or ArgumentException or KeyNotFoundException or OverflowException or NotSupportedException or Microsoft.Data.Sqlite.SqliteException)
        {
            return Failure(503, "Không thể tính kịch bản. Kiểm tra khóa bảo vệ, dữ liệu và bộ luật đã lưu rồi mở lại hồ sơ.");
        }
        return Page();
    }

    private PageResult Failure(int status, string message)
    {
        Comparison = null;
        Response.StatusCode = status;
        Error = message;
        return Page();
    }
}
