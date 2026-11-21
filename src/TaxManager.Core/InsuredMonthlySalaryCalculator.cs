namespace TaxManager.Core;

public sealed record TaxAfterPolicy(decimal Raw, decimal Applied, SalaryRoundingPolicy Policy, LegalBasis TaxBasis);
public sealed record InsuredMonthlySalaryResult(InsuranceResult Insurance, CalculationAmount GrossIncome, int Dependents,
    CalculationAmount PersonalDeduction, CalculationAmount DependentDeduction, CalculationAmount TaxableIncomeBeforeClamp,
    CalculationAmount TaxableIncome, PitResult Pit, TaxAfterPolicy Tax);

public static class InsuredMonthlySalaryCalculator
{
    public static InsuredMonthlySalaryResult Calculate(decimal grossIncome, InsuranceInput input, int dependents, int year, int month, TaxRuleProvider provider)
    {
        ArgumentNullException.ThrowIfNull(input); ArgumentNullException.ThrowIfNull(provider);
        if (grossIncome < 0) throw new ArgumentOutOfRangeException(nameof(grossIncome));
        if (dependents < 0) throw new ArgumentOutOfRangeException(nameof(dependents));
        var rule = provider.GetForMonth(year, month);
        var insurance = InsuranceCalculator.Calculate(input, rule, year, month);
        var salary = MonthlySalaryCalculator.Calculate(grossIncome, insurance.DeductibleTotal, dependents, rule);
        var raw = salary.Pit.TotalTax.Value;
        return new(insurance, salary.GrossIncome, salary.Dependents, salary.PersonalDeduction, salary.DependentDeduction,
            salary.TaxableIncomeBeforeClamp, salary.TaxableIncome, salary.Pit, new(raw, raw, insurance.Policy, rule.TaxBasis));
    }
}
