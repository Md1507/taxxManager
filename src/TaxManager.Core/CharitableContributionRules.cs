namespace TaxManager.Core;

public sealed record ContributionRecipientRule(ContributionRecipientKind Kind, LegalBasis Basis, LegalBasis LawBasis);
public sealed record ContributionProofRule(ContributionEvidenceKind Kind, LegalBasis Basis);
public sealed class CharitableContributionRuleSet
{
    public string Id { get; }
    public string Version { get; }
    public int Year { get; }
    public DateOnly PeriodFrom { get; }
    public DateOnly PeriodTo { get; }
    public ContributionScope Scope { get; }
    public ContributionTiming Timing { get; }
    public ContributionMode Mode { get; }
    public IReadOnlyList<ContributionRecipientRule> Recipients { get; }
    public IReadOnlyList<ContributionProofRule> Proofs { get; }
    public LegalBasis ConditionsBasis { get; }
    public LegalBasis SelfPaidBasis { get; }
    public LegalBasis DeductionBasis { get; }
    public LegalBasis NoCarryoverBasis { get; }
    public LegalBasis PeriodBasis { get; }
    public LegalBasis ApplicabilityBasis { get; }
    public LegalBasis TransitionBasis { get; }
    public LegalBasis OverpaymentContextBasis { get; }
    public CharitableContributionRuleSet(string id, string version, int year, DateOnly periodFrom, DateOnly periodTo, ContributionScope scope, ContributionTiming timing, ContributionMode mode,
        IEnumerable<ContributionRecipientRule> recipients, IEnumerable<ContributionProofRule> proofs, LegalBasis conditionsBasis, LegalBasis selfPaidBasis, LegalBasis deductionBasis, LegalBasis noCarryoverBasis, LegalBasis periodBasis, LegalBasis applicabilityBasis, LegalBasis transitionBasis, LegalBasis overpaymentContextBasis)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id); ArgumentException.ThrowIfNullOrWhiteSpace(version);
        if (year != 2026 || periodFrom.Year != year || periodTo.Year != year || periodFrom > periodTo) throw new ArgumentException("Invalid contribution rule coverage.");
        if (scope != ContributionScope.VietnameseResidentDomesticVndCalendarYear || timing != ContributionTiming.ContributionDate || mode != ContributionMode.DeductEligibleAmountBeforeAnnualTax) throw new ArgumentException("Unsupported contribution rule.");
        ArgumentNullException.ThrowIfNull(recipients); ArgumentNullException.ThrowIfNull(proofs);
        var rs = recipients.ToArray(); var ps = proofs.ToArray();
        if (rs.Length == 0 || rs.Any(x => x is null || x.Kind is not (ContributionRecipientKind.CareInstitutionForSpeciallyDisadvantagedChildrenDisabledOrUnsupportedElderly or ContributionRecipientKind.CharitableHumanitarianStudyPromotionFund or ContributionRecipientKind.LawfullyEstablishedFundraisingOrganization)) || rs.Select(x => x.Kind).Distinct().Count() != rs.Length) throw new ArgumentException("Invalid recipient rules.");
        if (ps.Length == 0 || ps.Any(x => x is null || x.Kind is not (ContributionEvidenceKind.LegalReceiptCopy or ContributionEvidenceKind.NonCashCreditInstitutionPayment)) || ps.Select(x => x.Kind).Distinct().Count() != ps.Length) throw new ArgumentException("Invalid proof rules.");
        foreach (var b in rs.SelectMany(x => new[] { x.Basis, x.LawBasis }).Concat(ps.Select(x => x.Basis)).Concat(new[] { conditionsBasis, selfPaidBasis, deductionBasis, noCarryoverBasis, periodBasis, applicabilityBasis, transitionBasis, overpaymentContextBasis }))
        {
            ArgumentNullException.ThrowIfNull(b); AnnualSalaryRuleSet.CheckBasis(b, year.ToString());
            if (string.IsNullOrWhiteSpace(b.DocumentNumber) || b.Version != version || !Uri.TryCreate(b.SourceUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps || b.EffectiveFrom > periodFrom || b.EffectiveTo < periodTo) throw new ArgumentException("Invalid contribution basis.");
        }
        Id = id; Version = version; Year = year; PeriodFrom = periodFrom; PeriodTo = periodTo; Scope = scope; Timing = timing; Mode = mode;
        Recipients = Array.AsReadOnly(rs); Proofs = Array.AsReadOnly(ps); ConditionsBasis = conditionsBasis; SelfPaidBasis = selfPaidBasis; DeductionBasis = deductionBasis; NoCarryoverBasis = noCarryoverBasis; PeriodBasis = periodBasis; ApplicabilityBasis = applicabilityBasis; TransitionBasis = transitionBasis; OverpaymentContextBasis = overpaymentContextBasis;
    }
    internal static CharitableContributionRuleSet Vietnam2026()
    {
        const string decree = "https://datafiles.chinhphu.vn/cpp/files/vbpq/2026/7/253m-ndcp.signed.pdf";
        const string law = "https://xaydungchinhsach.chinhphu.vn/luat-thue-thu-nhap-ca-nhan-119260623093630882.htm";
        LegalBasis B(string id, string article, string doc = "253/2026/ND-CP", string? point = null, string? summary = null) => new(VerificationStatus.Unverified, id, "1", point: point, article: article, documentNumber: doc, sourceUrl: doc == "112/VBHN-VPQH" ? law : decree, period: "2026", summary: summary);
        return new("charitable-2026", "1", 2026, new(2026, 1, 1), new(2026, 12, 31), ContributionScope.VietnameseResidentDomesticVndCalendarYear, ContributionTiming.ContributionDate, ContributionMode.DeductEligibleAmountBeforeAnnualTax,
            [new(ContributionRecipientKind.CareInstitutionForSpeciallyDisadvantagedChildrenDisabledOrUnsupportedElderly, B("care", "49(1)", point: "a"), B("law-care", "11(1)", "112/VBHN-VPQH", "a")), new(ContributionRecipientKind.CharitableHumanitarianStudyPromotionFund, B("fund", "49(1)", point: "b"), B("law-fund", "11(1)", "112/VBHN-VPQH", "b")), new(ContributionRecipientKind.LawfullyEstablishedFundraisingOrganization, B("fundraising", "49(1)", point: "c"), B("law-fundraising", "11(1)", "112/VBHN-VPQH", "c"))],
            [new(ContributionEvidenceKind.LegalReceiptCopy, B("receipt", "49(1)")), new(ContributionEvidenceKind.NonCashCreditInstitutionPayment, B("bank", "49(1)"))],
            B("conditions", "49(1)"), B("self-paid", "11(3)", "112/VBHN-VPQH", summary: "Bản hợp nhất Luật 109/2025/QH15 và Luật 09/2026/QH16."), B("deduction", "46(2)(c)"), B("no-carry", "49(5)"), B("period", "66(1)(a)"), B("applicability", "69(1)(a)", summary: "Áp dụng kỳ lương năm 2026; hiệu lực từ 01/07/2026."), B("transition", "70(2)"), B("overpayment", "68(1),(2),(4)"));
    }
}
