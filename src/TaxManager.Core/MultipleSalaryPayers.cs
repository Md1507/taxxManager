namespace TaxManager.Core;

public sealed class SalaryReceipt
{
    public string PaymentId { get; }
    public DateOnly ReceivedOn { get; }
    public decimal GrossPaid { get; }
    public decimal? ActualWithheldTax { get; }
    public SalaryReceiptSubject Subject { get; }
    public string? Label { get; }
    public LegalBasis InputBasis => LegalBasis.Missing;
    public SalaryReceipt(string paymentId, DateOnly receivedOn, decimal grossPaid, decimal? actualWithheldTax, SalaryReceiptSubject subject, string? label = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(paymentId);
        if (receivedOn == default) throw new ArgumentOutOfRangeException(nameof(receivedOn));
        if (grossPaid < 0 || actualWithheldTax < 0) throw new ArgumentOutOfRangeException(nameof(grossPaid));
        if (subject != SalaryReceiptSubject.OrdinaryEmploymentFullyTaxableGross) throw new ArgumentOutOfRangeException(nameof(subject));
        PaymentId = paymentId.Trim(); ReceivedOn = receivedOn; GrossPaid = grossPaid; ActualWithheldTax = actualWithheldTax; Subject = subject; Label = label;
    }
}

public sealed class SalaryPayerMonth
{
    public string PayerId { get; }
    public IReadOnlyList<SalaryReceipt> Payments { get; }
    public SalaryPayerMonth(string payerId, IEnumerable<SalaryReceipt> payments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payerId); ArgumentNullException.ThrowIfNull(payments);
        var copy = payments.ToArray();
        if (copy.Length == 0 || copy.Any(x => x is null)) throw new ArgumentException("Payments must be nonempty and non-null.");
        PayerId = payerId.Trim(); Payments = Array.AsReadOnly(copy);
    }
}

public sealed class MultipleSalaryMonth
{
    public int Year { get; }
    public int Month { get; }
    public decimal DeductibleInsurance { get; }
    public IReadOnlyList<SalaryPayerMonth> Payers { get; }
    public MultipleSalaryMonth(int year, int month, decimal deductibleInsurance, IEnumerable<SalaryPayerMonth> payers)
    {
        if (year is < 1 or > 9999) throw new ArgumentOutOfRangeException(nameof(year));
        if (month is < 1 or > 12) throw new ArgumentOutOfRangeException(nameof(month));
        if (deductibleInsurance < 0) throw new ArgumentOutOfRangeException(nameof(deductibleInsurance));
        ArgumentNullException.ThrowIfNull(payers); var copy = payers.ToArray();
        if (copy.Any(x => x is null)) throw new ArgumentException("Null payer.");
        if (copy.Select(x => x.PayerId).Distinct(StringComparer.Ordinal).Count() != copy.Length) throw new ArgumentException("Duplicate payer in month.");
        Year = year; Month = month; DeductibleInsurance = deductibleInsurance; Payers = Array.AsReadOnly(copy);
    }
}

public sealed class MultipleSalaryYearInput
{
    public int Year { get; }
    public MultipleSalaryScope Scope { get; }
    public bool CompleteNonOverlappingIncomeConfirmed { get; }
    public bool DeductibleInsuranceConfirmed { get; }
    public IReadOnlyList<MultipleSalaryMonth> Months { get; }
    public IReadOnlyList<DependentPeriod> Dependents { get; }
    public LegalBasis InputBasis => LegalBasis.Missing;
    public LegalBasis ScopeConfirmationBasis => LegalBasis.Missing;
    public LegalBasis InsuranceInputBasis => LegalBasis.Missing;
    public LegalBasis ActualInputBasis => LegalBasis.Missing;
    public MultipleSalaryYearInput(int year, MultipleSalaryScope scope, bool completeNonOverlappingIncomeConfirmed, bool deductibleInsuranceConfirmed, IEnumerable<MultipleSalaryMonth> months, IEnumerable<DependentPeriod> dependents)
    {
        if (year != 2026) throw new ArgumentOutOfRangeException(nameof(year));
        if (scope != MultipleSalaryScope.VietnameseResidentCalendarYearDomesticVndOnlyOrdinarySalary) throw new ArgumentOutOfRangeException(nameof(scope));
        ArgumentNullException.ThrowIfNull(months); ArgumentNullException.ThrowIfNull(dependents);
        var m = months.ToArray(); var d = dependents.ToArray();
        if (m.Any(x => x is null) || d.Any(x => x is null)) throw new ArgumentException("Null collection item.");
        if (m.Length != 12 || m.Any(x => x.Year != year) || m.Select(x => x.Month).Distinct().Count() != 12) throw new ArgumentException("Exactly twelve distinct months are required.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var month in m) foreach (var payer in month.Payers) foreach (var receipt in payer.Payments)
        {
            if (receipt.ReceivedOn.Year != year || receipt.ReceivedOn.Month != month.Month) throw new ArgumentException("Receipt date differs from its month.");
            if (!ids.Add(receipt.PaymentId)) throw new ArgumentException("Duplicate payment ID.");
        }
        Year = year; Scope = scope; CompleteNonOverlappingIncomeConfirmed = completeNonOverlappingIncomeConfirmed; DeductibleInsuranceConfirmed = deductibleInsuranceConfirmed;
        Months = Array.AsReadOnly(m); Dependents = Array.AsReadOnly(d);
    }
}

public sealed record SalaryPayerMonthResult(SalaryPayerMonth Input, TaxRuleSet RuleSet, MultipleSalaryRuleSet Rules, decimal Gross, decimal ActualWithheldTax)
{
    public string PayerId => Input.PayerId;
    public IReadOnlyList<SalaryReceipt> Payments => Input.Payments;
}
public sealed record MultipleSalaryMonthResult(MultipleSalaryMonth Input, TaxRuleSet RuleSet, MultipleSalaryRuleSet Rules, IReadOnlyList<SalaryPayerMonthResult> Payers, decimal Gross, decimal ActualWithheldTax, SalaryMonthResult AnnualMonth)
{
    public int Year => Input.Year;
    public int Month => Input.Month;
    public decimal Insurance => Input.DeductibleInsurance;
}
public sealed record MultipleSalaryResult(MultipleSalaryYearInput Input, AnnualSalaryResult Annual, IReadOnlyList<MultipleSalaryMonthResult> Months)
{
    public LegalBasis InputBasis => LegalBasis.Missing;
    public LegalBasis ScopeConfirmationBasis => LegalBasis.Missing;
    public LegalBasis InsuranceInputBasis => LegalBasis.Missing;
    public LegalBasis ActualInputBasis => LegalBasis.Missing;
}

public static class MultipleSalaryCalculator
{
    public static MultipleSalaryResult Calculate(MultipleSalaryYearInput input, TaxRuleProvider provider)
        => CalculateCore(input, provider, null, null);

