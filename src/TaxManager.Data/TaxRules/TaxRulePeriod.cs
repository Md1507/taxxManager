namespace TaxManager.Data.TaxRules;

public sealed class TaxRulePeriod
{
    public Guid Key { get; set; }
    public string RuleId { get; set; } = null!;
    public string Version { get; set; } = null!;
    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    public TaxRuleSnapshot Snapshot { get; set; } = null!;
}
