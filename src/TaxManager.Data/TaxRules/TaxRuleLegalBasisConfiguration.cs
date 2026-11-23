using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TaxManager.Data.TaxRules;

public sealed class TaxRuleLegalBasisConfiguration : IEntityTypeConfiguration<TaxRuleLegalBasis>
{
    public void Configure(EntityTypeBuilder<TaxRuleLegalBasis> builder)
    {
        builder.ToTable(t => t.HasCheckConstraint("CK_TaxRuleLegalBasis_Metadata",
            "typeof(Status) = 'integer' AND " +
            "Status BETWEEN 0 AND 2 AND " +
            "(EffectiveFrom IS NULL OR (length(EffectiveFrom) = 10 AND " +
            "strftime('%Y-%m-%d', EffectiveFrom, '+0 days') IS EffectiveFrom AND " +
            "substr(EffectiveFrom, 1, 4) BETWEEN '0001' AND '9999')) AND " +
            "(EffectiveTo IS NULL OR (length(EffectiveTo) = 10 AND " +
            "strftime('%Y-%m-%d', EffectiveTo, '+0 days') IS EffectiveTo AND " +
            "substr(EffectiveTo, 1, 4) BETWEEN '0001' AND '9999')) AND " +
            "(VerifiedOn IS NULL OR (length(VerifiedOn) = 10 AND " +
            "strftime('%Y-%m-%d', VerifiedOn, '+0 days') IS VerifiedOn AND " +
            "substr(VerifiedOn, 1, 4) BETWEEN '0001' AND '9999')) AND " +
            "(EffectiveFrom IS NULL OR EffectiveTo IS NULL OR EffectiveFrom <= EffectiveTo) AND " +
            "(Status = 2 OR VerifiedOn IS NULL)"));
        builder.HasKey(x => x.Key);
    }
}
