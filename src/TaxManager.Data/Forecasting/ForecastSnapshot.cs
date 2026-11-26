using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaxManager.Data.Protection;

namespace TaxManager.Data.Forecasting;

public sealed class ForecastSnapshot
{
    internal readonly ProtectedValue<string> Content = new(null);
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    public int Revision { get; set; }
    public int SchemaVersion { get; set; } = 1;
    [NotMapped] public string? Payload { get => Content.Value; set => Content.Value = value; }
    public byte[] PayloadEncrypted { get => Content.Cipher; internal set => Content.Cipher = value; }
}

public sealed class ForecastSnapshotConfiguration : IEntityTypeConfiguration<ForecastSnapshot>
{
    public void Configure(EntityTypeBuilder<ForecastSnapshot> b)
    {
        b.ToTable("ForecastSnapshots");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.PayloadEncrypted).IsRequired().UsePropertyAccessMode(PropertyAccessMode.Property);
        b.HasIndex(x => new { x.DocumentId, x.Revision }).IsUnique();
    }
}
