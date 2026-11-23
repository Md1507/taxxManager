namespace TaxManager.Data.CalculationHistory;

public sealed record CalculationDependentSnapshot(Guid DependentId, DateOnly FromMonth, DateOnly ToMonth);
