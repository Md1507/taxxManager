using System.Collections.ObjectModel;

namespace TaxManager.Core;

public enum VerificationStatus { Missing, Unverified, Verified }

public sealed record LegalBasis
{
    public string? Id { get; }
    public string? Version { get; }
    public string? Point { get; }
    public string? Clause { get; }
    public string? Article { get; }
    public string? DocumentNumber { get; }
    public string? Summary { get; }
    public string? SourceUrl { get; }
    public string? Period { get; }
    public DateOnly? EffectiveFrom { get; }
    public DateOnly? EffectiveTo { get; }
    public VerificationStatus Status { get; }
    public DateOnly? VerifiedOn { get; }

    public LegalBasis(VerificationStatus status, string? id = null, string? version = null,
        string? point = null, string? clause = null, string? article = null, string? documentNumber = null,
        string? summary = null, string? sourceUrl = null, string? period = null,
        DateOnly? effectiveFrom = null, DateOnly? effectiveTo = null, DateOnly? verifiedOn = null)
    {
        if (!Enum.IsDefined(status)) throw new ArgumentOutOfRangeException(nameof(status));
        if (effectiveFrom > effectiveTo) throw new ArgumentException("Effective dates are reversed.");
        if (status != VerificationStatus.Verified && verifiedOn != null)
            throw new ArgumentException("Only verified sources can have a verification date.");
        if (status == VerificationStatus.Verified &&
            (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(version) || verifiedOn is null ||
             !Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
             !(uri.Host.Equals("vbpl.vn", StringComparison.OrdinalIgnoreCase) || uri.Host.EndsWith(".vbpl.vn", StringComparison.OrdinalIgnoreCase))))
            throw new ArgumentException("Verified basis requires an ID, version, verification date and HTTPS vbpl.vn URL.");
        Id = id; Version = version; Point = point; Clause = clause; Article = article;
        DocumentNumber = documentNumber; Summary = summary; SourceUrl = sourceUrl; Period = period;
        EffectiveFrom = effectiveFrom; EffectiveTo = effectiveTo; Status = status; VerifiedOn = verifiedOn;
    }
    public static LegalBasis Missing { get; } = new(VerificationStatus.Missing);
}

public sealed record Bracket
{
    public decimal? UpperBound { get; }
    public decimal Rate { get; }
    public LegalBasis Basis { get; }
    public Bracket(decimal? upperBound, decimal rate, LegalBasis? basis = null)
    {
        if (rate is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(rate));
        UpperBound = upperBound; Rate = rate; Basis = basis ?? LegalBasis.Missing;
    }
}

public sealed class TaxRuleSet
{
    public string Id { get; }
    public string Version { get; }
    public string Period { get; }
    public int FromYear { get; }
    public decimal PersonalDeduction { get; }
    public decimal DependentDeduction { get; }
    public IReadOnlyList<Bracket> Brackets { get; }
    public LegalBasis PersonalDeductionBasis { get; }
    public LegalBasis DependentDeductionBasis { get; }
    public LegalBasis TaxableIncomeBasis { get; }
    public LegalBasis TaxBasis { get; }
    public InsuranceRuleSet? InsuranceRules { get; }
    public SalaryRoundingPolicy? RoundingPolicy { get; }
    public AnnualSalaryRuleSet? AnnualSalaryRules { get; }
    public CashBonusRuleSet? CashBonusRules { get; }
    public SalaryExtraRuleSet? SalaryExtraRules { get; }
    public CasualWithholdingRuleSet? CasualWithholdingRules { get; }

