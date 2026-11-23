using System.ComponentModel.DataAnnotations.Schema;
using TaxManager.Data.Protection;

namespace TaxManager.Data.Income;

public sealed class MonthlyIncome
{
    internal readonly ProtectedValue<decimal> Gross = new();
    internal readonly ProtectedValue<decimal> Insurance = new();
    internal readonly ProtectedValue<decimal> Withheld = new();
    public Guid ProfileId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    [NotMapped] public decimal GrossIncome { get => Gross.Value; set => Gross.Value = value; }
    [NotMapped] public decimal InsuranceAmount { get => Insurance.Value; set => Insurance.Value = value; }
    [NotMapped] public decimal ActualWithheldTax { get => Withheld.Value; set => Withheld.Value = value; }
    public byte[] GrossIncomeEncrypted { get => Gross.Cipher; internal set => Gross.Cipher = value; }
    public byte[] InsuranceAmountEncrypted { get => Insurance.Cipher; internal set => Insurance.Cipher = value; }
    public byte[] ActualWithheldTaxEncrypted { get => Withheld.Cipher; internal set => Withheld.Cipher = value; }
    internal bool AmountsInitialized => Gross.Initialized && Insurance.Initialized && Withheld.Initialized;
}
