using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TaxManager.Core.Forecasting;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Globalization;
using System.Text.RegularExpressions;

namespace TaxManager.Web.Pages;

public sealed class ForecastAmountBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext context)
    {
        var input = context.ValueProvider.GetValue(context.ModelName);
        if (input == ValueProviderResult.None) return Task.CompletedTask;
        context.ModelState.SetModelValue(context.ModelName, input);
        var text = input.FirstValue ?? "";
        if (text.Length == 0) context.Result = ModelBindingResult.Success(null);
        else if (Regex.IsMatch(text, @"^(0|[1-9][0-9]*)([,.][0-9]{1,28})?$", RegexOptions.CultureInvariant) &&
            decimal.TryParse(text.Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value) &&
            value.ToString(CultureInfo.InvariantCulture) == text.Replace(',', '.'))
            context.Result = ModelBindingResult.Success(value);
        else
        {
            context.ModelState.TryAddModelError(context.ModelName, "Nhập số tiền không âm, không dùng dấu phân cách hàng nghìn (ví dụ 30000000,50).");
            context.Result = ModelBindingResult.Success(null);
        }
        return Task.CompletedTask;
    }
}

public sealed class ForecastModel : PageModel
{
    public sealed class ExceptionInput
    {
        public int Month { get; set; }
        [ModelBinder(BinderType = typeof(ForecastAmountBinder))] public decimal? Gross { get; set; }
        public ForecastStatus Status { get; set; } = ForecastStatus.Planned;
        public string? Source { get; set; }
        public string? Note { get; set; }
        [ModelBinder(BinderType = typeof(ForecastAmountBinder))] public decimal? Insurance { get; set; }
        public ForecastStatus InsuranceStatus { get; set; } = ForecastStatus.Planned;
        public string? InsuranceSource { get; set; }
        public string? InsuranceNote { get; set; }
        public int? Dependents { get; set; }
        public ForecastStatus DependentsStatus { get; set; } = ForecastStatus.Planned;
        public string? DependentsSource { get; set; }
        public string? DependentsNote { get; set; }
        [ModelBinder(BinderType = typeof(ForecastAmountBinder))] public decimal? Withheld { get; set; }
        public ForecastStatus WithheldStatus { get; set; } = ForecastStatus.Planned;
        public string? WithheldSource { get; set; }
        public string? WithheldNote { get; set; }
    }
    public sealed class PeriodInput
    {
        public int FromMonth { get; set; } = 1;
        public int ToMonth { get; set; } = 12;
        [ModelBinder(BinderType = typeof(ForecastAmountBinder))] public decimal? Gross { get; set; }
        public ForecastStatus Status { get; set; } = ForecastStatus.Planned;
        public string? Source { get; set; }
        public string? Note { get; set; }
    }
    [BindProperty] public int FromMonth { get; set; } = 1;
    [BindProperty] public int ToMonth { get; set; } = 12;
    [BindProperty, ModelBinder(BinderType = typeof(ForecastAmountBinder))] public decimal? Gross { get; set; }
    [BindProperty] public ForecastStatus Status { get; set; } = ForecastStatus.Planned;
    [BindProperty] public string? Source { get; set; }
    [BindProperty] public string? Note { get; set; }
    [BindProperty] public List<ExceptionInput> Exceptions { get; set; } = Enumerable.Range(1, 12).Select(m => new ExceptionInput { Month = m }).ToList();
    [BindProperty] public int SelectedMonth { get; set; }
    [BindProperty] public int EditingMonth { get; set; }
    public bool ConfirmClear { get; private set; }
    [BindProperty] public List<PeriodInput> Periods { get; set; } = [];
    public SalarySchedulePreview? Preview { get; private set; }
    public IReadOnlyDictionary<int, ForecastValue<decimal>?> InsurancePreview { get; private set; } = new Dictionary<int, ForecastValue<decimal>?>();
    public IReadOnlyDictionary<int, ForecastValue<int>?> DependentsPreview { get; private set; } = new Dictionary<int, ForecastValue<int>?>();
    public IReadOnlyDictionary<int, ForecastValue<decimal>?> WithheldPreview { get; private set; } = new Dictionary<int, ForecastValue<decimal>?>();
    public IReadOnlyList<int> ReplacedPeriodMonths { get; private set; } = [];

