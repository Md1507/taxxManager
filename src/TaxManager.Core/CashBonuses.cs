using System.Collections.ObjectModel;

namespace TaxManager.Core;

public enum CashBonusTreatment { Unspecified = 0, TaxableCashFromCurrentEmployer = 1 }
public enum CashBonusTiming { Unspecified = 0, ReceivedOrPaidDate = 1 }

// Caller confirms eligibility and supplies VND; labels never determine treatment or period.
public sealed record CashBonus
{
    public string Id { get; }
    public decimal Amount { get; }
    public DateOnly ReceivedOn { get; }
    public CashBonusTreatment Treatment { get; }
    public string? Label { get; }
    public CashBonus(string id, decimal amount, DateOnly receivedOn, CashBonusTreatment treatment, string? label = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (treatment != CashBonusTreatment.TaxableCashFromCurrentEmployer) throw new ArgumentOutOfRangeException(nameof(treatment));
        Id = id.Trim(); Amount = amount; ReceivedOn = receivedOn; Treatment = treatment; Label = label;
    }
}

public sealed record CashBonusRuleSet
{
    public string Id { get; }
    public string Version { get; }
    public int Year { get; }
    public DateOnly PeriodFrom { get; }
    public DateOnly PeriodTo { get; }
    public CashBonusTreatment Treatment { get; }
    public CashBonusTiming Timing { get; }
    public LegalBasis TaxableBasis { get; }
    public LegalBasis TimingBasis { get; }
    public CashBonusRuleSet(string id, string version, int year, DateOnly periodFrom, DateOnly periodTo,
        CashBonusTreatment treatment, CashBonusTiming timing, LegalBasis taxableBasis, LegalBasis timingBasis)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id); ArgumentException.ThrowIfNullOrWhiteSpace(version);
        ArgumentNullException.ThrowIfNull(taxableBasis); ArgumentNullException.ThrowIfNull(timingBasis);
        if (year != 2026 || periodFrom.Year != year || periodTo.Year != year || periodFrom > periodTo) throw new ArgumentException("Unsupported bonus year or period.");
        if (treatment != CashBonusTreatment.TaxableCashFromCurrentEmployer || timing != CashBonusTiming.ReceivedOrPaidDate) throw new ArgumentException("Unsupported bonus treatment or timing.");
        foreach (var basis in new[] { taxableBasis, timingBasis })
        {
            AnnualSalaryRuleSet.CheckBasis(basis, year.ToString());
            if (basis.Version != version || !Uri.TryCreate(basis.SourceUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps || basis.EffectiveFrom > periodFrom || basis.EffectiveTo < periodTo)
                throw new ArgumentException("Bonus basis source or coverage is invalid.");
        }
        Id = id; Version = version; Year = year; PeriodFrom = periodFrom; PeriodTo = periodTo;
        Treatment = treatment; Timing = timing; TaxableBasis = taxableBasis; TimingBasis = timingBasis;
    }
}

public sealed record CashBonusLineResult(CashBonus Bonus, int Year, int Month, TaxRuleSet RuleSet, CashBonusRuleSet BonusRules)
{
    public LegalBasis InputBasis => LegalBasis.Missing;
    public LegalBasis TaxableBasis => BonusRules.TaxableBasis;
    public LegalBasis TimingBasis => BonusRules.TimingBasis;
}

internal sealed record CashBonusBreakdown(decimal BonusTotal, decimal TotalGross, IReadOnlyList<CashBonusLineResult> Lines);
internal static class CashBonusCalculation
{
    internal static CashBonusBreakdown Prepare(SalaryMonthInput input, TaxRuleSet rule)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var lines = new List<CashBonusLineResult>(); decimal total = 0;
        foreach (var bonus in input.Bonuses)
        {
            if (bonus is null) throw new ArgumentNullException("bonus item");
            if (!ids.Add(bonus.Id)) throw new ArgumentException("Duplicate bonus ID.");
            var rules = rule.CashBonusRules ?? throw new InvalidOperationException("Cash bonus rule is missing.");
            if (input.Year != rules.Year || rule.FromYear != input.Year || rule.Period != $"{input.Year} monthly" || rules.PeriodFrom > new DateOnly(input.Year, input.Month, 1) || rules.PeriodTo < new DateOnly(input.Year, input.Month, DateTime.DaysInMonth(input.Year, input.Month)) || bonus.ReceivedOn.Year != input.Year || bonus.ReceivedOn.Month != input.Month || bonus.ReceivedOn < rules.PeriodFrom || bonus.ReceivedOn > rules.PeriodTo || bonus.Treatment != rules.Treatment)
                throw new ArgumentException("Bonus, tax rule or period mismatch.");
            total += bonus.Amount;
            lines.Add(new(bonus, input.Year, input.Month, rule, rules));
        }
        return new(total, input.GrossIncome + total, new ReadOnlyCollection<CashBonusLineResult>(lines));
    }
}

public sealed record BonusMonthlySalaryResult(int Year, int Month, TaxRuleSet RuleSet, decimal BaseGrossIncome,
    decimal BonusTotal, IReadOnlyList<CashBonusLineResult> Bonuses, MonthlySalaryResult Salary, InsuranceResult? InsuranceDetails, decimal ActualWithheldTax)
{
    public decimal TotalGrossIncome => Salary.GrossIncome.Value;
    public IReadOnlyList<SalaryExtraLineResult> Extras { get; init; } = Array.Empty<SalaryExtraLineResult>();
    public decimal ExtraPaid => Extras.Sum(x => x.Paid);
    public decimal ExtraTaxable => ExtraPaid - ExemptIncome;
    public decimal ExemptIncome => Salary.ExemptIncome;
    public decimal TaxableGrossIncome => Salary.TaxableGrossIncome;
}
public static class BonusMonthlySalaryCalculator
{
    public static BonusMonthlySalaryResult Calculate(SalaryMonthInput input, int dependents, TaxRuleProvider provider)
    {
        ArgumentNullException.ThrowIfNull(input); ArgumentNullException.ThrowIfNull(provider);
        var rule = provider.GetForMonth(input.Year, input.Month);
        if (rule.FromYear != input.Year) throw new ArgumentException("Tax rule year mismatch.");
        var income = SalaryIncomePreparation.Prepare(input, rule);
        var breakdown = income.Bonus;
        InsuranceResult? detail = null;
        decimal amount;
        if (input.InsuranceInput is not null) { detail = InsuranceCalculator.Calculate(input.InsuranceInput, rule, input.Year, input.Month); amount = detail.DeductibleTotal; }
        else amount = input.InsuranceAmount!.Value;
        var salary = MonthlySalaryCalculator.Calculate(income.Gross, amount, dependents, rule, income.Exempt);
        return new(input.Year, input.Month, rule, input.GrossIncome, breakdown.BonusTotal, breakdown.Lines, salary, detail, input.ActualWithheldTax) { Extras = income.Lines };
    }
}
