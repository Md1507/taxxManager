using System.Collections.ObjectModel;

namespace TaxManager.Core;

public enum SalaryExtraKind { Unspecified, Overtime, NightWork, CashMeal }
public enum SalaryExtraMode { Unspecified, ConfirmedQualifiedAmount, MonthlyCap }

// The caller confirms the qualified amount against conditions, time, statutory limits and records.
public sealed record SalaryExtra
{
    public string Id { get; }
    public SalaryExtraKind Kind { get; }
    public decimal Paid { get; }
    public DateOnly ReceivedOn { get; }
    public decimal? QualifiedConditionsTimeAndLimitAmount { get; }
    public bool EligibilityConfirmed { get; }
    public string? EvidenceReference { get; }
    public SalaryExtra(string id, SalaryExtraKind kind, decimal paid, DateOnly receivedOn,
        decimal? qualifiedConditionsTimeAndLimitAmount = null, bool eligibilityConfirmed = false, string? evidenceReference = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        if (!Enum.IsDefined(kind) || kind == SalaryExtraKind.Unspecified) throw new ArgumentOutOfRangeException(nameof(kind));
        if (paid < 0 || qualifiedConditionsTimeAndLimitAmount < 0 || qualifiedConditionsTimeAndLimitAmount > paid) throw new ArgumentOutOfRangeException(nameof(paid));
        if (kind != SalaryExtraKind.CashMeal && (!eligibilityConfirmed || qualifiedConditionsTimeAndLimitAmount is null)) throw new ArgumentException("Work payments require explicit confirmation and qualified amount.");
        if (kind == SalaryExtraKind.CashMeal && (qualifiedConditionsTimeAndLimitAmount is not null || eligibilityConfirmed)) throw new ArgumentException("Cash meal does not accept a qualified amount.");
        Id = id.Trim(); Kind = kind; Paid = paid; ReceivedOn = receivedOn; QualifiedConditionsTimeAndLimitAmount = qualifiedConditionsTimeAndLimitAmount; EligibilityConfirmed = eligibilityConfirmed; EvidenceReference = evidenceReference;
    }
}
public sealed record SalaryExtraRule
{
    public SalaryExtraKind Kind { get; }
    public SalaryExtraMode Mode { get; }
    public decimal? Cap { get; }
    public LegalBasis Basis { get; }
    public SalaryExtraRule(SalaryExtraKind kind, SalaryExtraMode mode, decimal? cap, LegalBasis basis)
    {
        ArgumentNullException.ThrowIfNull(basis);
        if (!Enum.IsDefined(kind) || kind == SalaryExtraKind.Unspecified || !Enum.IsDefined(mode) || mode == SalaryExtraMode.Unspecified ||
            (mode == SalaryExtraMode.MonthlyCap ? cap is null || cap < 0 : cap is not null)) throw new ArgumentException("Invalid extra rule.");
        Kind = kind; Mode = mode; Cap = cap; Basis = basis;
    }
}
public sealed record SalaryExtraRuleSet
{
    public string Id { get; }
    public string Version { get; }
    public int Year { get; }
    public DateOnly PeriodFrom { get; }
    public DateOnly PeriodTo { get; }
    public CashBonusTiming Timing { get; }
    public IReadOnlyList<SalaryExtraRule> Entries { get; }
    public LegalBasis TimingBasis { get; }
    public LegalBasis PeriodBasis { get; }
    public LegalBasis TransitionBasis { get; }
    public SalaryExtraRuleSet(string id, string version, int year, DateOnly periodFrom, DateOnly periodTo, CashBonusTiming timing,
        IEnumerable<SalaryExtraRule> entries, LegalBasis timingBasis, LegalBasis periodBasis, LegalBasis transitionBasis)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id); ArgumentException.ThrowIfNullOrWhiteSpace(version);
        ArgumentNullException.ThrowIfNull(entries); ArgumentNullException.ThrowIfNull(timingBasis); ArgumentNullException.ThrowIfNull(periodBasis); ArgumentNullException.ThrowIfNull(transitionBasis);
        if (year != 2026 || periodFrom.Year != year || periodTo.Year != year || periodFrom > periodTo || timing != CashBonusTiming.ReceivedOrPaidDate) throw new ArgumentException("Invalid extra period or timing.");
        var copy = entries.ToArray();
        if (copy.Any(e => e is null) || copy.Select(e => e.Kind).Distinct().Count() != copy.Length) throw new ArgumentException("Invalid extra entries.");
        foreach (var b in copy.Select(e => e.Basis).Concat([timingBasis, periodBasis, transitionBasis]))
        {
            AnnualSalaryRuleSet.CheckBasis(b, year.ToString());
            if (b.Version != version || !Uri.TryCreate(b.SourceUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps || b.EffectiveFrom > periodFrom || b.EffectiveTo < periodTo) throw new ArgumentException("Extra basis coverage or version mismatch.");
        }
        Id = id; Version = version; Year = year; PeriodFrom = periodFrom; PeriodTo = periodTo; Timing = timing; Entries = Array.AsReadOnly(copy); TimingBasis = timingBasis; PeriodBasis = periodBasis; TransitionBasis = transitionBasis;
    }
}
public sealed record SalaryExtraLineResult(SalaryExtra Input, int Year, int Month, decimal Exempt, decimal Taxable, TaxRuleSet RuleSet, SalaryExtraRuleSet Rules, SalaryExtraRule Entry)
{
    public decimal Paid => Input.Paid;
    public LegalBasis InputBasis => LegalBasis.Missing;
    public LegalBasis QualifiedInputBasis => LegalBasis.Missing;
}
internal sealed record SalaryIncomeBreakdown(CashBonusBreakdown Bonus, decimal ExtraPaid, decimal Exempt, IReadOnlyList<SalaryExtraLineResult> Lines)
{
    public decimal Gross => Bonus.TotalGross + ExtraPaid;
}
internal static class SalaryIncomePreparation
{
    internal static SalaryIncomeBreakdown Prepare(SalaryMonthInput input, TaxRuleSet rule)
    {
        var bonus = CashBonusCalculation.Prepare(input, rule);
        var ids = new HashSet<string>(input.Bonuses.Select(b => b.Id), StringComparer.Ordinal);
        var lines = new List<SalaryExtraLineResult>(); decimal paid = 0, exempt = 0;
        var remaining = new Dictionary<SalaryExtraKind, decimal>();
        if (input.Extras.Any(x => x is null)) throw new ArgumentNullException("extra item");
        foreach (var item in input.Extras.OrderBy(x => x.ReceivedOn).ThenBy(x => x.Id, StringComparer.Ordinal))
        {
            if (!ids.Add(item.Id)) throw new ArgumentException("Duplicate payment ID.");
            var rules = rule.SalaryExtraRules ?? throw new InvalidOperationException("Salary extra rule is missing.");
            var first = new DateOnly(input.Year, input.Month, 1); var last = first.AddMonths(1).AddDays(-1);
            if (input.Year != rules.Year || rule.FromYear != input.Year || rule.Period != $"{input.Year} monthly" || rules.PeriodFrom > first || rules.PeriodTo < last || item.ReceivedOn < first || item.ReceivedOn > last) throw new ArgumentException("Extra, rule or period mismatch.");
            var entry = rules.Entries.SingleOrDefault(e => e.Kind == item.Kind) ?? throw new NotSupportedException("No rule for extra kind in this period.");
            if ((item.Kind == SalaryExtraKind.CashMeal) != (entry.Mode == SalaryExtraMode.MonthlyCap))
                throw new NotSupportedException("Extra kind and rule mode are incompatible.");
            decimal amount;
            if (entry.Mode == SalaryExtraMode.ConfirmedQualifiedAmount)
            {
                if (!item.EligibilityConfirmed || item.QualifiedConditionsTimeAndLimitAmount is null) throw new ArgumentException("Missing qualification confirmation.");
                amount = item.QualifiedConditionsTimeAndLimitAmount.Value;
            }
            else if (entry.Mode == SalaryExtraMode.MonthlyCap)
            {
                if (!remaining.TryGetValue(item.Kind, out var left)) left = entry.Cap!.Value;
                amount = Math.Min(item.Paid, left); remaining[item.Kind] = left - amount;
            }
            else throw new NotSupportedException("Unsupported extra mode.");
            paid += item.Paid; exempt += amount;
            lines.Add(new(item, input.Year, input.Month, amount, item.Paid - amount, rule, rules, entry));
        }
        return new(bonus, paid, exempt, new ReadOnlyCollection<SalaryExtraLineResult>(lines));
    }
}
