using TaxManager.Core;

namespace TaxManager.Data.CalculationHistory;

public sealed record CalculationPortionSnapshot(decimal LowerBound, decimal? UpperBound, decimal Rate,
    decimal TaxableAmount, decimal Tax, LegalBasis Basis)
{
    public override string ToString() => nameof(CalculationPortionSnapshot);
}
