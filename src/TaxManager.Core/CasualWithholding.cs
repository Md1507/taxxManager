using System.Collections.ObjectModel;
namespace TaxManager.Core;

public enum CasualPaymentScope { Unspecified, ResidentNonBusinessFullyTaxableVndInVietnam }
public enum CasualPaymentSubject { Unspecified, NoContract, ContractUnderThreeMonths, PostTerminationArticle50Paragraph2 }
public enum CasualRequestMode { Unspecified, Normal, VoluntaryBelowThreshold, CallerConfirmedCommitment }
public enum CasualTiming { Unspecified, ReceivedOrPaidDate }
public enum CasualRuleMode { Unspecified, FlatPerCompletePayment }
public enum CasualDecision { BelowThreshold, MandatoryFlatWithholding, RequestedBelowThreshold, TemporarilyNotWithheldOnCallerCommitment }

public sealed record CasualCommitment(bool OnlyIncomeSubjectToThisFlatWithholding, bool EstimatedAfterFamilyDeductionsBelowTaxLiability, bool ProperDeclarationSubmittedAndAcceptedByPayer, string? EvidenceReference = null);

public sealed record CasualPayment
{
    public string PaymentId { get; }
    public string PayerId { get; }
    public DateOnly ReceivedOn { get; }
    public decimal Paid { get; }
    public CasualPaymentScope Scope { get; }
    public CasualPaymentSubject Subject { get; }
    public CasualRequestMode RequestMode { get; }
    public decimal? ActualWithheld { get; }
    public CasualCommitment? Commitment { get; }
    public string? Label { get; }
    public CasualPayment(string paymentId, string payerId, DateOnly receivedOn, decimal paid, CasualPaymentScope scope, CasualPaymentSubject subject, CasualRequestMode requestMode, decimal? actualWithheld = null, CasualCommitment? commitment = null, string? label = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(paymentId); ArgumentException.ThrowIfNullOrWhiteSpace(payerId);
        if (paid < 0 || actualWithheld < 0) throw new ArgumentOutOfRangeException(nameof(paid));
        if (scope != CasualPaymentScope.ResidentNonBusinessFullyTaxableVndInVietnam || !Enum.IsDefined(subject) || subject == CasualPaymentSubject.Unspecified || !Enum.IsDefined(requestMode) || requestMode == CasualRequestMode.Unspecified) throw new ArgumentException("Unsupported scope, subject or request mode.");
        if (requestMode == CasualRequestMode.CallerConfirmedCommitment ? commitment is null || !commitment.OnlyIncomeSubjectToThisFlatWithholding || !commitment.EstimatedAfterFamilyDeductionsBelowTaxLiability || !commitment.ProperDeclarationSubmittedAndAcceptedByPayer : commitment is not null) throw new ArgumentException("Invalid or conflicting commitment.");
        PaymentId = paymentId.Trim(); PayerId = payerId.Trim(); ReceivedOn = receivedOn; Paid = paid; Scope = scope; Subject = subject; RequestMode = requestMode; ActualWithheld = actualWithheld; Commitment = commitment; Label = label;
    }
}

public sealed class CasualWithholdingRuleSet
{
    public string Id { get; } public string Version { get; } public int Year { get; }
    public DateOnly PeriodFrom { get; } public DateOnly PeriodTo { get; }
    public CasualTiming Timing { get; } public CasualRuleMode Mode { get; }
    public decimal Threshold { get; } public decimal Rate { get; }
    public IReadOnlyList<CasualPaymentSubject> SupportedSubjects { get; }
    public LegalBasis ThresholdBasis { get; } public LegalBasis RateBasis { get; } public LegalBasis VoluntaryBasis { get; } public LegalBasis CommitmentBasis { get; } public LegalBasis TimingBasis { get; } public LegalBasis PeriodBasis { get; } public LegalBasis TransitionBasis { get; }
    public CasualWithholdingRuleSet(string id, string version, int year, DateOnly periodFrom, DateOnly periodTo, CasualTiming timing, CasualRuleMode mode, decimal threshold, decimal rate, IEnumerable<CasualPaymentSubject> supportedSubjects, LegalBasis thresholdBasis, LegalBasis rateBasis, LegalBasis voluntaryBasis, LegalBasis commitmentBasis, LegalBasis timingBasis, LegalBasis periodBasis, LegalBasis transitionBasis)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id); ArgumentException.ThrowIfNullOrWhiteSpace(version); ArgumentNullException.ThrowIfNull(supportedSubjects);
        if (year != 2026 || periodFrom.Year != year || periodTo.Year != year || periodFrom > periodTo || timing != CasualTiming.ReceivedOrPaidDate || mode != CasualRuleMode.FlatPerCompletePayment || threshold <= 0 || rate <= 0 || rate > 1) throw new ArgumentException("Invalid casual rule period, timing, mode or values.");
        var subjects = supportedSubjects.ToArray();
        if (subjects.Length == 0 || subjects.Distinct().Count() != subjects.Length || subjects.Any(x => !Enum.IsDefined(x) || x == CasualPaymentSubject.Unspecified)) throw new ArgumentException("Invalid supported subjects.");
        foreach (var b in new[] { thresholdBasis, rateBasis, voluntaryBasis, commitmentBasis, timingBasis, periodBasis, transitionBasis })
        {
            ArgumentNullException.ThrowIfNull(b);
            AnnualSalaryRuleSet.CheckBasis(b, year.ToString());
            if (b.Version != version || !Uri.TryCreate(b.SourceUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps || b.EffectiveFrom > periodFrom || b.EffectiveTo < periodTo) throw new ArgumentException("Invalid basis version, URL or coverage.");
        }
        Id=id; Version=version; Year=year; PeriodFrom=periodFrom; PeriodTo=periodTo; Timing=timing; Mode=mode; Threshold=threshold; Rate=rate; SupportedSubjects=Array.AsReadOnly(subjects);
        ThresholdBasis=thresholdBasis; RateBasis=rateBasis; VoluntaryBasis=voluntaryBasis; CommitmentBasis=commitmentBasis; TimingBasis=timingBasis; PeriodBasis=periodBasis; TransitionBasis=transitionBasis;
    }
}

