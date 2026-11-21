namespace TaxManager.Core;

public enum ContributionScope { Unspecified, VietnameseResidentDomesticVndCalendarYear }
public enum ContributionRecipientKind { Unspecified, CareInstitutionForSpeciallyDisadvantagedChildrenDisabledOrUnsupportedElderly, CharitableHumanitarianStudyPromotionFund, LawfullyEstablishedFundraisingOrganization }
public enum ContributionEvidenceKind { Unspecified, LegalReceiptCopy, NonCashCreditInstitutionPayment }
public enum ContributionTiming { Unspecified, ContributionDate }
public enum ContributionMode { Unspecified, DeductEligibleAmountBeforeAnnualTax }
public enum ContributionExclusion { OrganizationNotConfirmed, PurposeNotConfirmed, NonProfitNotConfirmed, SelfPaidNotConfirmed, NotPreviouslyDeductedNotConfirmed, NoValidEvidence }

public sealed class ContributionEvidence
{
    public ContributionEvidenceKind Kind { get; }
    public string Reference { get; }
    public DateOnly IssuedOn { get; }
    public bool LegalReceiptCopyConfirmed { get; }
    public bool NonCashViaCreditInstitutionConfirmed { get; }
    public LegalBasis InputBasis => LegalBasis.Missing;
    public ContributionEvidence(ContributionEvidenceKind kind, string reference, DateOnly issuedOn, bool legalReceiptCopyConfirmed, bool nonCashViaCreditInstitutionConfirmed)
    {
        if (kind is not (ContributionEvidenceKind.LegalReceiptCopy or ContributionEvidenceKind.NonCashCreditInstitutionPayment)) throw new ArgumentOutOfRangeException(nameof(kind));
        ArgumentException.ThrowIfNullOrWhiteSpace(reference);
        if (issuedOn == default) throw new ArgumentOutOfRangeException(nameof(issuedOn));
        Kind = kind; Reference = reference.Trim(); IssuedOn = issuedOn; LegalReceiptCopyConfirmed = legalReceiptCopyConfirmed; NonCashViaCreditInstitutionConfirmed = nonCashViaCreditInstitutionConfirmed;
    }
}

public sealed class CharitableContribution
{
    public string Id { get; }
    public DateOnly ContributedOn { get; }
    public decimal Amount { get; }
    public string RecipientId { get; }
    public ContributionRecipientKind RecipientKind { get; }
    public bool LegallyEstablishedOrRecognizedConfirmed { get; }
    public bool QualifiedPurposeConfirmed { get; }
    public bool NonProfitConfirmed { get; }
    public IReadOnlyList<ContributionEvidence> Evidence { get; }
    public string? Label { get; }
    public LegalBasis InputBasis => LegalBasis.Missing;
    public CharitableContribution(string id, DateOnly contributedOn, decimal amount, string recipientId, ContributionRecipientKind recipientKind, bool legallyEstablishedOrRecognizedConfirmed, bool qualifiedPurposeConfirmed, bool nonProfitConfirmed, IEnumerable<ContributionEvidence> evidence, string? label = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id); ArgumentException.ThrowIfNullOrWhiteSpace(recipientId);
        if (contributedOn == default) throw new ArgumentOutOfRangeException(nameof(contributedOn));
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (recipientKind is not (ContributionRecipientKind.CareInstitutionForSpeciallyDisadvantagedChildrenDisabledOrUnsupportedElderly or ContributionRecipientKind.CharitableHumanitarianStudyPromotionFund or ContributionRecipientKind.LawfullyEstablishedFundraisingOrganization)) throw new ArgumentOutOfRangeException(nameof(recipientKind));
        ArgumentNullException.ThrowIfNull(evidence); var copy = evidence.ToArray();
        if (copy.Any(x => x is null)) throw new ArgumentException("Null evidence.");
        Id = id.Trim(); ContributedOn = contributedOn; Amount = amount; RecipientId = recipientId.Trim(); RecipientKind = recipientKind;
        LegallyEstablishedOrRecognizedConfirmed = legallyEstablishedOrRecognizedConfirmed; QualifiedPurposeConfirmed = qualifiedPurposeConfirmed; NonProfitConfirmed = nonProfitConfirmed; Evidence = Array.AsReadOnly(copy); Label = label;
    }
}