    public void OnGet() { }

    public static string FormatAmount(decimal amount) => amount.ToString("N" + ((decimal.GetBits(amount)[3] >> 16) & 0x7f), CultureInfo.GetCultureInfo("vi-VN"));

    public bool HasMonthData(int index)
    {
        if (index >= Exceptions.Count) return false;
        var e = Exceptions[index];
        return e.Gross is not null || e.Insurance is not null || e.Dependents is not null || e.Withheld is not null ||
            !string.IsNullOrEmpty(e.Source) || !string.IsNullOrEmpty(e.Note) || !string.IsNullOrEmpty(e.InsuranceSource) ||
            !string.IsNullOrEmpty(e.InsuranceNote) || !string.IsNullOrEmpty(e.DependentsSource) || !string.IsNullOrEmpty(e.DependentsNote) ||
            !string.IsNullOrEmpty(e.WithheldSource) || !string.IsNullOrEmpty(e.WithheldNote) ||
            ModelState.Any(kv => kv.Key.StartsWith($"Exceptions[{index}].", StringComparison.Ordinal) && kv.Value?.Errors.Count > 0);
    }

    public IActionResult OnPostSelectMonth(int? targetMonth)
    {
        EnsureExceptionSlots();
        if (targetMonth is >= 1 and <= 12) { SelectedMonth = targetMonth.Value; ModelState.Remove(nameof(SelectedMonth)); }
        if (SelectedMonth is < 0 or > 12) SelectedMonth = 0;
        return Page();
    }

    public IActionResult OnPostConfirmClearMonth()
    {
        EnsureExceptionSlots();
        SelectedMonth = EditingMonth;
        ModelState.Remove(nameof(SelectedMonth));
        ConfirmClear = SelectedMonth is >= 1 and <= 12;
        return Page();
    }

    public IActionResult OnPostClearMonth()
    {
        EnsureExceptionSlots();
        SelectedMonth = EditingMonth;
        ModelState.Remove(nameof(SelectedMonth));
        if (SelectedMonth is >= 1 and <= 12)
        {
            var index = SelectedMonth - 1;
            Exceptions[index] = new ExceptionInput { Month = SelectedMonth };
            foreach (var key in ModelState.Keys.Where(k => k.StartsWith($"Exceptions[{index}].", StringComparison.Ordinal)).ToArray()) ModelState.Remove(key);
        }
        return Page();
    }

    public IActionResult OnPostAddPeriod()
    {
        EnsureExceptionSlots();
        if (Periods.Count < 11) Periods.Add(new PeriodInput());
        return Page();
    }

    public IActionResult OnPostRemovePeriod(int index)
    {
        EnsureExceptionSlots();
        if (index < 0 || index >= Periods.Count) return Page();
        var entries = ModelState.Where(kv => kv.Key.StartsWith("Periods[", StringComparison.Ordinal)).ToArray();
        foreach (var entry in entries) ModelState.Remove(entry.Key);
        foreach (var entry in entries)
        {
            var match = Regex.Match(entry.Key, @"^Periods\[(\d+)\](.*)$");
            if (!match.Success || !int.TryParse(match.Groups[1].Value, out var oldIndex) || oldIndex == index || entry.Value is null) continue;
            var key = $"Periods[{(oldIndex > index ? oldIndex - 1 : oldIndex)}]{match.Groups[2].Value}";
            ModelState.SetModelValue(key, entry.Value.RawValue, entry.Value.AttemptedValue);
            foreach (var error in entry.Value.Errors) ModelState.AddModelError(key, error.ErrorMessage);
        }
        Periods.RemoveAt(index);
        return Page();
    }

