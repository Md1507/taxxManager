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

    public TaxRuleSet(string id, string version, string period, int fromYear, decimal personalDeduction,
        decimal dependentDeduction, IEnumerable<Bracket> brackets, LegalBasis? personalDeductionBasis = null,
        LegalBasis? dependentDeductionBasis = null, LegalBasis? taxableIncomeBasis = null, LegalBasis? taxBasis = null,
        InsuranceRuleSet? insuranceRules = null, SalaryRoundingPolicy? roundingPolicy = null)
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
        InsuranceRules = insuranceRules; RoundingPolicy = roundingPolicy;
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
