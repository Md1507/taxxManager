using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaxManager.Core;
using TaxManager.Core.Forecasting;
using TaxManager.Data.Protection;

namespace TaxManager.Data.Forecasting;

public sealed record AnnualTaxPortion(decimal LowerBound, decimal? UpperBound, decimal Rate, decimal TaxableAmount, decimal Tax, LegalBasis Basis);
public sealed record AnnualResultSnapshot(decimal Obligation, decimal? Difference, int ActualObligationMonths,
    int WithholdingMonths, int ActualWithholdingMonths, decimal ActualWithheld, decimal ForecastWithheld,
    decimal GrossIncome, decimal BonusAdded, decimal BonusAlreadyInGross, decimal Insurance,
    decimal PersonalDeduction, decimal DependentDeduction, decimal TaxableIncome, AnnualTaxPortion[] Portions)
{
    internal static AnnualResultSnapshot FromCore(AnnualForecastResult result) => new(result.Obligation, result.Difference,
        result.ActualObligationMonths, result.WithholdingMonths, result.ActualWithholdingMonths, result.ActualWithheld,
        result.ForecastWithheld, result.Details.GrossIncome, result.Details.BonusTotal, result.Details.BonusAlreadyInGross,
        result.Details.Insurance, result.Details.PersonalDeduction, result.Details.DependentDeduction,
        result.Details.TaxableIncome, result.Details.Pit.Portions.Select(p =>
            new AnnualTaxPortion(p.LowerBound, p.UpperBound, p.Rate, p.TaxableAmount, p.Tax, p.Basis)).ToArray());
}
public sealed record AnnualRuleVersion(DateOnly PeriodFrom, DateOnly PeriodTo, string Graph);
public sealed record AnnualCalculationSnapshot(int SchemaVersion, Guid HistoryId, Guid DocumentId,
    int InputRevision, Guid InputSnapshotId, int Year, DateTimeOffset CreatedAtUtc, string EngineVersion,
    string DraftPayload, ForecastAnnualInput Input, AnnualResultSnapshot Result, AnnualRuleVersion[] Rules);

/// <summary>Encrypted, service-only append. Intentionally no cascading foreign key to a live profile.</summary>
public sealed class AnnualCalculationRecord
{
    internal readonly ProtectedValue<string> Content = new(null);
    public Guid Id { get; private set; }
    public Guid DocumentId { get; private set; }
    public int InputRevision { get; private set; }
    public int Year { get; private set; }
    public int SchemaVersion { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public byte[] PayloadEncrypted { get => Content.Cipher; private set => Content.Cipher = value; }
    [NotMapped] internal string? Payload { get => Content.Value; set => Content.Value = value; }
    [NotMapped] public AnnualCalculationSnapshot Snapshot
    {
        get
        {
            try
            {
                var snapshot = JsonSerializer.Deserialize<AnnualCalculationSnapshot>(Payload ?? throw new JsonException()) ?? throw new JsonException();
                if (snapshot.SchemaVersion != 1 || snapshot.HistoryId != Id || snapshot.DocumentId != DocumentId ||
                    snapshot.InputRevision != InputRevision || snapshot.Year != Year || snapshot.CreatedAtUtc != CreatedAtUtc ||
                    snapshot.Input is null || snapshot.Result is null || snapshot.Rules is not { Length: > 0 })
                    throw new JsonException("Annual snapshot metadata does not match.");
                return snapshot;
            }
            catch (JsonException ex) { throw new DataProtectionException("Annual snapshot is invalid: " + ex.Message); }
        }
    }
    private AnnualCalculationRecord() { }
    internal AnnualCalculationRecord(AnnualCalculationSnapshot snapshot)
    {
        Id = snapshot.HistoryId; DocumentId = snapshot.DocumentId; InputRevision = snapshot.InputRevision;
        Year = snapshot.Year; SchemaVersion = snapshot.SchemaVersion; CreatedAtUtc = snapshot.CreatedAtUtc;
        Payload = JsonSerializer.Serialize(snapshot);
    }
    internal void SetCipher(byte[] cipher) => PayloadEncrypted = cipher;
}

public sealed class AnnualCalculationRecordConfiguration : IEntityTypeConfiguration<AnnualCalculationRecord>
{
    public void Configure(EntityTypeBuilder<AnnualCalculationRecord> builder)
    {
        builder.ToTable("AnnualCalculationHistory");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.PayloadEncrypted).IsRequired().UsePropertyAccessMode(PropertyAccessMode.Property);
        builder.HasIndex(x => new { x.DocumentId, x.CreatedAtUtc });
    }
}
