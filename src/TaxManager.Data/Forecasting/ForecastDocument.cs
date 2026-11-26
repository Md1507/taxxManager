using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaxManager.Data.Protection;

namespace TaxManager.Data.Forecasting;

public sealed class ForecastDocument
{
    internal readonly ProtectedValue<string> Content = new(null);
    public Guid Id { get; set; } = Guid.NewGuid();
    public int Revision { get; set; } = 1;
    [NotMapped] public string? Payload { get => Content.Value; set => Content.Value = value; }
    public byte[] PayloadEncrypted { get => Content.Cipher; internal set => Content.Cipher = value; }
}

public sealed class ForecastDocumentConfiguration : IEntityTypeConfiguration<ForecastDocument>
{
    public void Configure(EntityTypeBuilder<ForecastDocument> b)
    {
        b.ToTable("ForecastDocuments");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Revision).IsConcurrencyToken();
        b.Property(x => x.PayloadEncrypted).IsRequired().UsePropertyAccessMode(PropertyAccessMode.Property);
    }
}
