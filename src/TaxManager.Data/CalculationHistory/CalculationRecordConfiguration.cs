using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TaxManager.Data.CalculationHistory;

public sealed class CalculationRecordConfiguration : IEntityTypeConfiguration<CalculationRecord>
{
    public void Configure(EntityTypeBuilder<CalculationRecord> b)
    {
        b.ToTable("CalculationHistory", t =>
        {
            t.HasCheckConstraint("CK_History_Year", "Year BETWEEN 1 AND 9999");
            t.HasCheckConstraint("CK_History_Month", "Month BETWEEN 1 AND 12");
            t.HasCheckConstraint("CK_History_Schema", "SchemaVersion = 1");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.PayloadEncrypted).IsRequired().UsePropertyAccessMode(PropertyAccessMode.Property);
        b.HasIndex(x => new { x.ProfileId, x.Year, x.Month, x.CreatedAtUtc });
        // No FK to mutable source data: ordinary profile deletion intentionally retains history.
    }
}
