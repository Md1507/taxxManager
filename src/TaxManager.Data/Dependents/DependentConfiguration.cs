using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaxManager.Data.Profiles;

namespace TaxManager.Data.Dependents;

public sealed class DependentConfiguration : IEntityTypeConfiguration<Dependent>
{
    public void Configure(EntityTypeBuilder<Dependent> builder)
    {
        builder.HasKey(x => new { x.ProfileId, x.Id });
        builder.Property(x => x.DisplayNameEncrypted).IsRequired().UsePropertyAccessMode(PropertyAccessMode.Property);
        builder.HasOne<UserProfile>()
            .WithMany()
            .HasForeignKey(x => x.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
