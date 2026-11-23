namespace TaxManager.Data.Tests.CalculationHistory;

internal sealed class HistoryClock : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => HistoryFixture.Now;
}
