using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TaxManager.Data.TaxRules;

public sealed class TaxRuleSnapshotConfiguration : IEntityTypeConfiguration<TaxRuleSnapshot>
{
    public void Configure(EntityTypeBuilder<TaxRuleSnapshot> builder)
    {
        builder.ToTable(t => t.HasCheckConstraint("CK_TaxRuleSnapshot_Year", "typeof(FromYear) = 'integer' AND FromYear BETWEEN 1 AND 9999"));
        builder.HasKey(x => new { x.RuleId, x.Version });
        // Old supported schemas have no AnnualGraph column. Null graphs are omitted
        // from INSERT; the save coordinator writes non-null graphs in its transaction.
        builder.Property(x => x.AnnualGraph).Metadata.SetBeforeSaveBehavior(
            Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Ignore);
        builder.HasOne(x => x.PersonalBasis).WithMany().HasForeignKey(x => x.PersonalBasisKey).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.DependentBasis).WithMany().HasForeignKey(x => x.DependentBasisKey).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.IncomeBasis).WithMany().HasForeignKey(x => x.IncomeBasisKey).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.TaxBasis).WithMany().HasForeignKey(x => x.TaxBasisKey).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Brackets).WithOne().HasForeignKey(x => new { x.RuleId, x.Version });
    }
}
