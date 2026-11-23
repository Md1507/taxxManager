using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TaxManager.Data.Protection;

public sealed class DataProtectionMetadataConfiguration : IEntityTypeConfiguration<DataProtectionMetadata>
{
    public void Configure(EntityTypeBuilder<DataProtectionMetadata> b)
    {
        b.ToTable("DataProtectionMetadata", t => t.HasCheckConstraint("CK_Protection_Singleton", "Id = 1"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.KeyCheck).IsRequired();
    }
}
