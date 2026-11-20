using System.Collections.ObjectModel;

namespace TaxManager.Core;

// Each amount retains both its provenance and the immutable rule snapshot.
public sealed record CalculationAmount(decimal Value, TaxRuleSet RuleSet, LegalBasis Basis);
public sealed record TaxPortion(decimal LowerBound, decimal? UpperBound, decimal Rate, decimal TaxableAmount,
    decimal Tax, TaxRuleSet RuleSet, LegalBasis Basis);
public sealed record PitResult(CalculationAmount TaxableIncome, IReadOnlyList<TaxPortion> Portions, CalculationAmount TotalTax);
public sealed record MonthlySalaryResult(CalculationAmount GrossIncome, CalculationAmount Insurance, int Dependents,
    CalculationAmount PersonalDeduction, CalculationAmount DependentDeduction, CalculationAmount TaxableIncomeBeforeClamp,
    CalculationAmount TaxableIncome, PitResult Pit);

public static class PitCalculator
{
    public static PitResult Calculate(decimal taxableIncome, TaxRuleSet ruleSet)
    {
        ArgumentNullException.ThrowIfNull(ruleSet);
        if (taxableIncome < 0) throw new ArgumentOutOfRangeException(nameof(taxableIncome));
        decimal lower = 0, total = 0;
        var portions = new List<TaxPortion>();
        foreach (var bracket in ruleSet.Brackets)
        {
            decimal amount = Math.Max(0, Math.Min(taxableIncome, bracket.UpperBound ?? taxableIncome) - lower);
            decimal tax = amount * bracket.Rate;
            total += tax;
            portions.Add(new(lower, bracket.UpperBound, bracket.Rate, amount, tax, ruleSet, bracket.Basis));
            if (bracket.UpperBound.HasValue) lower = bracket.UpperBound.Value;
        }
        return new(new(taxableIncome, ruleSet, ruleSet.TaxableIncomeBasis),
            new ReadOnlyCollection<TaxPortion>(portions), new(total, ruleSet, ruleSet.TaxBasis));
    }
}

public static class MonthlySalaryCalculator
{
    public static MonthlySalaryResult Calculate(decimal grossIncome, decimal insurance, int dependents, TaxRuleSet ruleSet)
    {
        ArgumentNullException.ThrowIfNull(ruleSet);
        if (grossIncome < 0) throw new ArgumentOutOfRangeException(nameof(grossIncome));
        if (insurance < 0) throw new ArgumentOutOfRangeException(nameof(insurance));
        if (dependents < 0) throw new ArgumentOutOfRangeException(nameof(dependents));
        decimal dependentDeduction = dependents * ruleSet.DependentDeduction;
        decimal before = grossIncome - insurance - ruleSet.PersonalDeduction - dependentDeduction;
        decimal taxable = Math.Max(0, before);
        return new(new(grossIncome, ruleSet, LegalBasis.Missing), new(insurance, ruleSet, LegalBasis.Missing), dependents,
            new(ruleSet.PersonalDeduction, ruleSet, ruleSet.PersonalDeductionBasis),
            new(dependentDeduction, ruleSet, ruleSet.DependentDeductionBasis),
            new(before, ruleSet, ruleSet.TaxableIncomeBasis), new(taxable, ruleSet, ruleSet.TaxableIncomeBasis),
            PitCalculator.Calculate(taxable, ruleSet));
    }
}
