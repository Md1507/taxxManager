namespace TaxManager.Core;

public enum MultipleSalaryScope { Unspecified, VietnameseResidentCalendarYearDomesticVndOnlyOrdinarySalary }
public enum SalaryReceiptSubject { Unspecified, OrdinaryEmploymentFullyTaxableGross }
public enum MultipleSalaryTiming { Unspecified, ReceivedOrPaidDate }

public sealed class MultipleSalaryRuleSet
{
    public string Id { get; }
    public string Version { get; }
    public int Year { get; }
    public DateOnly PeriodFrom { get; }
    public DateOnly PeriodTo { get; }
    public MultipleSalaryScope Scope { get; }
    public SalaryReceiptSubject Subject { get; }
    public MultipleSalaryTiming Timing { get; }
    public LegalBasis AggregationBasis { get; }
    public LegalBasis InsuranceDeductionBasis { get; }
    public LegalBasis PersonalDeductionOnceBasis { get; }
    public LegalBasis DependentDeductionOnceBasis { get; }
    public LegalBasis TimingBasis { get; }
    public LegalBasis PeriodBasis { get; }
    public LegalBasis ApplicabilityBasis { get; }
    public LegalBasis TransitionBasis { get; }
    public LegalBasis OverpaymentContextBasis { get; }

    public MultipleSalaryRuleSet(string id, string version, int year, DateOnly periodFrom, DateOnly periodTo,
        MultipleSalaryScope scope, SalaryReceiptSubject subject, MultipleSalaryTiming timing,
        LegalBasis aggregationBasis, LegalBasis insuranceDeductionBasis, LegalBasis personalDeductionOnceBasis,
        LegalBasis dependentDeductionOnceBasis, LegalBasis timingBasis, LegalBasis periodBasis,
        LegalBasis applicabilityBasis, LegalBasis transitionBasis, LegalBasis overpaymentContextBasis)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id); ArgumentException.ThrowIfNullOrWhiteSpace(version);
        if (year != 2026 || periodFrom.Year != year || periodTo.Year != year || periodFrom > periodTo) throw new ArgumentException("Invalid coverage.");
        if (scope != MultipleSalaryScope.VietnameseResidentCalendarYearDomesticVndOnlyOrdinarySalary || subject != SalaryReceiptSubject.OrdinaryEmploymentFullyTaxableGross || timing != MultipleSalaryTiming.ReceivedOrPaidDate) throw new ArgumentException("Unsupported scope, subject or timing.");
        foreach (var basis in new[] { aggregationBasis, insuranceDeductionBasis, personalDeductionOnceBasis, dependentDeductionOnceBasis, timingBasis, periodBasis, applicabilityBasis, transitionBasis, overpaymentContextBasis })
        {
            ArgumentNullException.ThrowIfNull(basis);
            AnnualSalaryRuleSet.CheckBasis(basis, year.ToString());
            if (basis.Version != version || !Uri.TryCreate(basis.SourceUrl, UriKind.Absolute, out var url) || url.Scheme != Uri.UriSchemeHttps || basis.EffectiveFrom > periodFrom || basis.EffectiveTo < periodTo) throw new ArgumentException("Invalid basis version, URL or coverage.");
        }
        Id = id; Version = version; Year = year; PeriodFrom = periodFrom; PeriodTo = periodTo; Scope = scope; Subject = subject; Timing = timing;
        AggregationBasis = aggregationBasis; InsuranceDeductionBasis = insuranceDeductionBasis; PersonalDeductionOnceBasis = personalDeductionOnceBasis; DependentDeductionOnceBasis = dependentDeductionOnceBasis; TimingBasis = timingBasis; PeriodBasis = periodBasis; ApplicabilityBasis = applicabilityBasis; TransitionBasis = transitionBasis; OverpaymentContextBasis = overpaymentContextBasis;
    }

    internal static MultipleSalaryRuleSet Vietnam2026()
    {
        const string url = "https://datafiles.chinhphu.vn/cpp/files/vbpq/2026/7/253m-ndcp.signed.pdf";
        LegalBasis B(string id, string article, string clause, string? point, string summary) => new(VerificationStatus.Unverified, id, "1", point: point, clause: clause, article: article, documentNumber: "253/2026/ND-CP", summary: summary, sourceUrl: url, period: "2026");
        return new("multiple-salary-2026", "1", 2026, new(2026, 1, 1), new(2026, 12, 31), MultipleSalaryScope.VietnameseResidentCalendarYearDomesticVndOnlyOrdinarySalary, SalaryReceiptSubject.OrdinaryEmploymentFullyTaxableGross, MultipleSalaryTiming.ReceivedOrPaidDate,
            B("aggregation", "46", "1,2", null, "Thu nhập tính thuế dựa trên tổng thu nhập chịu thuế trong kỳ. Các khoản được trừ được tính ở cấp cá nhân."),
            B("insurance", "46", "2", "a", "Khoản bảo hiểm được trừ theo quy định. Người gọi xác nhận số thực tế đủ điều kiện."),
            B("personal", "48", "1", "a,b", "Giảm trừ bản thân chỉ tính một nơi cùng thời điểm. Quyết toán trong phạm vi này tính đủ mười hai tháng."),
            B("dependent", "48", "2", "c", "Thu nhập nhiều nguồn được tổng hợp. Mỗi người phụ thuộc chỉ giảm trừ một lần cho một người nộp thuế."),
            B("timing", "46", "3", null, "Thời điểm xác định thu nhập là khi trả hoặc nhận. Ngày thực nhận xác định tháng trong sổ."),
            B("period", "66", "1", "a", "Kỳ tính thuế là năm dương lịch. Sổ bao phủ toàn bộ năm."),
            B("applicability", "69", "1", "a", "Văn bản có hiệu lực từ ngày 1 tháng 7 năm 2026. Thu nhập tiền lương áp dụng cho kỳ thuế năm 2026."),
            B("transition", "70", "2", null, "Quy định chuyển tiếp áp dụng cho khai tháng hoặc quý. Không suy lại số khấu trừ thực tế."),
            B("overpayment", "68", "1,2,4", null, "Số nộp thừa và thủ tục được xét riêng. Chênh lệch ước tính không xác định quyền hoàn."));
    }
}