public sealed class CharitableContributionYearInput
{
    public int Year { get; }
    public ContributionScope Scope { get; }
    public bool SelfPaidNotFundedElsewhereConfirmed { get; }
    public bool NotPreviouslyDeductedConfirmed { get; }
    public IReadOnlyList<CharitableContribution> Contributions { get; }
    public LegalBasis InputBasis => LegalBasis.Missing;
    public LegalBasis ConfirmationBasis => LegalBasis.Missing;
    public CharitableContributionYearInput(int year, ContributionScope scope, bool selfPaidNotFundedElsewhereConfirmed, bool notPreviouslyDeductedConfirmed, IEnumerable<CharitableContribution> contributions)
    {
        if (year != 2026) throw new ArgumentOutOfRangeException(nameof(year));
        if (scope != ContributionScope.VietnameseResidentDomesticVndCalendarYear) throw new ArgumentOutOfRangeException(nameof(scope));
        ArgumentNullException.ThrowIfNull(contributions); var copy = contributions.ToArray();
        if (copy.Any(x => x is null)) throw new ArgumentException("Null contribution.");
        if (copy.Any(x => x.ContributedOn.Year != year)) throw new ArgumentException("Contribution year mismatch.");
        if (copy.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count() != copy.Length) throw new ArgumentException("Duplicate contribution ID.");
        Year = year; Scope = scope; SelfPaidNotFundedElsewhereConfirmed = selfPaidNotFundedElsewhereConfirmed; NotPreviouslyDeductedConfirmed = notPreviouslyDeductedConfirmed; Contributions = Array.AsReadOnly(copy);
    }
}

public sealed record ContributionLineResult(CharitableContribution Input, TaxRuleSet RuleSet, CharitableContributionRuleSet Rules, ContributionRecipientRule RecipientRule, ContributionEvidence? UsedEvidence, IReadOnlyList<ContributionExclusion> Reasons, decimal DeclaredAmount, decimal EligibleAmount)
{
    public LegalBasis InputBasis => LegalBasis.Missing;
    public LegalBasis EvidenceBasis => LegalBasis.Missing;
    public LegalBasis ConfirmationBasis => LegalBasis.Missing;
}
public sealed record CharitableContributionResult(IReadOnlyList<ContributionLineResult> Lines, decimal DeclaredTotal, decimal EligibleTotal, decimal IncomeReduction, decimal UnusedAgainstIncome);

internal static class CharitableContributionEvaluator
{
    internal static CharitableContributionResult Evaluate(CharitableContributionYearInput input, TaxRuleProvider provider, decimal before)
    {
        for (var month = 1; month <= 12; month++)
        {
            var snapshot = provider.GetForMonth(input.Year, month); var rule = snapshot.CharitableContributionRules ?? throw new InvalidOperationException("Contribution rule missing.");
            var first = new DateOnly(input.Year, month, 1); var last = first.AddMonths(1).AddDays(-1);
            if (snapshot.FromYear != input.Year || snapshot.Period != $"{input.Year} monthly" || rule.Year != input.Year || rule.Scope != input.Scope || rule.PeriodFrom > first || rule.PeriodTo < last) throw new ArgumentException("Contribution snapshot coverage mismatch.");
        }
        var lines = new List<ContributionLineResult>(); decimal declared = 0, eligible = 0;
        foreach (var c in input.Contributions)
        {
            var snapshot = provider.GetForDate(c.ContributedOn); var rule = snapshot.CharitableContributionRules!;
            var recipient = rule.Recipients.SingleOrDefault(x => x.Kind == c.RecipientKind) ?? throw new ArgumentException("Recipient rule missing.");
            if (c.Evidence.Any(e => !rule.Proofs.Any(p => p.Kind == e.Kind))) throw new ArgumentException("Evidence rule missing.");
            var proof = c.Evidence.FirstOrDefault(e => rule.Proofs.Any(p => p.Kind == e.Kind) && (e.Kind == ContributionEvidenceKind.LegalReceiptCopy ? e.LegalReceiptCopyConfirmed : e.NonCashViaCreditInstitutionConfirmed));
            var reasons = new List<ContributionExclusion>();
            if (!c.LegallyEstablishedOrRecognizedConfirmed) reasons.Add(ContributionExclusion.OrganizationNotConfirmed);
            if (!c.QualifiedPurposeConfirmed) reasons.Add(ContributionExclusion.PurposeNotConfirmed);
            if (!c.NonProfitConfirmed) reasons.Add(ContributionExclusion.NonProfitNotConfirmed);
            if (!input.SelfPaidNotFundedElsewhereConfirmed) reasons.Add(ContributionExclusion.SelfPaidNotConfirmed);
            if (!input.NotPreviouslyDeductedConfirmed) reasons.Add(ContributionExclusion.NotPreviouslyDeductedNotConfirmed);
            if (proof is null) reasons.Add(ContributionExclusion.NoValidEvidence);
            var amount = reasons.Count == 0 ? c.Amount : 0m;
            declared += c.Amount; eligible += amount;
            lines.Add(new(c, snapshot, rule, recipient, proof, reasons.AsReadOnly(), c.Amount, amount));
        }
        var reduction = Math.Min(eligible, Math.Max(0, before));
        return new(lines.AsReadOnly(), declared, eligible, reduction, eligible - reduction);
    }
}
