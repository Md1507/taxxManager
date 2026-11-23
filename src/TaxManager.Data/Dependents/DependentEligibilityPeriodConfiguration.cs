using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TaxManager.Data.Dependents;

public sealed class DependentEligibilityPeriodConfiguration : IEntityTypeConfiguration<DependentEligibilityPeriod>
{
    public void Configure(EntityTypeBuilder<DependentEligibilityPeriod> b)
    {
        b.HasKey(x => new { x.ProfileId, x.DependentId, x.EligibleFromMonth });
        b.HasOne<Dependent>()
            .WithMany()
            .HasForeignKey(x => new { x.ProfileId, x.DependentId })
            .OnDelete(DeleteBehavior.Cascade);
        const string validDates = "length(EligibleFromMonth) = 10 AND length(EligibleToMonth) = 10 AND " +
            "EligibleFromMonth GLOB '[0-9][0-9][0-9][0-9]-[0-9][0-9]-01' AND " +
            "EligibleToMonth GLOB '[0-9][0-9][0-9][0-9]-[0-9][0-9]-01' AND " +
            "substr(EligibleFromMonth,1,4) BETWEEN '0001' AND '9999' AND " +
            "substr(EligibleToMonth,1,4) BETWEEN '0001' AND '9999' AND " +
            "substr(EligibleFromMonth,6,2) BETWEEN '01' AND '12' AND " +
            "substr(EligibleToMonth,6,2) BETWEEN '01' AND '12' AND EligibleFromMonth <= EligibleToMonth";
        b.ToTable(table => table.HasCheckConstraint("CK_Period_Dates", validDates));
    }
}
