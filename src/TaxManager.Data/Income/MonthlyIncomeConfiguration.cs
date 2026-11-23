using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaxManager.Data.Profiles;

namespace TaxManager.Data.Income;

public sealed class MonthlyIncomeConfiguration : IEntityTypeConfiguration<MonthlyIncome>
{
    public void Configure(EntityTypeBuilder<MonthlyIncome> builder)
    {
        builder.HasKey(x => new { x.ProfileId, x.Year, x.Month });
        builder.HasOne<UserProfile>()
            .WithMany()
            .HasForeignKey(x => x.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_Income_Year", "Year BETWEEN 1 AND 9999");
            table.HasCheckConstraint("CK_Income_Month", "Month BETWEEN 1 AND 12");
        });
        builder.Property(x => x.GrossIncomeEncrypted).IsRequired().UsePropertyAccessMode(PropertyAccessMode.Property);
        builder.Property(x => x.InsuranceAmountEncrypted).IsRequired().UsePropertyAccessMode(PropertyAccessMode.Property);
        builder.Property(x => x.ActualWithheldTaxEncrypted).IsRequired().UsePropertyAccessMode(PropertyAccessMode.Property);
    }
}
