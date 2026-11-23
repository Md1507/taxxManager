namespace TaxManager.Data.TaxRules;

public sealed class TaxRuleBracket
{
    public string RuleId { get; set; } = null!;
    public string Version { get; set; } = null!;
    public int Ordinal { get; set; }
    public decimal? UpperBound { get; set; }
    public decimal Rate { get; set; }
    public Guid BasisKey { get; set; }
    public TaxRuleLegalBasis Basis { get; set; } = null!;
}
