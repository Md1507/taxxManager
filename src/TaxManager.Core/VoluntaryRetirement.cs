namespace TaxManager.Core;

public enum RetirementExclusion { QualifiedProductNotConfirmed, ActuallyPaidNotConfirmed, MonthlyAttributionNotConfirmed, AlreadyDeductedNotRuledOut, MissingReceipt, MissingIncomePayerConfirmation }

public sealed class RetirementEvidence
{
    public RetirementEvidenceKind Kind { get; }
    public string Reference { get; }
    public DateOnly IssuedOn { get; }
    public string IssuerId { get; }
    public decimal Amount { get; }
    public bool Confirmed { get; }
    public LegalBasis InputBasis => LegalBasis.Missing;
    public RetirementEvidence(RetirementEvidenceKind kind, string reference, DateOnly issuedOn, string issuerId, decimal amount, bool confirmed)
    {
        if (kind is not (RetirementEvidenceKind.ReceiptCopy or RetirementEvidenceKind.IncomePayerConfirmation)) throw new ArgumentOutOfRangeException(nameof(kind));
        ArgumentException.ThrowIfNullOrWhiteSpace(reference); ArgumentException.ThrowIfNullOrWhiteSpace(issuerId);
        if (issuedOn == default) throw new ArgumentOutOfRangeException(nameof(issuedOn));
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        Kind = kind; Reference = reference.Trim(); IssuedOn = issuedOn; IssuerId = issuerId.Trim(); Amount = amount; Confirmed = confirmed;
    }
}

public sealed class RetirementContribution
{
    public string Id { get; }
    public DateOnly PaidOn { get; }
    public DateOnly ContributionMonth { get; }
    public decimal Amount { get; }
    public RetirementKind Kind { get; }
    public RetirementPaidBy PaidBy { get; }
    public string PayerId { get; }
    public string ReceivingOrganizationId { get; }
    public string ContractId { get; }
    public bool QualifiedProductConfirmed { get; }
    public bool ActuallyPaidConfirmed { get; }
    public bool DocumentedMonthlyAttributionConfirmed { get; }
    public bool NotAlreadyDeductedConfirmed { get; }
    public IReadOnlyList<RetirementEvidence> Evidence { get; }
    public LegalBasis InputBasis => LegalBasis.Missing;
    public RetirementContribution(string id, DateOnly paidOn, DateOnly contributionMonth, decimal amount, RetirementKind kind, RetirementPaidBy paidBy, string payerId, string receivingOrganizationId, string contractId, bool qualifiedProductConfirmed, bool actuallyPaidConfirmed, bool documentedMonthlyAttributionConfirmed, bool notAlreadyDeductedConfirmed, IEnumerable<RetirementEvidence> evidence)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id); ArgumentException.ThrowIfNullOrWhiteSpace(payerId); ArgumentException.ThrowIfNullOrWhiteSpace(receivingOrganizationId); ArgumentException.ThrowIfNullOrWhiteSpace(contractId);
        if (paidOn == default || contributionMonth == default || contributionMonth.Day != 1 || paidOn.Year != contributionMonth.Year || paidOn.Month != contributionMonth.Month) throw new ArgumentException("Paid date must belong to the attributed month.");
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (kind is not (RetirementKind.SupplementaryRetirementUnderSocialInsuranceLaw or RetirementKind.VoluntaryRetirementInsurance or RetirementKind.LifeInsurance)) throw new ArgumentOutOfRangeException(nameof(kind));
        if (paidBy is not (RetirementPaidBy.Self or RetirementPaidBy.Employer)) throw new ArgumentOutOfRangeException(nameof(paidBy));
        ArgumentNullException.ThrowIfNull(evidence); var copy = evidence.ToArray(); if (copy.Any(x => x is null)) throw new ArgumentException("Null evidence.");
        Id = id.Trim(); PaidOn = paidOn; ContributionMonth = contributionMonth; Amount = amount; Kind = kind; PaidBy = paidBy; PayerId = payerId.Trim(); ReceivingOrganizationId = receivingOrganizationId.Trim(); ContractId = contractId.Trim(); QualifiedProductConfirmed = qualifiedProductConfirmed; ActuallyPaidConfirmed = actuallyPaidConfirmed; DocumentedMonthlyAttributionConfirmed = documentedMonthlyAttributionConfirmed; NotAlreadyDeductedConfirmed = notAlreadyDeductedConfirmed; Evidence = Array.AsReadOnly(copy);
    }
}