    public TaxRuleSet(string id, string version, string period, int fromYear, decimal personalDeduction,
        decimal dependentDeduction, IEnumerable<Bracket> brackets, LegalBasis? personalDeductionBasis = null,
        LegalBasis? dependentDeductionBasis = null, LegalBasis? taxableIncomeBasis = null, LegalBasis? taxBasis = null,
        InsuranceRuleSet? insuranceRules = null, SalaryRoundingPolicy? roundingPolicy = null, AnnualSalaryRuleSet? annualSalaryRules = null, CashBonusRuleSet? cashBonusRules = null, SalaryExtraRuleSet? salaryExtraRules = null, CasualWithholdingRuleSet? casualWithholdingRules = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id); ArgumentException.ThrowIfNullOrWhiteSpace(version);
        ArgumentException.ThrowIfNullOrWhiteSpace(period); ArgumentNullException.ThrowIfNull(brackets);
        if (fromYear < 1) throw new ArgumentOutOfRangeException(nameof(fromYear));
        if (personalDeduction < 0) throw new ArgumentOutOfRangeException(nameof(personalDeduction));
        if (dependentDeduction < 0) throw new ArgumentOutOfRangeException(nameof(dependentDeduction));
        var copy = brackets.ToArray();
        if (copy.Length == 0 || copy.Any(b => b is null)) throw new ArgumentException("Brackets cannot be empty or contain null.", nameof(brackets));
        decimal previous = 0;
        for (int i = 0; i < copy.Length; i++)
        {
            var upper = copy[i].UpperBound;
            if (i == copy.Length - 1 ? upper is not null : upper is null || upper <= previous)
                throw new ArgumentException("Finite bounds must increase and the last bracket must be unbounded.", nameof(brackets));
            if (upper.HasValue) previous = upper.Value;
        }
        Id = id; Version = version; Period = period; FromYear = fromYear;
        PersonalDeduction = personalDeduction; DependentDeduction = dependentDeduction;
        Brackets = new ReadOnlyCollection<Bracket>(copy);
        PersonalDeductionBasis = personalDeductionBasis ?? LegalBasis.Missing;
        DependentDeductionBasis = dependentDeductionBasis ?? LegalBasis.Missing;
        TaxableIncomeBasis = taxableIncomeBasis ?? LegalBasis.Missing; TaxBasis = taxBasis ?? LegalBasis.Missing;
        InsuranceRules = insuranceRules; RoundingPolicy = roundingPolicy; AnnualSalaryRules = annualSalaryRules; CashBonusRules = cashBonusRules; SalaryExtraRules = salaryExtraRules; CasualWithholdingRules = casualWithholdingRules;
    }

    // User-supplied 2025 figures; not yet fully verified against original legislation.
    public static TaxRuleSet Vietnam2025 { get; } = CreateVietnam2025();
    private static TaxRuleSet CreateVietnam2025()
    {
        const string period = "2025 monthly tax period";
        var deduction = new LegalBasis(VerificationStatus.Unverified, id: "2025-deductions", version: "user-reference-1",
            summary: "Former personal and dependent deductions reported by Bao Chinh phu; not fully legally verified.",
            sourceUrl: "https://baochinhphu.vn/cai-cach-thue-thu-nhap-ca-nhan-nguoi-lao-dong-co-the-co-them-hang-chuc-trieu-dong-moi-nam-102261113121825969.htm", period: period);
        var brackets = new LegalBasis(VerificationStatus.Unverified, id: "2025-brackets", version: "user-reference-1",
            documentNumber: "111/2013/TT-BTC", summary: "Monthly brackets supplied by user citing TT111; original text not fully checked.", period: period);
        return new TaxRuleSet("vietnam-2025-reference", "1", period, 2025, 11_000_000m, 4_400_000m,
            [new(5_000_000m, .05m, brackets), new(10_000_000m, .10m, brackets),
             new(18_000_000m, .15m, brackets), new(32_000_000m, .20m, brackets),
             new(52_000_000m, .25m, brackets), new(80_000_000m, .30m, brackets), new(null, .35m, brackets)],
            deduction, deduction, LegalBasis.Missing, brackets);
    }

    // PLAN data only; FromYear is a label, not a legal effective date.
    public static TaxRuleSet Vietnam2026 { get; } = CreateVietnam2026();
    public static TaxRuleProvider CreateVietnam2026AnnualSalaryProvider(InsuranceRegion region)
    {
        const string law = "https://xaydungchinhsach.chinhphu.vn/luat-thue-thu-nhap-ca-nhan-119260623093630882.htm";
        const string decree = "https://datafiles.chinhphu.vn/cpp/files/vbpq/2026/7/253m-ndcp.signed.pdf";
        LegalBasis Basis(string id, string article, string url, string period) => new(VerificationStatus.Unverified,
            id, "2026-1", article: article, sourceUrl: url, period: period);
        var personal = Basis("personal-2026", "10", law, "2026 annual");
        var dependent = Basis("dependent-2026", "10", law, "2026 dependent-month");
        var income = Basis("income-2026", "8", law, "2026");
        var tax = Basis("tax-2026", "9", law, "2026 annual");
        var annualTax = new TaxRuleSet("vn-salary-annual-2026", "1", "2026 annual", 2026, 186_000_000m, 6_200_000m,
            [new(120_000_000m, .05m, tax), new(360_000_000m, .10m, tax), new(720_000_000m, .20m, tax), new(1_200_000_000m, .30m, tax), new(null, .35m, tax)], personal, dependent, income, tax);
        var annual = new AnnualSalaryRuleSet(2026, annualTax, Basis("dependent-period-2026", "48(2)", decree, "2026"),
            Basis("settlement-2026", "48(1)(b); 51; 68", decree, "2026"), Basis("period-2026", "66; 69", decree, "2026"),
            Basis("transition-2026", "70", decree, "2026"));
        var old = CreateVietnam2026SalaryProvider(region);
        TaxRuleSet Snapshot(int month)
        {
            var s = old.GetForMonth(2026, month);
            var monthlyTax = Basis("monthly-tax-2026", "9", law, "2026 monthly");
            return new TaxRuleSet("vn-salary-monthly-2026", "1", "2026 monthly", 2026, 15_500_000m, 6_200_000m,
                [new(10_000_000m, .05m, monthlyTax), new(30_000_000m, .10m, monthlyTax), new(60_000_000m, .20m, monthlyTax), new(100_000_000m, .30m, monthlyTax), new(null, .35m, monthlyTax)],
                personal, dependent, income, monthlyTax, s.InsuranceRules, s.RoundingPolicy, annual);
        }
        return new TaxRuleProvider([new(Snapshot(1), new(2026, 1, 1), new(2026, 6, 30)), new(Snapshot(7), new(2026, 7, 1), new(2026, 12, 31))]);
    }
    public static TaxRuleProvider CreateVietnam2026CashBonusProvider(InsuranceRegion region)
    {
        const string url = "https://datafiles.chinhphu.vn/cpp/files/vbpq/2026/7/253m-ndcp.signed.pdf";
        var taxable = new LegalBasis(VerificationStatus.Unverified, "cash-bonus-taxable-2026", "1", point: "i", clause: "2", article: "8", documentNumber: "253/2026/ND-CP", sourceUrl: url, period: "2026");
        var timing = new LegalBasis(VerificationStatus.Unverified, "cash-bonus-timing-2026", "1", clause: "3", article: "46", documentNumber: "253/2026/ND-CP", sourceUrl: url, period: "2026");
        var bonus = new CashBonusRuleSet("vn-cash-bonus-2026", "1", 2026, new(2026, 1, 1), new(2026, 12, 31), CashBonusTreatment.TaxableCashFromCurrentEmployer, CashBonusTiming.ReceivedOrPaidDate, taxable, timing);
        var source = CreateVietnam2026AnnualSalaryProvider(region);
        TaxRuleSet Snapshot(int month)
        {
            var s = source.GetForMonth(2026, month);
            return new TaxRuleSet(s.Id, s.Version, s.Period, s.FromYear, s.PersonalDeduction, s.DependentDeduction, s.Brackets,
                s.PersonalDeductionBasis, s.DependentDeductionBasis, s.TaxableIncomeBasis, s.TaxBasis, s.InsuranceRules, s.RoundingPolicy, s.AnnualSalaryRules, bonus);
        }
        return new TaxRuleProvider([new(Snapshot(1), new(2026, 1, 1), new(2026, 6, 30)), new(Snapshot(7), new(2026, 7, 1), new(2026, 12, 31))]);
    }
    public static TaxRuleProvider CreateVietnam2026SalaryExtraProvider(InsuranceRegion region)
    {
        const string url = "https://datafiles.chinhphu.vn/cpp/files/vbpq/2026/7/253m-ndcp.signed.pdf";
        LegalBasis Basis(string id, string article, string clause, string? point, string summary) => new(VerificationStatus.Unverified, id, "1", point: point, clause: clause, article: article, documentNumber: "253/2026/ND-CP", summary: summary, sourceUrl: url, period: "2026");
        var work = Basis("extra-work-2026", "26", "1,3", null, "Miễn tiền làm thêm, làm đêm đáp ứng điều kiện và thời gian theo luật lao động. Phần vượt mức quy định chịu thuế.");
        var meal = Basis("extra-meal-2026", "8", "2", "g", "Tiền ăn giữa ca bằng tiền vượt mức tháng tính vào thu nhập. Quy định này áp dụng từ tháng 7 năm 2026.");
        var timing = Basis("extra-timing-2026", "46", "3", null, "Thu nhập tính theo thời điểm thực nhận. Ngày chi trả xác định kỳ tính thuế.");
        var period = Basis("extra-period-2026", "69", "1", "a,b", "Thu nhập tiền lương áp dụng cho kỳ thuế năm 2026. Quy định tiền ăn áp dụng từ ngày 1 tháng 7 năm 2026.");
        var transition = Basis("extra-transition-2026", "70", "2", null, "Khoản đã khai nộp theo quy định cũ trước hiệu lực không phải khai lại tháng hoặc quý. Điều chỉnh thực hiện khi quyết toán năm 2026.");
        var source = CreateVietnam2026CashBonusProvider(region);
        TaxRuleSet Snapshot(int month)
        {
            var s = source.GetForMonth(2026, month);
            var entries = new List<SalaryExtraRule> { new(SalaryExtraKind.Overtime, SalaryExtraMode.ConfirmedQualifiedAmount, null, work), new(SalaryExtraKind.NightWork, SalaryExtraMode.ConfirmedQualifiedAmount, null, work) };
            if (month >= 7) entries.Add(new(SalaryExtraKind.CashMeal, SalaryExtraMode.MonthlyCap, 1_200_000m, meal));
            var extra = new SalaryExtraRuleSet("vn-salary-extra-2026", "1", 2026, new(2026, month, 1), new(2026, month == 1 ? 6 : 12, month == 1 ? 30 : 31), CashBonusTiming.ReceivedOrPaidDate, entries, timing, period, transition);
            return new TaxRuleSet(s.Id, s.Version, s.Period, s.FromYear, s.PersonalDeduction, s.DependentDeduction, s.Brackets, s.PersonalDeductionBasis, s.DependentDeductionBasis, s.TaxableIncomeBasis, s.TaxBasis, s.InsuranceRules, s.RoundingPolicy, s.AnnualSalaryRules, s.CashBonusRules, extra);
        }
        return new TaxRuleProvider([new(Snapshot(1), new(2026, 1, 1), new(2026, 6, 30)), new(Snapshot(7), new(2026, 7, 1), new(2026, 12, 31))]);
    }
    public static TaxRuleProvider CreateVietnam2026CasualWithholdingProvider()
    {
        const string url = "https://datafiles.chinhphu.vn/cpp/files/vbpq/2026/7/253m-ndcp.signed.pdf";
        LegalBasis Basis(string id, string article, string clause, string? point, string summary) => new(VerificationStatus.Unverified,
            id, "1", point: point, clause: clause, article: article, documentNumber: "253/2026/ND-CP", summary: summary,
            sourceUrl: url, period: "2026", effectiveFrom: new(2026, 7, 1));
        var threshold = Basis("casual-threshold-2026", "50", "2", null, "Ngưỡng xét theo từng lần chi trả. Khoản đủ ngưỡng phải khấu trừ trước khi trả.");
        var rate = Basis("casual-rate-2026", "50", "2", null, "Tỷ lệ áp dụng trên toàn bộ thu nhập của lần chi thuộc diện này. Khấu trừ được thực hiện trước khi trả.");
        var voluntary = Basis("casual-request-2026", "50", "2", null, "Dưới ngưỡng cá nhân có thể yêu cầu khấu trừ. Yêu cầu do người gọi xác nhận.");
        var commitment = Basis("casual-commitment-2026", "50", "2", null, "Cam kết đủ điều kiện cho phép tạm thời chưa khấu trừ. Người cam kết chịu trách nhiệm về nội dung.");
        var timing = Basis("casual-timing-2026", "46", "3", null, "Thời điểm xác định thu nhập là khi thực nhận. Ngày chi trả được dùng để chọn kỳ.");
        var period = Basis("casual-period-2026", "69", "1", "a", "Văn bản có hiệu lực từ ngày 1 tháng 7 năm 2026. Thu nhập tiền lương cư trú áp dụng cho kỳ thuế năm 2026.");
        var transition = Basis("casual-transition-2026", "70", "2", null, "Không khai lại tháng hoặc quý trước hiệu lực theo quy định chuyển tiếp. Việc điều chỉnh thực hiện khi quyết toán năm 2026.");
        var rules = new CasualWithholdingRuleSet("vn-casual-2026", "1", 2026, new(2026, 7, 1), new(2026, 12, 31), CasualTiming.ReceivedOrPaidDate, CasualRuleMode.FlatPerCompletePayment, 5_000_000m, .10m,
            [CasualPaymentSubject.NoContract, CasualPaymentSubject.ContractUnderThreeMonths, CasualPaymentSubject.PostTerminationArticle50Paragraph2], threshold, rate, voluntary, commitment, timing, period, transition);
        var s = Vietnam2026;
        var snapshot = new TaxRuleSet("vn-casual-withholding-h2-2026", "1", "2026 casual withholding H2", 2026, s.PersonalDeduction, s.DependentDeduction, s.Brackets, s.PersonalDeductionBasis, s.DependentDeductionBasis, s.TaxableIncomeBasis, s.TaxBasis, casualWithholdingRules: rules);
        return new TaxRuleProvider([new(snapshot, new(2026, 7, 1), new(2026, 12, 31))]);
    }
    public static TaxRuleProvider CreateVietnam2026SalaryProvider(InsuranceRegion region)
    {
        if (!Enum.IsDefined(region)) throw new ArgumentOutOfRangeException(nameof(region));
        decimal minimum = region switch { InsuranceRegion.I => 5_310_000m, InsuranceRegion.II => 4_730_000m,
            InsuranceRegion.III => 4_140_000m, InsuranceRegion.IV => 3_700_000m, _ => throw new ArgumentOutOfRangeException(nameof(region)) };
        var wage = new LegalBasis(VerificationStatus.Unverified, "minimum-wage-293", "2026", article: "3", clause: "1", documentNumber: "293/2025/ND-CP", sourceUrl: "https://xaydungchinhsach.chinhphu.vn/nghi-dinh-so-293-2025-nd-cp-quy-dinh-muc-luong-toi-thieu-doi-voi-nguoi-lao-dong-lam-viec-theo-hop-dong-lao-dong-119251110172808433.htm");
        var social = new LegalBasis(VerificationStatus.Unverified, "social-41", "2026", article: "31, 33, 141", documentNumber: "41/2024/QH15", sourceUrl: "https://xaydungchinhsach.chinhphu.vn/toan-van-luat-so-41-2024-qh15-bao-hiem-xa-hoi-119240723163650489.htm");
        var health = new LegalBasis(VerificationStatus.Unverified, "health-188", "2026", article: "6", documentNumber: "188/2025/ND-CP", sourceUrl: "https://datafiles.chinhphu.vn/cpp/files/vbpq/2025/7/188-ndcp.signed.pdf");
        var unemploymentRate = new LegalBasis(VerificationStatus.Unverified, "unemployment-rate-374", "2026", article: "4", clause: "1", documentNumber: "374/2025/ND-CP", sourceUrl: "https://datafiles.chinhphu.vn/cpp/files/vbpq/2026/01/374-1.signed.pdf");
        var unemploymentCap = new LegalBasis(VerificationStatus.Unverified, "unemployment-cap-74", "2026", article: "34", clause: "2", documentNumber: "74/2025/QH15", sourceUrl: "https://xaydungchinhsach.chinhphu.vn/toan-van-luat-viec-lam-119250711173403835.htm");
        var policy = new SalaryRoundingPolicy("no-rounding", "user-reference-1", new LegalBasis(VerificationStatus.Unverified, "user-no-rounding", "1", summary: "User chose to retain decimal values; not a verified legal rounding rule."));
        TaxRuleSet Snapshot(int start, int end, decimal reference, string url, string document)
        {
            var referenceBasis = new LegalBasis(VerificationStatus.Unverified, "reference-" + start, "2026", article: "3", documentNumber: document, sourceUrl: url);
            var rules = new InsuranceRuleSet("vn-employee-2026", $"{start}-{end}", InsuranceSubject.VietnameseFullMonthEmployee, region,
                new DateOnly(2026, start, 1), new DateOnly(2026, end, DateTime.DaysInMonth(2026, end)), minimum, wage,
                new InsuranceComponentRule(.08m, reference, reference * 20m, social, referenceBasis, social, referenceBasis),
                new InsuranceComponentRule(.015m, reference, reference * 20m, health, referenceBasis, social, referenceBasis),
                new InsuranceComponentRule(.01m, null, minimum * 20m, unemploymentRate, null, unemploymentCap, wage));
            var tax = Vietnam2026;
            return new TaxRuleSet(tax.Id, tax.Version, tax.Period, tax.FromYear, tax.PersonalDeduction, tax.DependentDeduction,
                tax.Brackets, tax.PersonalDeductionBasis, tax.DependentDeductionBasis, tax.TaxableIncomeBasis, tax.TaxBasis, rules, policy);
        }
        var first = Snapshot(1, 6, 2_340_000m, "https://xaydungchinhsach.chinhphu.vn/toan-van-nghi-dinh-73-2024-nd-cp-quy-dinh-muc-luong-co-so-va-che-do-tien-thuong-119240701121328994.htm", "73/2024/ND-CP");
        var second = Snapshot(7, 12, 2_530_000m, "https://datafiles.chinhphu.vn/cpp/files/vbpq/2026/5/161-ndcp.signed.pdf", "161/2026/ND-CP");
        return new TaxRuleProvider([new(first, new(2026, 1, 1), new(2026, 6, 30)), new(second, new(2026, 7, 1), new(2026, 12, 31))]);
    }
    private static TaxRuleSet CreateVietnam2026()
    {
        var basis = new LegalBasis(VerificationStatus.Unverified, id: "plan-2026", version: "reference-1", period: "2026 reference");
        return new TaxRuleSet("vietnam-2026-reference", "1", "2026 reference", 2026, 15_500_000m, 6_200_000m,
            [new(10_000_000m, .05m, basis), new(30_000_000m, .10m, basis), new(60_000_000m, .20m, basis),
             new(100_000_000m, .30m, basis), new(null, .35m, basis)], basis, basis, basis, basis);
    }
}
