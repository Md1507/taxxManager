using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TaxManager.Data.TaxRules;

public sealed class TaxRuleBracketConfiguration : IEntityTypeConfiguration<TaxRuleBracket>
{
    public void Configure(EntityTypeBuilder<TaxRuleBracket> builder)
    {
        builder.ToTable(t => t.HasCheckConstraint("CK_TaxRuleBracket_Ordinal", "typeof(Ordinal) = 'integer' AND Ordinal BETWEEN 0 AND 2147483647"));
        builder.HasKey(x => new { x.RuleId, x.Version, x.Ordinal });
        builder.HasOne(x => x.Basis).WithMany().HasForeignKey(x => x.BasisKey).OnDelete(DeleteBehavior.Restrict);
    }
}