public sealed class RetirementMonthInput
{
    public int Year { get; }
    public int Month { get; }
    public IReadOnlyList<RetirementContribution> Contributions { get; }
    public RetirementMonthInput(int year, int month, IEnumerable<RetirementContribution> contributions)
    {
        if (year is < 1 or > 9999) throw new ArgumentOutOfRangeException(nameof(year));
        if (month is < 1 or > 12) throw new ArgumentOutOfRangeException(nameof(month));
        ArgumentNullException.ThrowIfNull(contributions); var copy = contributions.ToArray();
        if (copy.Any(x => x is null)) throw new ArgumentException("Null contribution.");
        if (copy.Any(x => x.ContributionMonth.Year != year || x.ContributionMonth.Month != month)) throw new ArgumentException("Contribution month mismatch.");
        Year = year; Month = month; Contributions = Array.AsReadOnly(copy);
    }
}

public sealed class RetirementYearInput
{
    public int Year { get; }
    public RetirementScope Scope { get; }
    public string PersonalId { get; }
    public bool CompletePersonalPoolConfirmed { get; }
    public bool NoHiddenDuplicatesConfirmed { get; }
    public bool EmployerGrossTreatmentConfirmed { get; }
    public IReadOnlyList<RetirementMonthInput> Months { get; }
    public LegalBasis InputBasis => LegalBasis.Missing;
    public LegalBasis ConfirmationBasis => LegalBasis.Missing;
    public RetirementYearInput(int year, RetirementScope scope, string personalId, bool completePersonalPoolConfirmed, bool noHiddenDuplicatesConfirmed, bool employerGrossTreatmentConfirmed, IEnumerable<RetirementMonthInput> months)
    {
        if (year != 2026) throw new ArgumentOutOfRangeException(nameof(year));
        if (scope != RetirementScope.VietnameseResidentDomesticVndCalendarYear) throw new ArgumentOutOfRangeException(nameof(scope));
        ArgumentException.ThrowIfNullOrWhiteSpace(personalId); ArgumentNullException.ThrowIfNull(months); var copy = months.ToArray();
        if (copy.Any(x => x is null) || copy.Length != 12 || copy.Any(x => x.Year != year) || copy.Select(x => x.Month).Distinct().Count() != 12) throw new ArgumentException("Exactly twelve distinct months are required.");
        var all = copy.SelectMany(x => x.Contributions).ToArray();
        if (all.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count() != all.Length) throw new ArgumentException("Duplicate contribution ID.");
        if (all.Any(x => x.PaidBy == RetirementPaidBy.Self && x.PayerId != personalId.Trim())) throw new ArgumentException("Self payer must match personal ID.");
        Year = year; Scope = scope; PersonalId = personalId.Trim(); CompletePersonalPoolConfirmed = completePersonalPoolConfirmed; NoHiddenDuplicatesConfirmed = noHiddenDuplicatesConfirmed; EmployerGrossTreatmentConfirmed = employerGrossTreatmentConfirmed; Months = Array.AsReadOnly(copy);
    }
}

public sealed record RetirementLineResult(RetirementContribution Input, TaxRuleSet RuleSet, RetirementRuleSet Rules, RetirementKindRule KindRule, RetirementEvidence? UsedReceipt, RetirementEvidence? UsedPayerConfirmation, IReadOnlyList<RetirementExclusion> Reasons, decimal DeclaredAmount, decimal EligibleAmount)
{
    public decimal ExcludedAmount => DeclaredAmount - EligibleAmount;
    public LegalBasis InputBasis => LegalBasis.Missing;
    public LegalBasis EvidenceBasis => LegalBasis.Missing;
    public LegalBasis ConfirmationBasis => LegalBasis.Missing;
}
public sealed record RetirementMonthResult(RetirementMonthInput Input, TaxRuleSet RuleSet, RetirementRuleSet Rules, IReadOnlyList<RetirementLineResult> Lines, decimal DeclaredTotal, decimal EligibleTotal, decimal ExcludedTotal, decimal CappedAmount, decimal DeductibleTotal);
public sealed record RetirementYearResult(IReadOnlyList<RetirementMonthResult> Months, decimal DeclaredTotal, decimal EligibleTotal, decimal ExcludedTotal, decimal CappedAmount, decimal DeductibleTotal, decimal IncomeReduction, decimal UnusedAgainstIncome);