    public static MultipleSalaryResult Calculate(MultipleSalaryYearInput input, TaxRuleProvider provider, CharitableContributionYearInput contributions)
    {
        ArgumentNullException.ThrowIfNull(contributions);
        return CalculateCore(input, provider, contributions, null);
    }

    public static MultipleSalaryResult CalculateWithRetirement(MultipleSalaryYearInput input, TaxRuleProvider provider, RetirementYearInput retirement, CharitableContributionYearInput? contributions = null)
    {
        ArgumentNullException.ThrowIfNull(retirement);
        return CalculateCore(input, provider, contributions, retirement);
    }

    private static MultipleSalaryResult CalculateCore(MultipleSalaryYearInput input, TaxRuleProvider provider, CharitableContributionYearInput? contributions, RetirementYearInput? retirement)
    {
        ArgumentNullException.ThrowIfNull(input); ArgumentNullException.ThrowIfNull(provider);
        if (!input.CompleteNonOverlappingIncomeConfirmed || !input.DeductibleInsuranceConfirmed) throw new ArgumentException("Income completeness and deductible insurance must be confirmed.");
        var normalized = new List<SalaryMonthInput>();
        var details = new List<(MultipleSalaryMonth Input, TaxRuleSet Snapshot, MultipleSalaryRuleSet Rules, IReadOnlyList<SalaryPayerMonthResult> Payers, decimal Gross, decimal Actual)>();
        foreach (var month in input.Months.OrderBy(x => x.Month))
        {
            var snapshot = provider.GetForMonth(input.Year, month.Month);
            var rules = snapshot.MultipleSalaryRules ?? throw new InvalidOperationException("Multiple salary rule missing.");
            var first = new DateOnly(input.Year, month.Month, 1); var last = first.AddMonths(1).AddDays(-1);
            if (snapshot.FromYear != input.Year || snapshot.Period != $"{input.Year} monthly" || rules.Year != input.Year || rules.Scope != input.Scope || rules.PeriodFrom > first || rules.PeriodTo < last || snapshot.RoundingPolicy?.Id != "no-rounding") throw new ArgumentException("Invalid snapshot or month coverage.");
            var payers = new List<SalaryPayerMonthResult>(); decimal gross = 0, actual = 0;
            foreach (var payer in month.Payers)
            {
                decimal pg = 0, pa = 0;
                foreach (var receipt in payer.Payments)
                {
                    if (receipt.Subject != rules.Subject) throw new ArgumentException("Receipt subject mismatch.");
                    if (!receipt.ActualWithheldTax.HasValue) throw new ArgumentException($"Actual withholding is unknown for payment {receipt.PaymentId}.");
                    pg += receipt.GrossPaid; pa += receipt.ActualWithheldTax.Value;
                }
                gross += pg; actual += pa; payers.Add(new(payer, snapshot, rules, pg, pa));
            }
            normalized.Add(new(input.Year, month.Month, gross, actual, insuranceAmount: month.DeductibleInsurance));
            details.Add((month, snapshot, rules, payers.AsReadOnly(), gross, actual));
        }
        var annual = retirement is not null ? AnnualSalaryCalculator.CalculateWithRetirement(input.Year, normalized, input.Dependents, provider, retirement, contributions) : contributions is null ? AnnualSalaryCalculator.Calculate(input.Year, normalized, input.Dependents, provider) : AnnualSalaryCalculator.Calculate(input.Year, normalized, input.Dependents, provider, contributions);
        var result = details.Select((x, i) => new MultipleSalaryMonthResult(x.Input, x.Snapshot, x.Rules, x.Payers, x.Gross, x.Actual, annual.Months[i])).ToArray();
        return new(input, annual, Array.AsReadOnly(result));
    }
}
