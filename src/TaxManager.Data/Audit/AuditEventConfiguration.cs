using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TaxManager.Data.Audit;

public sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> b)
    {
        b.ToTable("AuditEvents");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.OperationId, x.Ordinal }).IsUnique();
        b.HasIndex(x => x.ProfileId);
        b.Property(x => x.Entity).IsRequired();
        b.Property(x => x.EntityKey).IsRequired();
        b.Property(x => x.Action).IsRequired();
        b.Property(x => x.Payload).IsRequired();
        // Logical profile link only: ordinary cascades must retain encrypted audit.
    }
}
