namespace TaxManager.Core.Forecasting;

/// <summary>A user-entered salary period, kept separately from month exceptions.</summary>
public sealed record ForecastSalaryPeriod(int FromMonth, int ToMonth, ForecastValue<decimal>? Gross);

/// <summary>Forecast inputs as entered, without flattening periods into months.</summary>
public sealed class ForecastProfile
{
    public int Year { get; }
    public IReadOnlyList<ForecastSalaryPeriod> Periods { get; }
    public IReadOnlyList<ForecastMonth> Exceptions { get; }

    public ForecastProfile(int year, IEnumerable<ForecastSalaryPeriod> periods, IEnumerable<ForecastMonth> exceptions)
    {
        if (year is < 1 or > 9999) throw new ArgumentOutOfRangeException(nameof(year));
        ArgumentNullException.ThrowIfNull(periods);
        ArgumentNullException.ThrowIfNull(exceptions);
        Year = year;
        Periods = Array.AsReadOnly(periods.ToArray());
        Exceptions = Array.AsReadOnly(exceptions.ToArray());
    }
}
