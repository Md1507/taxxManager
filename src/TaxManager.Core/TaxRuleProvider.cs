namespace TaxManager.Core;

// Dates describe tax periods, not legal effective dates.
public sealed class TaxRuleRegistration
{
    public TaxRuleSet RuleSet { get; }
    public DateOnly PeriodFrom { get; }
    public DateOnly PeriodTo { get; }

    public TaxRuleRegistration(TaxRuleSet ruleSet, DateOnly periodFrom, DateOnly periodTo)
    {
        ArgumentNullException.ThrowIfNull(ruleSet);
        if (periodFrom > periodTo || periodFrom.Day != 1 ||
            periodTo.Day != DateTime.DaysInMonth(periodTo.Year, periodTo.Month))
            throw new ArgumentException("Tax periods must be ordered and cover whole months.");
        RuleSet = ruleSet; PeriodFrom = periodFrom; PeriodTo = periodTo;
    }
}

public sealed class TaxRuleProvider
{
    private readonly TaxRuleRegistration[] registrations;

    public TaxRuleProvider(IEnumerable<TaxRuleRegistration> registrations)
    {
        ArgumentNullException.ThrowIfNull(registrations);
        var copy = registrations.ToArray();
        if (copy.Any(r => r is null)) throw new ArgumentException("Registrations cannot contain null.", nameof(registrations));
        Array.Sort(copy, (a, b) => a.PeriodFrom.CompareTo(b.PeriodFrom));
        for (int i = 1; i < copy.Length; i++)
            if (copy[i].PeriodFrom <= copy[i - 1].PeriodTo)
                throw new ArgumentException("Tax period registrations cannot overlap or duplicate.", nameof(registrations));
        this.registrations = copy;
    }

    public TaxRuleSet GetForDate(DateOnly periodDate) => GetForMonth(periodDate.Year, periodDate.Month);

    public TaxRuleSet GetForMonth(int year, int month)
    {
        if (year is < 1 or > 9999) throw new ArgumentOutOfRangeException(nameof(year));
        if (month is < 1 or > 12) throw new ArgumentOutOfRangeException(nameof(month));
        var date = new DateOnly(year, month, 1);
        return registrations.FirstOrDefault(r => r.PeriodFrom <= date && date <= r.PeriodTo)?.RuleSet
            ?? throw new NotSupportedException($"No tax rule is registered for {year:D4}-{month:D2}.");
    }

    public TaxRuleSet GetForYear(int year)
    {
        var first = GetForMonth(year, 1);
        bool multiple = false;
        for (int month = 2; month <= 12; month++)
            if (!ReferenceEquals(first, GetForMonth(year, month))) multiple = true;
        if (multiple) throw new InvalidOperationException($"Year {year} has multiple rule snapshots; use GetForMonth.");
        return first;
    }
}
