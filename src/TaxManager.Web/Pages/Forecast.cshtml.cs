using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TaxManager.Core.Forecasting;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using TaxManager.Data.Forecasting;
using TaxManager.Data.Protection;
using TaxManager.Web.Demo;

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
        else if (MoneyInput.TryParse(text, context.ValueProvider.GetValue("AmountFormat").FirstValue == MoneyInput.GroupedFormat, out var value))
            context.Result = ModelBindingResult.Success(value);
        else
        {
            var grouped = context.ValueProvider.GetValue("AmountFormat").FirstValue == MoneyInput.GroupedFormat;
            context.ModelState.TryAddModelError(context.ModelName, grouped
                ? "Số tiền chưa đúng định dạng. Dùng dấu chấm phân cách hàng nghìn và dấu phẩy cho phần lẻ, ví dụ 30.000.000,50."
                : "Nhập số tiền không âm, không dùng dấu phân cách hàng nghìn (ví dụ 30000000,50).");
            context.Result = ModelBindingResult.Success(null);
        }
        return Task.CompletedTask;
    }
}

public sealed class ForecastModel : PageModel
{
    private readonly IDataProtector draftProtector;
    private readonly TimeProvider clock;
    private readonly TaxManager.Web.Backup.SelectedDatabase database;
    public ForecastModel(IDataProtectionProvider protection, TimeProvider clock, TaxManager.Web.Backup.SelectedDatabase database)
    {
        draftProtector = protection.CreateProtector("TaxManager.Forecast.EntryDraft.v1");
        this.clock = clock;
        this.database = database;
    }
    private sealed record EntryStamp(string Fingerprint, DateTimeOffset EnteredAt);
    [BindProperty] public string? DraftToken { get; set; }
    [BindProperty] public Guid? ProfileId { get; set; }
    [BindProperty] public int Revision { get; set; }
    public AnnualCalculationSnapshot? AnnualResult { get; private set; }
    public ForecastCompleteness? Completeness { get; private set; }
    public IReadOnlyList<AnnualCalculationSnapshot> AnnualHistory { get; private set; } = [];
    public bool SavedInputConfirmed { get; private set; }
    private sealed record SavedDraft(int FromMonth, int ToMonth, decimal? Gross, ForecastStatus Status,
        string? Source, string? Note, List<PeriodInput> Periods, List<ExceptionInput> Exceptions, string Stamps,
        List<BonusInput>? Bonuses = null, List<DependentPeriodInput>? DependentPeriods = null);
    public sealed class BonusInput
    {
        public int Month { get; set; } = 12;
        [ModelBinder(BinderType = typeof(ForecastAmountBinder))] public decimal? Amount { get; set; }
        public ForecastStatus Status { get; set; } = ForecastStatus.Planned;
        public string? Source { get; set; }
        public string? Note { get; set; }
        public bool IncludedInGross { get; set; }
    }
    [BindProperty] public List<BonusInput> Bonuses { get; set; } = [new()];
    public sealed class DependentPeriodInput
    {
        public int FromMonth { get; set; } = 1;
        public int ToMonth { get; set; } = 12;
        public int? Count { get; set; }
        public ForecastStatus Status { get; set; } = ForecastStatus.Planned;
        public string? Source { get; set; }
        public string? Note { get; set; }
    }
    [BindProperty] public List<DependentPeriodInput> DependentPeriods { get; set; } = [new()];
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
    public IReadOnlyList<ForecastBonus> BonusPreview { get; private set; } = [];
    public IReadOnlyList<ForecastDependentPeriod> DependentPeriodPreview { get; private set; } = [];
    public IReadOnlyList<int> ReplacedPeriodMonths { get; private set; } = [];

