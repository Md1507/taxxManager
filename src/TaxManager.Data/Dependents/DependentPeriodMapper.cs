using TaxManager.Core;

namespace TaxManager.Data.Dependents;

public static class DependentPeriodMapper
{
    public static DependentPeriod ToCorePeriod(DependentEligibilityPeriod period) => new(
        period.DependentId.ToString("D"),
        period.EligibleFromMonth,
        period.EligibleToMonth);
}
