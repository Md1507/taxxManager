namespace TaxManager.Data.Dependents;

public sealed class DependentEligibilityPeriod
{
    public Guid ProfileId { get; set; }
    public Guid DependentId { get; set; }
    public DateOnly EligibleFromMonth { get; set; }
    public DateOnly EligibleToMonth { get; set; }
}
