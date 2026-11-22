namespace TaxManager.Core;

public enum RetirementScope { Unspecified, VietnameseResidentDomesticVndCalendarYear }
public enum RetirementKind { Unspecified, SupplementaryRetirementUnderSocialInsuranceLaw, VoluntaryRetirementInsurance, LifeInsurance }
public enum RetirementPaidBy { Unspecified, Self, Employer }
public enum RetirementEvidenceKind { Unspecified, ReceiptCopy, IncomePayerConfirmation }
public enum RetirementTiming { Unspecified, PaidInAttributedMonthOnly }
public enum RetirementMode { Unspecified, PooledMonthlyCap }
public sealed record RetirementKindRule(RetirementKind Kind, LegalBasis Basis);
public sealed record RetirementProofRule(RetirementEvidenceKind Kind, LegalBasis Basis);

public sealed class RetirementRuleSet
{
    public string Id { get; }
    public string Version { get; }
    public int Year { get; }
    public DateOnly PeriodFrom { get; }
    public DateOnly PeriodTo { get; }
    public RetirementScope Scope { get; }
    public RetirementTiming Timing { get; }
    public RetirementMode Mode { get; }
    public decimal MonthlyCap { get; }
    public IReadOnlyList<RetirementKindRule> Kinds { get; }
    public IReadOnlyList<RetirementProofRule> Proofs { get; }
    public LegalBasis PoolCapBasis { get; }
    public LegalBasis DeductionBasis { get; }
    public LegalBasis LawBasis { get; }
    public LegalBasis PeriodBasis { get; }
    public LegalBasis ApplicabilityBasis { get; }
    public LegalBasis TransitionBasis { get; }
    public LegalBasis OverpaymentContextBasis { get; }

    public RetirementRuleSet(string id, string version, int year, DateOnly periodFrom, DateOnly periodTo, RetirementScope scope, RetirementTiming timing, RetirementMode mode, decimal monthlyCap,
        IEnumerable<RetirementKindRule> kinds, IEnumerable<RetirementProofRule> proofs, LegalBasis poolCapBasis, LegalBasis deductionBasis, LegalBasis lawBasis, LegalBasis periodBasis, LegalBasis applicabilityBasis, LegalBasis transitionBasis, LegalBasis overpaymentContextBasis)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id); ArgumentException.ThrowIfNullOrWhiteSpace(version);
        if (year != 2026 || periodFrom.Year != year || periodTo.Year != year || periodFrom > periodTo) throw new ArgumentException("Invalid retirement rule coverage.");
        if (scope != RetirementScope.VietnameseResidentDomesticVndCalendarYear || timing != RetirementTiming.PaidInAttributedMonthOnly || mode != RetirementMode.PooledMonthlyCap) throw new ArgumentException("Unsupported retirement rule.");
        if (monthlyCap < 0) throw new ArgumentOutOfRangeException(nameof(monthlyCap));
        ArgumentNullException.ThrowIfNull(kinds); ArgumentNullException.ThrowIfNull(proofs);
        var ks = kinds.ToArray(); var ps = proofs.ToArray();
        if (ks.Length != 3 || ks.Any(x => x is null || x.Kind is not (RetirementKind.SupplementaryRetirementUnderSocialInsuranceLaw or RetirementKind.VoluntaryRetirementInsurance or RetirementKind.LifeInsurance)) || ks.Select(x => x.Kind).Distinct().Count() != 3) throw new ArgumentException("Invalid retirement kind rules.");
        if (ps.Length != 2 || ps.Any(x => x is null || x.Kind is not (RetirementEvidenceKind.ReceiptCopy or RetirementEvidenceKind.IncomePayerConfirmation)) || ps.Select(x => x.Kind).Distinct().Count() != 2) throw new ArgumentException("Invalid retirement proof rules.");
        foreach (var b in ks.Select(x => x.Basis).Concat(ps.Select(x => x.Basis)).Concat(new[] { poolCapBasis, deductionBasis, lawBasis, periodBasis, applicabilityBasis, transitionBasis, overpaymentContextBasis }))
        {
            ArgumentNullException.ThrowIfNull(b); AnnualSalaryRuleSet.CheckBasis(b, year.ToString());
            if (string.IsNullOrWhiteSpace(b.DocumentNumber) || b.Version != version || !Uri.TryCreate(b.SourceUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps || b.EffectiveFrom > periodFrom || b.EffectiveTo < periodTo) throw new ArgumentException("Invalid retirement basis.");
        }
        Id = id; Version = version; Year = year; PeriodFrom = periodFrom; PeriodTo = periodTo; Scope = scope; Timing = timing; Mode = mode; MonthlyCap = monthlyCap;
        Kinds = Array.AsReadOnly(ks); Proofs = Array.AsReadOnly(ps); PoolCapBasis = poolCapBasis; DeductionBasis = deductionBasis; LawBasis = lawBasis; PeriodBasis = periodBasis; ApplicabilityBasis = applicabilityBasis; TransitionBasis = transitionBasis; OverpaymentContextBasis = overpaymentContextBasis;
    }

    internal static RetirementRuleSet Vietnam2026()
    {
        const string decree = "https://datafiles.chinhphu.vn/cpp/files/vbpq/2026/7/253m-ndcp.signed.pdf";
        const string law = "https://xaydungchinhsach.chinhphu.vn/luat-thue-thu-nhap-ca-nhan-119260623093630882.htm";
        LegalBasis B(string id, string article, string? clause = null, string? point = null, bool isLaw = false) => new(VerificationStatus.Unverified, id, "1", article: article, clause: clause, point: point, documentNumber: isLaw ? "112/VBHN-VPQH" : "253/2026/ND-CP", sourceUrl: isLaw ? law : decree, period: "2026", summary: isLaw ? "Bản hợp nhất Luật 109/2025/QH15 và Luật 09/2026/QH16." : null);
        return new("retirement-2026", "1", 2026, new(2026, 1, 1), new(2026, 12, 31), RetirementScope.VietnameseResidentDomesticVndCalendarYear, RetirementTiming.PaidInAttributedMonthOnly, RetirementMode.PooledMonthlyCap, 3_000_000m,
            [new(RetirementKind.SupplementaryRetirementUnderSocialInsuranceLaw, B("retirement-kind-supplementary", "46", "2", "a")), new(RetirementKind.VoluntaryRetirementInsurance, B("retirement-kind-voluntary", "46", "2", "a")), new(RetirementKind.LifeInsurance, B("retirement-kind-life", "46", "2", "a"))],
            [new(RetirementEvidenceKind.ReceiptCopy, B("retirement-receipt", "46", "2", "a")), new(RetirementEvidenceKind.IncomePayerConfirmation, B("retirement-payer-confirmation", "46", "2", "a"))],
            B("retirement-pool-cap", "46", "2", "a"), B("retirement-deduction", "46", "2", "a"), B("retirement-law", "8", "2", isLaw: true), B("retirement-period", "66", "1", "a"), B("retirement-applicability", "69", "1", "a"), B("retirement-transition", "70", "2"), B("retirement-overpayment", "68", "1,2,4"));
    }
}
