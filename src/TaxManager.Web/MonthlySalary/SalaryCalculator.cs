using Microsoft.EntityFrameworkCore;
using TaxManager.Data.Audit;
using TaxManager.Data.CalculationHistory;
using TaxManager.Data.Income;
using TaxManager.Data.Profiles;
using TaxManager.Web.Demo;

namespace TaxManager.Web.MonthlySalary;

public sealed class SalaryCalculator(SyntheticDatabase database) : ISalaryCalculator
{
    public async Task<IReadOnlyList<CalculationSnapshot>> CalculateAsync(int year, IReadOnlyList<SalaryInput> months)
    {
        if (year is not (2025 or 2026) || months.Count != 12)
            throw new ArgumentException("Kỳ tính không được hỗ trợ hoặc thiếu tháng.");
        await using var context = database.Open();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var profile = new UserProfile { DisplayName = "Demo giả lập" };
        context.UserProfiles.Add(profile);
        for (var i = 0; i < 12; i++)
            context.MonthlyIncomes.Add(new MonthlyIncome { ProfileId = profile.Id, Year = year, Month = i + 1,
                GrossIncome = months[i].Gross, InsuranceAmount = months[i].Insurance, ActualWithheldTax = months[i].Withheld });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var service = new CalculationHistoryService(context);
        var results = new List<CalculationSnapshot>();
        for (var month = 1; month <= 12; month++)
        {
            context.AuditContext = new AuditWriteContext("synthetic-demo", "demo calculation", OperationId: Guid.NewGuid());
            var saved = await service.CreateMonthlyAsync(profile.Id, year, month, CalculationSourceMode.References);
            results.Add(await service.ReadAsync(profile.Id, saved.HistoryId) ?? throw new InvalidOperationException());
        }
        await transaction.CommitAsync();
        return results;
    }
}