    public async Task OnGetAsync(Guid? id, int? month)
    {
        try
        {
            await using var context = database.Open();
            var store = new ForecastStore(context);
            // Confirm the existing store is readable, including when starting a new profile.
            await store.ListAsync();
            if (id is null) return;
            var document = await store.ReadAsync(id.Value);
            if (document is null) { ModelState.AddModelError(string.Empty, "Không tìm thấy hồ sơ này."); return; }
            var draft = JsonSerializer.Deserialize<SavedDraft>(document.Payload!) ?? throw new JsonException();
            if (draft.Periods is null || draft.Exceptions is null || draft.Stamps is null) throw new JsonException("Unsupported editor draft.");
            FromMonth = draft.FromMonth; ToMonth = draft.ToMonth; Gross = draft.Gross; Status = draft.Status;
            Source = draft.Source; Note = draft.Note; Periods = draft.Periods; Exceptions = draft.Exceptions;
            Bonuses = draft.Bonuses is { Count: > 0 } ? draft.Bonuses : [new()];
            DependentPeriods = draft.DependentPeriods is { Count: > 0 } ? draft.DependentPeriods : [new()];
            DraftToken = draftProtector.Protect(draft.Stamps);
            ProfileId = document.Id; Revision = document.Revision;
            SavedInputConfirmed = true;
            AnnualHistory = await new AnnualForecastService(context).HistoryAsync(document.Id);
            EnsureExceptionSlots();
            if (month is >= 1 and <= 12) SelectedMonth = month.Value;
            BuildPreview(requireSeparatePeriods: false);
        }
        catch (Exception ex) when (ex is DataProtectionException or JsonException or DbUpdateException)
        { ModelState.AddModelError(string.Empty, "Không thể mở hồ sơ: kiểm tra khóa bảo vệ và dữ liệu."); }
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        OnPostPreview();
        if (!ModelState.IsValid || Preview is null) return Page();
        try
        {
            var stamps = draftProtector.Unprotect(DraftToken!);
            var payload = JsonSerializer.Serialize(new SavedDraft(FromMonth, ToMonth, Gross, Status, Source, Note, Periods, Exceptions, stamps, Bonuses, DependentPeriods));
            await using var context = database.Open();
            var store = new ForecastStore(context);
            var id = ProfileId ?? await store.CreateAsync(payload);
            if (ProfileId is not null) await store.UpdateAsync(id, Revision, payload);
            return RedirectToPage("/Forecast", new { id, month = SelectedMonth > 0 ? (int?)SelectedMonth : null });
        }
        catch (Exception ex) when (ex is DataProtectionException or InvalidOperationException or DbUpdateException or Microsoft.Data.Sqlite.SqliteException)
        {
            ModelState.AddModelError(string.Empty, "Không lưu được hồ sơ; có thể dữ liệu đã thay đổi hoặc khóa bảo vệ không hợp lệ. Hãy mở lại và kiểm tra.");
            return Page();
        }
    }

    public async Task<IActionResult> OnPostCalculateAsync()
    {
        BuildPreview(requireSeparatePeriods: false);
        if (!ModelState.IsValid || Preview is null) return Page();
        if (ProfileId is null)
        {
            ModelState.AddModelError(string.Empty, "Hãy lưu hồ sơ trước khi tính năm.");
            return Page();
        }
        try
        {
            await using var context = database.Open();
            context.Clock = clock;
            var document = await new ForecastStore(context).ReadAsync(ProfileId.Value);
            if (document is null || document.Revision != Revision)
                throw new InvalidOperationException("Hồ sơ đã thay đổi; hãy mở lại.");
            var stored = JsonSerializer.Deserialize<SavedDraft>(document.Payload!) ?? throw new JsonException();
            var current = new SavedDraft(FromMonth, ToMonth, Gross, Status, Source, Note, Periods, Exceptions, "", Bonuses, DependentPeriods);
            var comparable = stored with { Stamps = "", Bonuses = stored.Bonuses is { Count: > 0 } ? stored.Bonuses : [new()],
                DependentPeriods = stored.DependentPeriods is { Count: > 0 } ? stored.DependentPeriods : [new()] };
            if (JsonSerializer.Serialize(current) != JsonSerializer.Serialize(comparable))
            {
                ModelState.AddModelError(string.Empty, "Bạn có thay đổi chưa lưu. Hãy lưu hồ sơ trước khi tính năm.");
                return Page();
            }
            SavedInputConfirmed = true;
            var attempt = await new AnnualForecastService(context).CalculateAsync(ProfileId.Value, Revision);
            Completeness = attempt.Completeness;
            AnnualResult = attempt.Snapshot;
            AnnualHistory = await new AnnualForecastService(context).HistoryAsync(ProfileId.Value);
        }
        catch (Exception ex) when (ex is DataProtectionException or InvalidOperationException or ArgumentException or
            JsonException or NotSupportedException or DbUpdateException or Microsoft.Data.Sqlite.SqliteException)
        {
            ModelState.AddModelError(string.Empty, "Chưa tính được năm: hồ sơ có thể đã thay đổi, thiếu luật hoặc kỳ áp dụng, hoặc dữ liệu/khóa không hợp lệ. Hãy mở lại và kiểm tra.");
        }
        return Page();
    }