    public IActionResult OnPostPreview()
    {
        void ValidatePeriod(string prefix, string label, int from, int to, decimal? gross, ForecastStatus status, string? note)
        {
            if (from is < 1 or > 12) ModelState.AddModelError(prefix + "FromMonth", $"{label}: chọn tháng từ 1 đến 12.");
            if (to is < 1 or > 12 || from > to) ModelState.AddModelError(prefix + "ToMonth", $"{label}: tháng kết thúc phải từ 1 đến 12 và không trước tháng bắt đầu.");
            if (gross is not null && status == ForecastStatus.Assumed && string.IsNullOrWhiteSpace(note))
                ModelState.AddModelError(prefix + "Note", $"{label}: hãy ghi lý do giả định.");
        }
        ValidatePeriod("", "Giai đoạn lương", FromMonth, ToMonth, Gross, Status, Note);
        for (var i = 0; i < Periods.Count; i++)
        {
            var p = Periods[i];
            ValidatePeriod($"Periods[{i}].", $"Giai đoạn thay đổi {i + 1}", p.FromMonth, p.ToMonth, p.Gross, p.Status, p.Note);
            var grossKey = $"Periods[{i}].Gross";
            if (p.Gross is null && ModelState[grossKey]?.Errors.Count is not > 0)
                ModelState.AddModelError(grossKey, $"Giai đoạn thay đổi {i + 1}: hãy nhập lương gross mỗi tháng, hoặc bỏ giai đoạn này.");
        }
        for (var i = 0; i < Exceptions.Count; i++)
        {
            var e = Exceptions[i];
            void Check(string field, bool invalid, string message)
            {
                var key = $"Exceptions[{i}].{field}";
                if (invalid && !ModelState.ContainsKey(key)) ModelState.AddModelError(key, message);
                else if (invalid && ModelState[key]?.Errors.Count == 0) ModelState.AddModelError(key, message);
                if (ModelState[key]?.Errors.Count > 0)
                {
                    var label = field switch { "Gross" => "Gross", "Insurance" => "Bảo hiểm", "Dependents" => "Người phụ thuộc", "Withheld" => "Khấu trừ", _ => field };
                    ModelState.AddModelError(string.Empty, $"Tháng {i + 1} — {label}: {message}");
                    if (SelectedMonth == 0 || !ModelState.Keys.Any(k => k.StartsWith($"Exceptions[{SelectedMonth - 1}].") && ModelState[k]?.Errors.Count > 0))
                    { SelectedMonth = i + 1; ModelState.Remove(nameof(SelectedMonth)); }
                }
            }
            Check("Gross", e.Gross < 0 || e.Gross is not null && e.Status == ForecastStatus.Assumed && string.IsNullOrWhiteSpace(e.Note), "Kiểm tra số không âm và lý do giả định.");
            Check("Insurance", e.Insurance < 0 || e.Insurance is not null && e.InsuranceStatus == ForecastStatus.Assumed && string.IsNullOrWhiteSpace(e.InsuranceNote), "Kiểm tra số không âm và lý do giả định.");
            Check("Dependents", e.Dependents < 0 || e.Dependents is not null && e.DependentsStatus == ForecastStatus.Assumed && string.IsNullOrWhiteSpace(e.DependentsNote), "Kiểm tra số không âm và lý do giả định.");
            Check("Withheld", e.Withheld < 0 || e.Withheld is not null && e.WithheldStatus == ForecastStatus.Assumed && string.IsNullOrWhiteSpace(e.WithheldNote), "Kiểm tra số không âm và lý do giả định.");
        }
        if (!ModelState.IsValid || Gross < 0 || FromMonth is < 1 or > 12 ||
            ToMonth is < 1 or > 12 || FromMonth > ToMonth || !Enum.IsDefined(Status) ||
            Gross is not null && Status == ForecastStatus.Assumed && string.IsNullOrWhiteSpace(Note))
        {
            ModelState.AddModelError(string.Empty, "Hãy nhập tháng hợp lệ, mức lương không âm và lý do nếu là giả định.");
            EnsureExceptionSlots();
            return Page();
        }
        if (Exceptions.Count != 12 || Exceptions.Select(e => e.Month).Order().SequenceEqual(Enumerable.Range(1, 12)) == false ||
            Exceptions.Any(e => e.Gross < 0 || !Enum.IsDefined(e.Status) || e.Gross is not null && e.Status == ForecastStatus.Assumed && string.IsNullOrWhiteSpace(e.Note) ||
                e.Insurance < 0 || !Enum.IsDefined(e.InsuranceStatus) || e.Insurance is not null && e.InsuranceStatus == ForecastStatus.Assumed && string.IsNullOrWhiteSpace(e.InsuranceNote) ||
                e.Dependents < 0 || !Enum.IsDefined(e.DependentsStatus) || e.Dependents is not null && e.DependentsStatus == ForecastStatus.Assumed && string.IsNullOrWhiteSpace(e.DependentsNote) ||
                e.Withheld < 0 || !Enum.IsDefined(e.WithheldStatus) || e.Withheld is not null && e.WithheldStatus == ForecastStatus.Assumed && string.IsNullOrWhiteSpace(e.WithheldNote)) ||
            Periods.Count > 11 || Periods.Any(p => p.FromMonth is < 1 or > 12 || p.ToMonth is < 1 or > 12 || p.FromMonth > p.ToMonth || p.Gross is null or < 0 || !Enum.IsDefined(p.Status) || p.Status == ForecastStatus.Assumed && string.IsNullOrWhiteSpace(p.Note)))
        {
            ModelState.AddModelError(string.Empty, "Kiểm tra tháng ngoại lệ, giá trị và lý do giả định.");
            EnsureExceptionSlots();
            return Page();
        }
        var now = DateTimeOffset.UtcNow;
        InsurancePreview = Exceptions.ToDictionary(e => e.Month, e => e.Insurance is null ? null :
            new ForecastValue<decimal>(e.Insurance.Value, e.InsuranceStatus, e.InsuranceSource, e.InsuranceNote, now));
        DependentsPreview = Exceptions.ToDictionary(e => e.Month, e => e.Dependents is null ? null :
            new ForecastValue<int>(e.Dependents.Value, e.DependentsStatus, e.DependentsSource, e.DependentsNote, now));
        WithheldPreview = Exceptions.ToDictionary(e => e.Month, e => e.Withheld is null ? null :
            new ForecastValue<decimal>(e.Withheld.Value, e.WithheldStatus, e.WithheldSource, e.WithheldNote, now));
        var schedule = new SalarySchedule(2026);
        foreach (var item in Exceptions.Where(e => e.Gross is not null))
            schedule = schedule.WithException(item.Month, new ForecastValue<decimal>(item.Gross!.Value, item.Status, item.Source, item.Note, now));
        var conflicts = new HashSet<int>();
        var replaced = new HashSet<int>();
        if (Gross is not null)
        {
            var value = new ForecastValue<decimal>(Gross.Value, Status, Source, Note, now);
            conflicts.UnionWith(schedule.PreviewPeriod(FromMonth, ToMonth, value).ConflictingMonths);
            schedule = schedule.ApplyPeriod(FromMonth, ToMonth, value);
        }
        foreach (var period in Periods)
        {
            var next = new ForecastValue<decimal>(period.Gross!.Value, period.Status, period.Source, period.Note, now);
            var preview = schedule.PreviewPeriod(period.FromMonth, period.ToMonth, next);
            for (var month = period.FromMonth; month <= period.ToMonth; month++)
                if (schedule.Months[month] is not null && !preview.ConflictingMonths.Contains(month)) replaced.Add(month);
            conflicts.UnionWith(preview.ConflictingMonths);
            schedule = schedule.ApplyPeriod(period.FromMonth, period.ToMonth, next);
        }
        Preview = new SalarySchedulePreview(schedule.Months, conflicts.Order().ToArray());
        ReplacedPeriodMonths = replaced.Order().ToArray();
        return Page();
    }

    private void EnsureExceptionSlots()
    {
        while (Exceptions.Count < 12) Exceptions.Add(new ExceptionInput { Month = Exceptions.Count + 1 });
    }
}
