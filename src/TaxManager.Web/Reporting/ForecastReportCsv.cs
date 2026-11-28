using System.Globalization;
using System.Text;
using TaxManager.Core.Forecasting;
using TaxManager.Data.TaxRules;

namespace TaxManager.Web.Reporting;

public static class ForecastReportCsv
{
    public static string Status(ForecastStatus? status) => status switch
    {
        ForecastStatus.Actual => "Thực tế", ForecastStatus.Planned => "Dự kiến",
        ForecastStatus.Assumed => "Giả định", _ => "Chưa nhập"
    };

    public static byte[] Write(ForecastReport report)
    {
        var output = new StringBuilder();
        void Row(params string?[] cells) => output.AppendJoin(',', cells.Select((cell, index) =>
            "\"" + (index is 7 or 8 ? SafeText(cell) : cell ?? "").Replace("\"", "\"\"") + "\"")).Append("\r\n");
        var year = report.Input.Year.ToString(CultureInfo.InvariantCulture);
        var revision = report.Revision.ToString(CultureInfo.InvariantCulture);
        Row("Năm", "Phiên bản hồ sơ", "Tháng/kỳ", "Chỉ tiêu", "Giá trị", "Đơn vị", "Trạng thái", "Nguồn", "Ghi chú", "Ngày nhập UTC", "Đã gồm gross");
        Row(year, revision, "Cả năm", "Phạm vi báo cáo", "", "", "Tham khảo", "TaxManager",
            "Dữ liệu đã lưu; không phải tờ khai chính thức hoặc bản sao lưu. Trạng thái nguồn giữ nguyên theo bản lưu.", "", "");
        foreach (var value in report.Values)
            Row(year, revision, value.Month.ToString(CultureInfo.InvariantCulture), value.Label,
                Number(value.Amount), value.Unit, Status(value.Status), value.Source, value.Note,
                value.EnteredAt?.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
                value.IncludedInGross is null ? "" : value.IncludedInGross.Value ? "Đã gồm gross" : "Cộng thêm vào gross");
        if (report.Calculation is { } saved)
        {
            Row(year, revision, "Cả năm", "Bản tính năm", saved.HistoryId.ToString(), "", "Bản lưu",
                "TaxManager", $"Engine {saved.EngineVersion}; snapshot đầu vào {saved.InputSnapshotId}; schema {saved.SchemaVersion}",
                saved.CreatedAtUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture), "");
            void Result(string label, decimal? amount, string? state = null) =>
                Row(year, revision, "Cả năm", label, Number(amount), "đ", state ?? "Kết quả năm đã lưu",
                    "TaxManager", "", saved.CreatedAtUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture), "");
            Result("Nghĩa vụ thuế năm ước tính", saved.Result.Obligation);
            Result("Tổng gross chịu thuế", saved.Result.GrossIncome);
            Result("Thưởng cộng thêm", saved.Result.BonusAdded);
            Result("Thưởng đã gồm gross", saved.Result.BonusAlreadyInGross);
            Result("Bảo hiểm năm", saved.Result.Insurance);
            Result("Giảm trừ bản thân", saved.Result.PersonalDeduction);
            Result("Giảm trừ người phụ thuộc", saved.Result.DependentDeduction);
            Result("Thu nhập tính thuế", saved.Result.TaxableIncome);
            Result("Khấu trừ thực tế đã nhập", saved.Result.ActualWithheld);
            Result("Khấu trừ dự kiến/giả định đã nhập", saved.Result.ForecastWithheld);
            Result("Chênh lệch nghĩa vụ trừ khấu trừ", saved.Result.Difference,
                saved.Result.Difference is null ? "Chưa đủ khấu trừ" : "Kết quả năm đã lưu");
            foreach (var registration in saved.Rules)
            {
                var rule = AnnualRuleGraphCodec.Decode(registration.Graph);
                var bases = new[] { rule.TaxBasis, rule.PersonalDeductionBasis, rule.DependentDeductionBasis,
                    rule.TaxableIncomeBasis, rule.RoundingPolicy!.Basis, rule.AnnualSalaryRules!.TaxRules.TaxBasis,
                    rule.AnnualSalaryRules.PeriodBasis, rule.AnnualSalaryRules.TransitionBasis,
                    rule.AnnualSalaryRules.SettlementBasis, rule.AnnualSalaryRules.DependentPeriodBasis,
                    rule.CashBonusRules!.TaxableBasis, rule.CashBonusRules.TimingBasis }.Distinct();
                foreach (var basis in bases)
                    Row(year, revision, $"{registration.PeriodFrom:yyyy-MM-dd}/{registration.PeriodTo:yyyy-MM-dd}",
                        $"Luật {rule.Id}, phiên bản {rule.Version}", "", "", basis.Status.ToString(), basis.SourceUrl,
                        $"Căn cứ {basis.Id}, phiên bản {basis.Version}; {basis.DocumentNumber}; điều {basis.Article}; khoản {basis.Clause}; điểm {basis.Point}; kỳ {basis.Period}; hiệu lực {basis.EffectiveFrom:yyyy-MM-dd}/{basis.EffectiveTo:yyyy-MM-dd}; xác minh {basis.VerifiedOn:yyyy-MM-dd}; {basis.Summary}", "", "");
            }
        }
        else Row(year, revision, "Cả năm", "Kết quả năm", "", "", "Chưa có kết quả năm đã lưu", "", "", "", "");
        var encoding = new UTF8Encoding(true);
        return encoding.GetPreamble().Concat(encoding.GetBytes(output.ToString())).ToArray();
    }

    private static string Number(decimal? value) => value?.ToString(CultureInfo.InvariantCulture) ?? "";

    private static string SafeText(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        var start = 0;
        while (start < text.Length && (char.IsWhiteSpace(text[start]) || char.IsControl(text[start]))) start++;
        var first = start < text.Length ? text[start] : '\0';
        return first is '=' or '+' or '-' or '@' || text.Take(start).Any(char.IsControl) ? "'" + text : text;
    }
}
