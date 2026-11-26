using TaxManager.Core.Forecasting;
using Xunit;

namespace TaxManager.Core.Tests.Forecasting;

public sealed class SalaryScheduleTests
{
    [Fact]
    public void Applying_period_keeps_exceptions_and_missing_months_distinct_from_zero()
    {
        var entered = new DateTimeOffset(2026, 1, 5, 0, 0, 0, TimeSpan.Zero);
        var planned = new ForecastValue<decimal>(25m, ForecastStatus.Planned, "Kế hoạch", null, entered);
        var actualZero = new ForecastValue<decimal>(0m, ForecastStatus.Actual, "Phiếu lương", null, entered);
        var schedule = new SalarySchedule(2026).WithException(3, actualZero).ApplyPeriod(2, 5, planned);
        Assert.Null(schedule.Months[1]);
        Assert.Equal(25m, schedule.Months[2]!.Value);
        Assert.Equal(0m, schedule.Months[3]!.Value);
        Assert.Equal(ForecastStatus.Actual, schedule.Months[3]!.Status);
        Assert.Null(schedule.Months[6]);
    }

    [Fact]
    public void Later_period_changes_only_its_months_and_preserves_metadata()
    {
        var entered = new DateTimeOffset(2026, 1, 5, 0, 0, 0, TimeSpan.Zero);
        var first = new ForecastValue<decimal>(10m, ForecastStatus.Planned, "Lịch lương", null, entered);
        var second = new ForecastValue<decimal>(20m, ForecastStatus.Assumed, "Ứng dụng", "Ngoại suy", entered.AddDays(2));
        var schedule = new SalarySchedule(2026).ApplyPeriod(1, 12, first).ApplyPeriod(7, 12, second);
        Assert.Equal(10m, schedule.Months[6]!.Value);
        Assert.Equal(20m, schedule.Months[7]!.Value);
        Assert.Equal(ForecastStatus.Assumed, schedule.Months[7]!.Status);
        Assert.Equal("Ngoại suy", schedule.Months[7]!.Note);
        Assert.Equal(entered.AddDays(2), schedule.Months[7]!.EnteredAt);
        Assert.Throws<ArgumentException>(() => new ForecastValue<int>(1, ForecastStatus.Assumed, null, null, entered));
    }

    [Fact]
    public void Preview_preserves_existing_exception_and_reports_conflict()
    {
        var entered = new DateTimeOffset(2026, 1, 5, 0, 0, 0, TimeSpan.Zero);
        var regular = new ForecastValue<decimal>(30_000_000m, ForecastStatus.Planned, "Kế hoạch", null, entered);
        var exception = new ForecastValue<decimal>(0m, ForecastStatus.Actual, "Phiếu lương", "Nghỉ không lương", entered);
        var schedule = new SalarySchedule(2026).WithException(3, exception);
        var preview = schedule.PreviewPeriod(1, 12, regular);

        Assert.Equal(new[] { 3 }, preview.ConflictingMonths);
        Assert.Equal(30_000_000m, preview.Months[1]!.Value);
        Assert.Equal(0m, preview.Months[3]!.Value);
        Assert.Equal(ForecastStatus.Actual, preview.Months[3]!.Status);
        Assert.Equal("Phiếu lương", preview.Months[3]!.Source);
        Assert.Null(schedule.Months[1]);
    }
}
