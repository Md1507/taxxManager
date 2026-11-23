namespace TaxManager.Data.TaxRules;

public sealed class TaxRuleLegalBasis
{
    public Guid Key { get; set; }
    public string? Id { get; set; }
    public string? Version { get; set; }
    public string? Point { get; set; }
    public string? Clause { get; set; }
    public string? Article { get; set; }
    public string? DocumentNumber { get; set; }
    public string? Summary { get; set; }
    public string? SourceUrl { get; set; }
    public string? Period { get; set; }
    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public int Status { get; set; }
    public DateOnly? VerifiedOn { get; set; }
}
