using TaxManager.Core.Forecasting;
using Xunit;

namespace TaxManager.Core.Tests.Forecasting;

public sealed class ForecastProfileTests
{
    [Fact]
    public void Profile_retains_periods_and_independent_missing_and_zero_month_values()
    {
        var entered = new DateTimeOffset(2026, 2, 3, 4, 5, 6, TimeSpan.Zero);
        var gross = new ForecastValue<decimal>(12345678.123456789m, ForecastStatus.Planned, "nguồn", "ghi chú", entered);
        var zero = new ForecastValue<decimal>(0m, ForecastStatus.Actual, "tay", null, entered);
        var profile = new ForecastProfile(2026,
            [new ForecastSalaryPeriod(1, 6, gross)],
            [new ForecastMonth(3, zero, null, null, null)]);

        Assert.Equal(gross, Assert.Single(profile.Periods).Gross);
        var month = Assert.Single(profile.Exceptions);
        Assert.Equal(0m, month.Gross!.Value);
        Assert.Null(month.Insurance);
        Assert.Equal(entered, month.Gross.EnteredAt);
        Assert.Equal(ForecastStatus.Actual, month.Gross.Status);
    }
}