    public static string FormatAmount(decimal amount) => amount.ToString("N" + ((decimal.GetBits(amount)[3] >> 16) & 0x7f), CultureInfo.GetCultureInfo("vi-VN"));
    public static string LocalTime(DateTimeOffset value) => value.ToOffset(TimeSpan.FromHours(7))
        .ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("vi-VN"));

    public static bool HasCompleteInputs(ForecastDocument document)
    {
        var input = ForecastDraftReader.Read(document.Payload!);
        var completeness = AnnualForecastCalculator.Assess(input.Months, input.Bonuses);
        return completeness.MissingObligation.Count == 0 && completeness.MissingWithholdingMonths.Count == 0;
    }

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

    public IActionResult OnPostAddBonus()
    {
        EnsureExceptionSlots();
        if (Bonuses.Count < 100) Bonuses.Add(new BonusInput());
        return Page();
    }
    public IActionResult OnPostAddDependentPeriod()
    {
        EnsureExceptionSlots();
        if (DependentPeriods.Count < 12) DependentPeriods.Add(new DependentPeriodInput());
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
        if (!string.IsNullOrEmpty(DraftToken))
        {
            try
            {
                var stamps = JsonSerializer.Deserialize<Dictionary<string, EntryStamp>>(draftProtector.Unprotect(DraftToken))
                    ?? throw new CryptographicException("Draft is empty.");
                var shifted = new Dictionary<string, EntryStamp>();
                foreach (var stamp in stamps)
                {
                    var match = Regex.Match(stamp.Key, @"^period/(\d+)$");
                    if (!match.Success || !int.TryParse(match.Groups[1].Value, out var oldIndex))
                        shifted[stamp.Key] = stamp.Value;
                    else if (oldIndex != index)
                        shifted[$"period/{(oldIndex > index ? oldIndex - 1 : oldIndex)}"] = stamp.Value;
                }
                DraftToken = draftProtector.Protect(JsonSerializer.Serialize(shifted));
                ModelState.Remove(nameof(DraftToken));
            }
            catch (Exception ex) when (ex is CryptographicException or JsonException or FormatException)
            {
                ModelState.AddModelError(string.Empty, "Không thể đọc bản nháp; hãy mở lại hồ sơ để tiếp tục.");
                return Page();
            }
        }
        Periods.RemoveAt(index);
        return Page();
    }

    public IActionResult OnPostPreview() => BuildPreview(requireSeparatePeriods: true);

    private IActionResult BuildPreview(bool requireSeparatePeriods)
    {
        EnsureExceptionSlots();
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
        if (requireSeparatePeriods)
        {
            var occupied = new HashSet<int>();
            if (Gross is not null && FromMonth is >= 1 and <= 12 && ToMonth >= FromMonth && ToMonth <= 12)
                occupied.UnionWith(Enumerable.Range(FromMonth, ToMonth - FromMonth + 1));
            for (var i = 0; i < Periods.Count; i++)
            {
                var p = Periods[i];
                if (p.Gross is null || p.FromMonth is < 1 or > 12 || p.ToMonth < p.FromMonth || p.ToMonth > 12) continue;
                var months = Enumerable.Range(p.FromMonth, p.ToMonth - p.FromMonth + 1).ToArray();
                var overlap = months.Where(occupied.Contains).ToArray();
                if (overlap.Length > 0)
                    ModelState.AddModelError($"Periods[{i}].FromMonth", $"Hai giai đoạn cùng áp dụng cho tháng {string.Join(", ", overlap)}. Sửa khoảng tháng trước khi lưu.");
                occupied.UnionWith(months);
            }
        }
        for (var i = 0; i < Bonuses.Count; i++)
        {
            var bonus = Bonuses[i];
            if (bonus.Amount is null && (bonus.Source is not null || bonus.Note is not null))
                ModelState.AddModelError($"Bonuses[{i}].Amount", "Hãy nhập số tiền thưởng hoặc bỏ thông tin thưởng.");
            if (bonus.Amount is not null && (bonus.Amount < 0 || bonus.Month is < 1 or > 12 ||
                bonus.Status is not (ForecastStatus.Actual or ForecastStatus.Planned)))
                ModelState.AddModelError($"Bonuses[{i}].Amount", "Thưởng cần số tiền không âm, tháng hợp lệ và trạng thái thực tế hoặc dự kiến.");
        }
        if (Bonuses.Count > 100) ModelState.AddModelError(string.Empty, "Quá nhiều khoản thưởng.");
        for (var i = 0; i < DependentPeriods.Count; i++)
        {
            var period = DependentPeriods[i];
            if (period.Count is null && (period.Source is not null || period.Note is not null))
                ModelState.AddModelError($"DependentPeriods[{i}].Count", "Hãy nhập số người hoặc bỏ thông tin giai đoạn.");
            if (period.Count is not null && (period.Count < 0 || period.FromMonth is < 1 or > 12 ||
                period.ToMonth is < 1 or > 12 || period.FromMonth > period.ToMonth || !Enum.IsDefined(period.Status) ||
                period.Status == ForecastStatus.Assumed && string.IsNullOrWhiteSpace(period.Note)))
                ModelState.AddModelError($"DependentPeriods[{i}].Count", "Kiểm tra số người, khoảng tháng và lý do giả định.");
        }
        var populatedDependentPeriods = DependentPeriods.Where(p => p.Count is not null).ToArray();
        if (DependentPeriods.Count > 12 || populatedDependentPeriods.SelectMany(p =>
            p.FromMonth is >= 1 and <= 12 && p.ToMonth >= p.FromMonth && p.ToMonth <= 12
                ? Enumerable.Range(p.FromMonth, p.ToMonth - p.FromMonth + 1) : []).GroupBy(m => m).Any(g => g.Count() > 1))
            ModelState.AddModelError(string.Empty, "Các giai đoạn người phụ thuộc không được trùng tháng.");
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
        Dictionary<string, EntryStamp> previous;
        try
        {
            previous = string.IsNullOrEmpty(DraftToken) ? [] :
                JsonSerializer.Deserialize<Dictionary<string, EntryStamp>>(draftProtector.Unprotect(DraftToken))
                ?? throw new CryptographicException("Draft is empty.");
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException or FormatException)
        {
            ModelState.AddModelError(string.Empty, "Không thể đọc bản nháp; hãy mở lại hồ sơ để tiếp tục.");
            return Page();
        }
        var now = clock.GetUtcNow().ToUniversalTime();
        var current = new Dictionary<string, EntryStamp>();
        DateTimeOffset Stamp(string key, object value)
        {
            var fingerprint = JsonSerializer.Serialize(value);
            var entered = previous.TryGetValue(key, out var old) && old.Fingerprint == fingerprint ? old.EnteredAt : now;
            current[key] = new EntryStamp(fingerprint, entered);
            return entered;
        }
        var bonusPreview = new List<ForecastBonus>();
        for (var i = 0; i < Bonuses.Count; i++)
            if (Bonuses[i].Amount is { } amount)
            {
                var bonus = Bonuses[i];
                bonusPreview.Add(new ForecastBonus($"bonus/{i}", bonus.Month,
                    new ForecastValue<decimal>(amount, bonus.Status, bonus.Source, bonus.Note, Stamp($"bonus/{i}", bonus)), bonus.IncludedInGross));
            }
        BonusPreview = bonusPreview.AsReadOnly();
        var dependentPreview = new List<ForecastDependentPeriod>();
        for (var i = 0; i < DependentPeriods.Count; i++)
            if (DependentPeriods[i].Count is { } count)
            {
                var period = DependentPeriods[i];
                dependentPreview.Add(new ForecastDependentPeriod(period.FromMonth, period.ToMonth,
                    new ForecastValue<int>(count, period.Status, period.Source, period.Note, Stamp($"dependent-period/{i}", period))));
            }
        DependentPeriodPreview = dependentPreview.AsReadOnly();
        InsurancePreview = Exceptions.ToDictionary(e => e.Month, e => e.Insurance is null ? null :
            new ForecastValue<decimal>(e.Insurance.Value, e.InsuranceStatus, e.InsuranceSource, e.InsuranceNote, Stamp($"{e.Month}/insurance", new { e.Insurance, e.InsuranceStatus, e.InsuranceSource, e.InsuranceNote })));
        DependentsPreview = Exceptions.ToDictionary(e => e.Month, e => e.Dependents is null ? null :
            new ForecastValue<int>(e.Dependents.Value, e.DependentsStatus, e.DependentsSource, e.DependentsNote, Stamp($"{e.Month}/dependents", new { e.Dependents, e.DependentsStatus, e.DependentsSource, e.DependentsNote })));
        DependentsPreview = ForecastDependentPeriod.Apply(Enumerable.Range(1, 12)
            .Select(month => new ForecastMonth(month, null, null, DependentsPreview[month], null)),
            DependentPeriodPreview).ToDictionary(month => month.Month, month => month.Dependents);
        WithheldPreview = Exceptions.ToDictionary(e => e.Month, e => e.Withheld is null ? null :
            new ForecastValue<decimal>(e.Withheld.Value, e.WithheldStatus, e.WithheldSource, e.WithheldNote, Stamp($"{e.Month}/withheld", new { e.Withheld, e.WithheldStatus, e.WithheldSource, e.WithheldNote })));
        var schedule = new SalarySchedule(2026);
        foreach (var item in Exceptions.Where(e => e.Gross is not null))
            schedule = schedule.WithException(item.Month, new ForecastValue<decimal>(item.Gross!.Value, item.Status, item.Source, item.Note, Stamp($"{item.Month}/gross", new { item.Gross, item.Status, item.Source, item.Note })));
        var conflicts = new HashSet<int>();
        var replaced = new HashSet<int>();
        if (Gross is not null)
        {
            var value = new ForecastValue<decimal>(Gross.Value, Status, Source, Note, Stamp("base", new { Gross, Status, Source, Note, FromMonth, ToMonth }));
            conflicts.UnionWith(schedule.PreviewPeriod(FromMonth, ToMonth, value).ConflictingMonths);
            schedule = schedule.ApplyPeriod(FromMonth, ToMonth, value);
        }
        for (var periodIndex = 0; periodIndex < Periods.Count; periodIndex++)
        {
            var period = Periods[periodIndex];
            var next = new ForecastValue<decimal>(period.Gross!.Value, period.Status, period.Source, period.Note, Stamp($"period/{periodIndex}", period));
            var preview = schedule.PreviewPeriod(period.FromMonth, period.ToMonth, next);
            for (var month = period.FromMonth; month <= period.ToMonth; month++)
                if (schedule.Months[month] is not null && !preview.ConflictingMonths.Contains(month)) replaced.Add(month);
            conflicts.UnionWith(preview.ConflictingMonths);
            schedule = schedule.ApplyPeriod(period.FromMonth, period.ToMonth, next);
        }
        Preview = new SalarySchedulePreview(schedule.Months, conflicts.Order().ToArray());
        DraftToken = draftProtector.Protect(JsonSerializer.Serialize(current));
        ModelState.Remove(nameof(DraftToken));
        ReplacedPeriodMonths = replaced.Order().ToArray();
        return Page();
    }

    private void EnsureExceptionSlots()
    {
        // Legacy RemovePeriod posts use an unprefixed `index`. MVC can bind that
        // index to absent optional collections, producing a null empty slot.
        for (var i = 0; i < Bonuses.Count; i++) Bonuses[i] ??= new();
        for (var i = 0; i < DependentPeriods.Count; i++) DependentPeriods[i] ??= new();
        while (Exceptions.Count < 12) Exceptions.Add(new ExceptionInput { Month = Exceptions.Count + 1 });
    }
}