internal static class RetirementEvaluator
{
    internal static RetirementYearResult Evaluate(RetirementYearInput input, TaxRuleProvider provider, decimal before)
    {
        if (!input.CompletePersonalPoolConfirmed || !input.NoHiddenDuplicatesConfirmed) throw new ArgumentException("Complete pool and no hidden duplicates must be confirmed.");
        if (!input.EmployerGrossTreatmentConfirmed && input.Months.SelectMany(x => x.Contributions).Any(x => x.PaidBy == RetirementPaidBy.Employer)) throw new ArgumentException("Employer gross treatment must be confirmed.");
        var months = new List<RetirementMonthResult>();
        foreach (var month in input.Months.OrderBy(x => x.Month))
        {
            var snapshot = provider.GetForMonth(input.Year, month.Month); var rule = snapshot.RetirementRules ?? throw new InvalidOperationException("Retirement rule missing.");
            var first = new DateOnly(input.Year, month.Month, 1); var last = first.AddMonths(1).AddDays(-1);
            if (snapshot.FromYear != input.Year || snapshot.Period != $"{input.Year} monthly" || rule.Year != input.Year || rule.Scope != input.Scope || rule.PeriodFrom > first || rule.PeriodTo < last) throw new ArgumentException("Invalid retirement snapshot or month coverage.");
            var lines = new List<RetirementLineResult>(); decimal declared = 0, eligible = 0;
            foreach (var item in month.Contributions)
            {
                var paidSnapshot = provider.GetForDate(item.PaidOn); if (!ReferenceEquals(snapshot, paidSnapshot)) throw new ArgumentException("Retirement payment snapshot mismatch.");
                var kind = rule.Kinds.Single(x => x.Kind == item.Kind); var reasons = new List<RetirementExclusion>();
                if (!item.QualifiedProductConfirmed) reasons.Add(RetirementExclusion.QualifiedProductNotConfirmed);
                if (!item.ActuallyPaidConfirmed) reasons.Add(RetirementExclusion.ActuallyPaidNotConfirmed);
                if (!item.DocumentedMonthlyAttributionConfirmed) reasons.Add(RetirementExclusion.MonthlyAttributionNotConfirmed);
                if (!item.NotAlreadyDeductedConfirmed) reasons.Add(RetirementExclusion.AlreadyDeductedNotRuledOut);
                var receipt = item.Evidence.FirstOrDefault(x => x.Kind == RetirementEvidenceKind.ReceiptCopy && x.Confirmed && x.IssuerId == item.ReceivingOrganizationId && x.Amount == item.Amount);
                var confirmation = item.Evidence.FirstOrDefault(x => x.Kind == RetirementEvidenceKind.IncomePayerConfirmation && x.Confirmed && x.IssuerId == item.PayerId && x.Amount == item.Amount);
                if (receipt is null) reasons.Add(RetirementExclusion.MissingReceipt);
                if (item.PaidBy == RetirementPaidBy.Employer && confirmation is null) reasons.Add(RetirementExclusion.MissingIncomePayerConfirmation);
                var amount = reasons.Count == 0 ? item.Amount : 0m; declared += item.Amount; eligible += amount;
                lines.Add(new(item, snapshot, rule, kind, receipt, item.PaidBy == RetirementPaidBy.Employer ? confirmation : null, reasons.AsReadOnly(), item.Amount, amount));
            }
            var deductible = Math.Min(eligible, rule.MonthlyCap);
            months.Add(new(month, snapshot, rule, lines.AsReadOnly(), declared, eligible, declared - eligible, eligible - deductible, deductible));
        }
        var total = months.Sum(x => x.DeductibleTotal); var reduction = Math.Min(total, Math.Max(0m, before));
        return new(months.AsReadOnly(), months.Sum(x => x.DeclaredTotal), months.Sum(x => x.EligibleTotal), months.Sum(x => x.ExcludedTotal), months.Sum(x => x.CappedAmount), total, reduction, total - reduction);
    }
}
