using TaxManager.Data.CalculationHistory;
using TaxManager.Web.MonthlySalary;

namespace TaxManager.Web.Tests.LegalCitations;

public sealed class CitationScenarioCalculator(IReadOnlyList<CalculationSnapshot> snapshots) : ISalaryCalculator
{
    public Task<IReadOnlyList<CalculationSnapshot>> CalculateAsync(int year, IReadOnlyList<SalaryInput> months) => Task.FromResult(snapshots);
}
