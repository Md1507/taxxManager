using System.Collections.Immutable;

namespace TaxManager.Data.CalculationHistory;

public sealed record CalculationInputSnapshot(decimal GrossIncome, decimal ManualInsurance,
    decimal ActualWithheldTax, decimal ExemptIncome, int DependentCount,
    ImmutableArray<CalculationDependentSnapshot> Dependents)
{
    public override string ToString() => nameof(CalculationInputSnapshot);
}
