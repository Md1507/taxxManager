using System.Collections.ObjectModel;

namespace TaxManager.Core.Forecasting;

public sealed record SalarySchedulePreview(
    IReadOnlyDictionary<int, ForecastValue<decimal>?> Months,
    IReadOnlyList<int> ConflictingMonths);

/// <summary>Gross values for one year; preview never changes the source schedule.</summary>
public sealed class SalarySchedule
{
    private readonly IReadOnlyDictionary<int, ForecastValue<decimal>?> months;
    private readonly HashSet<int> exceptions;
    public int Year { get; }
    public IReadOnlyDictionary<int, ForecastValue<decimal>?> Months => months;

    public SalarySchedule(int year) : this(year,
        Enumerable.Range(1, 12).ToDictionary(month => month, _ => (ForecastValue<decimal>?)null), []) { }

    private SalarySchedule(int year, Dictionary<int, ForecastValue<decimal>?> values, HashSet<int> exceptions)
    {
        if (year is < 1 or > 9999) throw new ArgumentOutOfRangeException(nameof(year));
        Year = year;
        months = new ReadOnlyDictionary<int, ForecastValue<decimal>?>(values);
        this.exceptions = exceptions;
    }

    public SalarySchedule WithException(int month, ForecastValue<decimal> value)
    {
        ValidateMonth(month);
        ArgumentNullException.ThrowIfNull(value);
        if (value.Value < 0) throw new ArgumentOutOfRangeException(nameof(value));
        var values = months.ToDictionary();
        values[month] = value;
        var updated = new HashSet<int>(exceptions) { month };
        return new SalarySchedule(Year, values, updated);
    }

    public SalarySchedulePreview PreviewPeriod(int fromMonth, int toMonth, ForecastValue<decimal> value)
    {
        ValidateMonth(fromMonth);
        ValidateMonth(toMonth);
        ArgumentNullException.ThrowIfNull(value);
        if (fromMonth > toMonth || value.Value < 0) throw new ArgumentOutOfRangeException(nameof(fromMonth));
        var values = months.ToDictionary();
        var conflicts = new List<int>();
        for (var month = fromMonth; month <= toMonth; month++)
        {
            if (exceptions.Contains(month)) conflicts.Add(month);
            else values[month] = value;
        }
        return new SalarySchedulePreview(new ReadOnlyDictionary<int, ForecastValue<decimal>?>(values), conflicts.AsReadOnly());
    }

    public SalarySchedule ApplyPeriod(int fromMonth, int toMonth, ForecastValue<decimal> value)
    {
        var preview = PreviewPeriod(fromMonth, toMonth, value);
        return new SalarySchedule(Year, preview.Months.ToDictionary(), new HashSet<int>(exceptions));
    }

    private static void ValidateMonth(int month)
    {
        if (month is < 1 or > 12) throw new ArgumentOutOfRangeException(nameof(month));
    }
}
