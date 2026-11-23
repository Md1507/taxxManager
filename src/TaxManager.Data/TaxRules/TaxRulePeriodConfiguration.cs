using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TaxManager.Data.TaxRules;

public sealed class TaxRulePeriodConfiguration : IEntityTypeConfiguration<TaxRulePeriod>
{
    public void Configure(EntityTypeBuilder<TaxRulePeriod> builder)
    {
        builder.ToTable(t => t.HasCheckConstraint("CK_TaxRulePeriod_Dates",
            "length(PeriodFrom) = 10 AND " +
            "length(PeriodTo) = 10 AND " +
            "strftime('%Y-%m-%d', PeriodFrom, '+0 days') IS PeriodFrom AND " +
            "strftime('%Y-%m-%d', PeriodTo, '+0 days') IS PeriodTo AND " +
            "substr(PeriodFrom, 1, 4) BETWEEN '0001' AND '9999' AND " +
            "substr(PeriodTo, 1, 4) BETWEEN '0001' AND '9999' AND " +
            "substr(PeriodFrom, 9, 2) = '01' AND " +
            "date(PeriodTo, '+1 day', 'start of month') IS date(PeriodTo, '+1 day') AND " +
            "PeriodFrom <= PeriodTo"));
        builder.HasKey(x => x.Key);
        builder.HasIndex(x => new { x.PeriodFrom, x.PeriodTo }).IsUnique();
        builder.HasOne(x => x.Snapshot).WithMany().HasForeignKey(x => new { x.RuleId, x.Version });
    }
}
