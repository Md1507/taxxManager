namespace TaxManager.Data.CalculationHistory;

/// <summary>A self-contained document. Reading it never invokes the current engine or source catalog.</summary>
public sealed record CalculationSnapshot(int SchemaVersion, Guid HistoryId, Guid ProfileId, int Year, int Month,
    DateTimeOffset CreatedAtUtc, CalculationSourceMode SourceMode, CalculationEngineSnapshot Engine,
    CalculationInputSnapshot Input, CalculationRuleSnapshot Rules, CalculationResultSnapshot Result)
{
    public override string ToString() => nameof(CalculationSnapshot);
}
