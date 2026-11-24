using TaxManager.Data.CalculationHistory;

namespace TaxManager.Web.MonthlySalary;

public interface ISalaryCalculator
{
    Task<IReadOnlyList<CalculationSnapshot>> CalculateAsync(int year, IReadOnlyList<SalaryInput> months);
}
