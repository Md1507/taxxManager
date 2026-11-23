using TaxManager.Core;

namespace TaxManager.Data.Income;

public static class SalaryInputMapper
{
    public static SalaryMonthInput ToCoreInput(MonthlyIncome income) => new(
        income.Year,
        income.Month,
        income.GrossIncome,
        income.ActualWithheldTax,
        income.InsuranceAmount);

    public static MonthlyIncome FromCoreInput(Guid profileId, SalaryMonthInput input)
    {
        if (input.InsuranceInput is not null || input.InsuranceAmount is null ||
            input.Bonuses.Count != 0 || input.Extras.Count != 0)
        {
            throw new ArgumentException(
                "Only manual insurance and no bonuses or extras are supported.", nameof(input));
        }

        return new MonthlyIncome
        {
            ProfileId = profileId,
            Year = input.Year,
            Month = input.Month,
            GrossIncome = input.GrossIncome,
            InsuranceAmount = input.InsuranceAmount.Value,
            ActualWithheldTax = input.ActualWithheldTax
        };
    }
}
