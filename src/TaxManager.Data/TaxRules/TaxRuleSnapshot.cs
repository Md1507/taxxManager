namespace TaxManager.Data.TaxRules;

public sealed class TaxRuleSnapshot
{
    public string RuleId { get; set; } = null!;
    public string Version { get; set; } = null!;
    public string Period { get; set; } = null!;
    public int FromYear { get; set; }
    public decimal PersonalDeduction { get; set; }
    public decimal DependentDeduction { get; set; }
    public Guid PersonalBasisKey { get; set; }
    public Guid DependentBasisKey { get; set; }
    public Guid IncomeBasisKey { get; set; }
    public Guid TaxBasisKey { get; set; }
    public TaxRuleLegalBasis PersonalBasis { get; set; } = null!;
    public TaxRuleLegalBasis DependentBasis { get; set; } = null!;
    public TaxRuleLegalBasis IncomeBasis { get; set; } = null!;
    public TaxRuleLegalBasis TaxBasis { get; set; } = null!;
    public List<TaxRuleBracket> Brackets { get; set; } = [];
}
