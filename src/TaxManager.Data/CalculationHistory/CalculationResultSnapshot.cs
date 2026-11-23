using System.Collections.Immutable;
using TaxManager.Core;

namespace TaxManager.Data.CalculationHistory;

public sealed record CalculationResultSnapshot(CalculationAmountSnapshot GrossIncome,
    CalculationAmountSnapshot Insurance, int Dependents, CalculationAmountSnapshot PersonalDeduction,
    CalculationAmountSnapshot DependentDeduction, CalculationAmountSnapshot TaxableIncomeBeforeClamp,
    CalculationAmountSnapshot TaxableIncome, decimal ExemptIncome, decimal TaxableGrossIncome,
    CalculationAmountSnapshot PitTaxableIncome, CalculationAmountSnapshot TotalTax,
    ImmutableArray<CalculationPortionSnapshot> Portions)
{
    internal static CalculationResultSnapshot FromCore(MonthlySalaryResult r) => new(
        CalculationAmountSnapshot.FromCore(r.GrossIncome), CalculationAmountSnapshot.FromCore(r.Insurance),
        r.Dependents, CalculationAmountSnapshot.FromCore(r.PersonalDeduction),
        CalculationAmountSnapshot.FromCore(r.DependentDeduction), CalculationAmountSnapshot.FromCore(r.TaxableIncomeBeforeClamp),
        CalculationAmountSnapshot.FromCore(r.TaxableIncome), r.ExemptIncome, r.TaxableGrossIncome,
        CalculationAmountSnapshot.FromCore(r.Pit.TaxableIncome), CalculationAmountSnapshot.FromCore(r.Pit.TotalTax),
        r.Pit.Portions.Select(p => new CalculationPortionSnapshot(p.LowerBound, p.UpperBound, p.Rate,
            p.TaxableAmount, p.Tax, p.Basis)).ToImmutableArray());

    public override string ToString() => nameof(CalculationResultSnapshot);
}