public sealed record CasualWithholdingPaymentResult(CasualPayment Input, decimal ExpectedWithheld, CasualDecision Decision, TaxRuleSet RuleSet, CasualWithholdingRuleSet Rules, IReadOnlyList<LegalBasis> DecisionBases)
{
    public decimal? ActualWithheld => Input.ActualWithheld;
    public decimal? WithholdingDifference => ActualWithheld is decimal actual ? ExpectedWithheld - actual : null;
    public LegalBasis InputBasis => LegalBasis.Missing; public LegalBasis SubjectInputBasis => LegalBasis.Missing; public LegalBasis ActualInputBasis => LegalBasis.Missing; public LegalBasis DeclarationInputBasis => LegalBasis.Missing;
}
public sealed class CasualWithholdingBatch
{
    public string PayerId { get; } public IReadOnlyList<CasualPayment> Payments { get; }
    public CasualWithholdingBatch(string payerId, IEnumerable<CasualPayment> payments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payerId); ArgumentNullException.ThrowIfNull(payments);
        var copy = payments.ToArray(); PayerId = payerId.Trim();
        if (copy.Length == 0 || copy.Any(x => x is null) || copy.Any(x => !StringComparer.Ordinal.Equals(x.PayerId, PayerId)) || copy.Select(x => x.PaymentId).Distinct(StringComparer.Ordinal).Count() != copy.Length) throw new ArgumentException("Invalid batch, payer or duplicate payment ID.");
        Payments = Array.AsReadOnly(copy);
    }
}
public sealed record CasualWithholdingBatchResult(IReadOnlyList<CasualWithholdingPaymentResult> Lines, decimal TotalPaid, decimal TotalExpected, decimal? TotalActual, decimal? TotalDifference);
public static class CasualWithholdingCalculator
{
    public static CasualWithholdingPaymentResult Calculate(CasualPayment payment, TaxRuleProvider provider)
    {
        ArgumentNullException.ThrowIfNull(payment); ArgumentNullException.ThrowIfNull(provider);
        var date=payment.ReceivedOn;
        if (date < new DateOnly(2026,7,1) || date > new DateOnly(2026,12,31)) throw new NotSupportedException("Payment date outside supported H2 2026.");
        var snapshot=provider.GetForDate(date); var rules=snapshot.CasualWithholdingRules ?? throw new NotSupportedException("Casual rules missing.");
        var first=new DateOnly(date.Year,date.Month,1); var last=first.AddMonths(1).AddDays(-1);
        if (snapshot.FromYear != date.Year || snapshot.Period != "2026 casual withholding H2" || rules.Year != date.Year || rules.PeriodFrom > first || rules.PeriodTo < last || !rules.SupportedSubjects.Contains(payment.Subject)) throw new NotSupportedException("Rule does not cover payment period or subject.");
        var bases=new List<LegalBasis>(); CasualDecision decision; decimal expected;
        if (payment.RequestMode == CasualRequestMode.CallerConfirmedCommitment) { decision=CasualDecision.TemporarilyNotWithheldOnCallerCommitment; expected=0; bases.Add(rules.CommitmentBasis); }
        else if (payment.RequestMode == CasualRequestMode.VoluntaryBelowThreshold) { if (payment.Paid >= rules.Threshold) throw new ArgumentException("Voluntary mode only applies below threshold."); decision=CasualDecision.RequestedBelowThreshold; expected=payment.Paid*rules.Rate; bases.AddRange([rules.ThresholdBasis,rules.RateBasis,rules.VoluntaryBasis]); }
        else { bases.Add(rules.ThresholdBasis); if (payment.Paid >= rules.Threshold) { decision=CasualDecision.MandatoryFlatWithholding; expected=payment.Paid*rules.Rate; bases.Add(rules.RateBasis); } else { decision=CasualDecision.BelowThreshold; expected=0; } }
        bases.AddRange([rules.TimingBasis,rules.PeriodBasis,rules.TransitionBasis]);
        return new(payment,expected,decision,snapshot,rules,Array.AsReadOnly(bases.ToArray()));
    }
    public static CasualWithholdingBatchResult CalculateBatch(CasualWithholdingBatch batch, TaxRuleProvider provider)
    {
        ArgumentNullException.ThrowIfNull(batch); ArgumentNullException.ThrowIfNull(provider);
        var lines=new List<CasualWithholdingPaymentResult>(); decimal paid=0, expected=0, actual=0, difference=0; bool known=true;
        foreach (var p in batch.Payments) { var line=Calculate(p,provider); lines.Add(line); paid+=p.Paid; expected+=line.ExpectedWithheld; if (line.ActualWithheld is decimal a) { actual+=a; difference+=line.WithholdingDifference!.Value; } else known=false; }
        return new(Array.AsReadOnly(lines.ToArray()),paid,expected,known ? actual : null,known ? difference : null);
    }
}
