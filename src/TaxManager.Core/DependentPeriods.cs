namespace TaxManager.Core;

public sealed record DependentPeriod
{
    public string DependentId { get; }
    public DateOnly EligibleFromMonth { get; }
    public DateOnly EligibleToMonth { get; }
    public DependentPeriod(string dependentId, DateOnly eligibleFromMonth, DateOnly eligibleToMonth)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dependentId);
        if (eligibleFromMonth.Day != 1 || eligibleToMonth.Day != 1 || eligibleFromMonth > eligibleToMonth) throw new ArgumentException("Eligible periods must be ordered first days of months.");
        DependentId = dependentId.Trim(); EligibleFromMonth = eligibleFromMonth; EligibleToMonth = eligibleToMonth;
    }
}

public sealed record DependentMonth(string DependentId, DateOnly EligibleFromMonth, DateOnly EligibleToMonth,
    DateOnly YearFromMonth, DateOnly YearToMonth, DateOnly Month, LegalBasis Basis)
{
    public LegalBasis InputBasis => LegalBasis.Missing;
}
