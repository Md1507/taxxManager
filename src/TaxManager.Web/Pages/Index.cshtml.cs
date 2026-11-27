using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TaxManager.Data.CalculationHistory;
using TaxManager.Web.MonthlySalary;

namespace TaxManager.Web.Pages;

public sealed class IndexModel(ISalaryCalculator calculator) : PageModel
{
    public IReadOnlyList<CalculationSnapshot>? Results { get; private set; }
    public string? Error { get; private set; }
    public int Year { get; private set; } = 2025;
    public string[] Gross { get; private set; } = new string[12];
    public string[] Insurance { get; private set; } = new string[12];
    public string[] Withheld { get; private set; } = new string[12];
    public void OnGet() { }

    public void OnGetSample()
    {
        Gross = Enumerable.Repeat("30000000", 12).ToArray();
        Insurance = Enumerable.Repeat("0", 12).ToArray();
        Withheld = Enumerable.Repeat("0", 12).ToArray();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!Request.HasFormContentType)
            return new ContentResult { StatusCode = StatusCodes.Status415UnsupportedMediaType,
                ContentType = "text/plain; charset=utf-8", Content = "Không hỗ trợ định dạng dữ liệu; vui lòng gửi biểu mẫu." };
        var form = await Request.ReadFormAsync();
        var allowed = new HashSet<string>(StringComparer.Ordinal) { "year", "gross", "insurance", "withheld", "__RequestVerificationToken", "AmountFormat" };
        if (form.Keys.Any(key => !allowed.Contains(key)) || form["year"].Count != 1 ||
            !int.TryParse(form["year"], out var year) || year is not (2025 or 2026))
        {
            Error = "Năm hoặc trường nhập không được hỗ trợ; chưa tính thuế.";
            return Page();
        }
        Year = year;
        Gross = form["gross"].Select(x => x ?? "").ToArray();
        Insurance = form["insurance"].Select(x => x ?? "").ToArray();
        Withheld = form["withheld"].Select(x => x ?? "").ToArray();
        if (Gross.Length != 12 || Insurance.Length != 12 || Withheld.Length != 12)
        {
            Error = "Cần nhập đủ chính xác 12 tháng; chưa tính thuế.";
            return Page();
        }
        var months = new List<SalaryInput>();
        var grouped = form["AmountFormat"] == MoneyInput.GroupedFormat;
        for (var i = 0; i < 12; i++)
        {
            if (!MoneyInput.TryParse(Gross[i], grouped, out var gross) || !MoneyInput.TryParse(Insurance[i], grouped, out var insurance) ||
                !MoneyInput.TryParse(Withheld[i], grouped, out var withheld))
            {
                Error = grouped
                    ? $"Tháng {i + 1}: nhập số tiền không âm như 30.000.000,50; dùng dấu phẩy cho phần lẻ (tối đa 28 chữ số)."
                    : $"Tháng {i + 1}: nhập số tiền không âm, không dùng dấu phân nhóm và tối đa 28 chữ số thập phân.";
                return Page();
            }
            months.Add(new SalaryInput(gross, insurance, withheld));
        }
        try { Results = await calculator.CalculateAsync(year, months); }
        catch (Exception)
        {
            Error = "Không hoàn tất tính đủ 12 tháng; trạng thái lưu có thể chưa xác định. Không tự gửi lại khi chưa kiểm tra.";
        }
        return Page();
    }

}
