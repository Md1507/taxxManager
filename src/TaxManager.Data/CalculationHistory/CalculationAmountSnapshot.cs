using TaxManager.Core;

namespace TaxManager.Data.CalculationHistory;

/// <summary>Every amount references the single Rules snapshot in its parent document.</summary>
public sealed record CalculationAmountSnapshot(decimal Value, LegalBasis Basis)
{
    internal static CalculationAmountSnapshot FromCore(CalculationAmount amount) => new(amount.Value, amount.Basis);
    public override string ToString() => nameof(CalculationAmountSnapshot);
}
