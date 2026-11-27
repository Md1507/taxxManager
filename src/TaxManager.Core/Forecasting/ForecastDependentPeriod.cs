namespace TaxManager.Core.Forecasting;

/// <summary>A user-provided monthly dependent count over a range; it does not certify registration.</summary>
public sealed record ForecastDependentPeriod(int FromMonth, int ToMonth, ForecastValue<int> Count)
{
    public static IReadOnlyList<ForecastMonth> Apply(IEnumerable<ForecastMonth> months,
        IEnumerable<ForecastDependentPeriod> periods)
    {
        ArgumentNullException.ThrowIfNull(months);
        ArgumentNullException.ThrowIfNull(periods);
        var input = months.ToArray();
        var ranges = periods.ToArray();
        if (input.Length != 12 || input.Any(m => m is null || m.Month is < 1 or > 12) ||
            input.Select(m => m.Month).Distinct().Count() != 12)
            throw new ArgumentException("Exactly twelve distinct months are required.", nameof(months));
        if (ranges.Any(p => p is null || p.FromMonth < 1 || p.ToMonth > 12 ||
            p.FromMonth > p.ToMonth || p.Count is null || p.Count.Value < 0))
            throw new ArgumentException("Dependent periods require a valid month range and count.", nameof(periods));

        var values = new ForecastValue<int>?[13];
        foreach (var period in ranges)
            for (var month = period.FromMonth; month <= period.ToMonth; month++)
            {
                if (values[month] is not null)
                    throw new ArgumentException("Dependent periods overlap.", nameof(periods));
                values[month] = period.Count;
            }
        return Array.AsReadOnly(input.Select(m => m.Dependents is null
            ? m with { Dependents = values[m.Month] } : m).ToArray());
    }
}
