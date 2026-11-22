using System.Collections.ObjectModel;

namespace TaxManager.Core;

public sealed record SalaryMonthInput
{
    public int Year { get; }
    public int Month { get; }
    public decimal GrossIncome { get; }
    public decimal ActualWithheldTax { get; }
    public decimal? InsuranceAmount { get; }
    public InsuranceInput? InsuranceInput { get; }
    public IReadOnlyList<CashBonus> Bonuses { get; }
    public IReadOnlyList<SalaryExtra> Extras { get; }
    public SalaryMonthInput(int year, int month, decimal grossIncome, decimal actualWithheldTax, decimal? insuranceAmount = null, InsuranceInput? insuranceInput = null, IEnumerable<CashBonus>? bonuses = null, IEnumerable<SalaryExtra>? extras = null)
    {
        if (year is < 1 or > 9999) throw new ArgumentOutOfRangeException(nameof(year));
        if (month is < 1 or > 12) throw new ArgumentOutOfRangeException(nameof(month));
        if (grossIncome < 0 || actualWithheldTax < 0 || insuranceAmount < 0) throw new ArgumentOutOfRangeException(nameof(grossIncome), "Amounts cannot be negative.");
        if (insuranceAmount.HasValue == (insuranceInput is not null)) throw new ArgumentException("Specify exactly one insurance mode.");
        Bonuses = Array.AsReadOnly((bonuses ?? []).ToArray());
        Extras = Array.AsReadOnly((extras ?? []).ToArray());
        Year = year; Month = month; GrossIncome = grossIncome; ActualWithheldTax = actualWithheldTax; InsuranceAmount = insuranceAmount; InsuranceInput = insuranceInput;
    }
}

public sealed record SalaryMonthResult(int Year, int Month, TaxRuleSet RuleSet, IReadOnlyList<DependentMonth> Dependents,
    MonthlySalaryResult Salary, decimal ActualWithheldTax, InsuranceResult? InsuranceDetails)
{
    public decimal BaseGrossIncome { get; init; }
    public decimal BonusTotal { get; init; }
    public decimal TotalGrossIncome => Salary.GrossIncome.Value;
    public IReadOnlyList<CashBonusLineResult> Bonuses { get; init; } = Array.Empty<CashBonusLineResult>();
    public IReadOnlyList<SalaryExtraLineResult> Extras { get; init; } = Array.Empty<SalaryExtraLineResult>();
    public decimal ExtraPaid => Extras.Sum(x => x.Paid);
    public decimal ExtraTaxable => ExtraPaid - ExemptIncome;
    public decimal ExemptIncome => Salary.ExemptIncome;
    public decimal TaxableGrossIncome => Salary.TaxableGrossIncome;
    public int DependentCount => Dependents.Count;
    public LegalBasis WithheldTaxBasis => LegalBasis.Missing;
}

public sealed record AnnualSalaryResult(int Year, IReadOnlyList<SalaryMonthResult> Months, AnnualSalaryRuleSet Rules,
    decimal GrossIncome, decimal Insurance, decimal PersonalDeduction, decimal DependentDeduction, int DependentMonths,
    decimal TaxableIncomeBeforeClamp, decimal TaxableIncome, PitResult Pit, decimal TotalWithheldTax, decimal Delta,
    decimal AdditionalPayable, decimal EstimatedOverpaid, SalaryRoundingPolicy Policy)
{
    public decimal BaseGrossIncome => Months.Sum(m => m.BaseGrossIncome);
    public decimal BonusTotal => Months.Sum(m => m.BonusTotal);
    public decimal ExtraPaid => Months.Sum(m => m.ExtraPaid);
    public decimal ExemptIncome => Months.Sum(m => m.ExemptIncome);
    public decimal ExtraTaxable => ExtraPaid - ExemptIncome;
    public decimal TaxableGrossIncome => GrossIncome - ExemptIncome;
    public CharitableContributionResult? ContributionDetails { get; init; }
    public decimal EligibleCharitableContribution => ContributionDetails?.EligibleTotal ?? 0m;
    public decimal IncomeReduction => ContributionDetails?.IncomeReduction ?? 0m;
    public RetirementYearResult? RetirementDetails { get; init; }
    public decimal RetirementDeduction => RetirementDetails?.DeductibleTotal ?? 0m;
    public decimal RetirementIncomeReduction => RetirementDetails?.IncomeReduction ?? 0m;
    public decimal TotalDeductions => Insurance + PersonalDeduction + DependentDeduction + RetirementDeduction + EligibleCharitableContribution;
    public decimal RawTax => Pit.TotalTax.Value;
    public decimal AppliedTax => RawTax;
}

public static class AnnualSalaryCalculator
{
    private static bool SameTax(TaxRuleSet a, TaxRuleSet b) =>
        a.Id == b.Id && a.Version == b.Version && a.Period == b.Period && a.FromYear == b.FromYear &&
        a.PersonalDeduction == b.PersonalDeduction && a.DependentDeduction == b.DependentDeduction &&
        a.PersonalDeductionBasis == b.PersonalDeductionBasis && a.DependentDeductionBasis == b.DependentDeductionBasis &&
        a.TaxableIncomeBasis == b.TaxableIncomeBasis && a.TaxBasis == b.TaxBasis && a.Brackets.SequenceEqual(b.Brackets);

    public static AnnualSalaryResult Calculate(int year, IEnumerable<SalaryMonthInput> months, IEnumerable<DependentPeriod> dependents, TaxRuleProvider provider)
        => CalculateCore(year, months, dependents, provider, null, null);

    public static AnnualSalaryResult Calculate(int year, IEnumerable<SalaryMonthInput> months, IEnumerable<DependentPeriod> dependents, TaxRuleProvider provider, CharitableContributionYearInput contributions)
    {
        ArgumentNullException.ThrowIfNull(contributions);
        if (contributions.Year != year) throw new ArgumentException("Contribution year mismatch.");
        return CalculateCore(year, months, dependents, provider, contributions, null);
    }

    public static AnnualSalaryResult CalculateWithRetirement(int year, IEnumerable<SalaryMonthInput> months, IEnumerable<DependentPeriod> dependents, TaxRuleProvider provider, RetirementYearInput retirement, CharitableContributionYearInput? contributions = null)
    {
        ArgumentNullException.ThrowIfNull(retirement);
        if (retirement.Year != year) throw new ArgumentException("Retirement year mismatch.");
        if (contributions is not null && contributions.Year != year) throw new ArgumentException("Contribution year mismatch.");
        return CalculateCore(year, months, dependents, provider, contributions, retirement);
    }

    private static AnnualSalaryResult CalculateCore(int year, IEnumerable<SalaryMonthInput> months, IEnumerable<DependentPeriod> dependents, TaxRuleProvider provider, CharitableContributionYearInput? contributions, RetirementYearInput? retirement)
    {
        if (year is < 1 or > 9999) throw new ArgumentOutOfRangeException(nameof(year));
        ArgumentNullException.ThrowIfNull(months); ArgumentNullException.ThrowIfNull(dependents); ArgumentNullException.ThrowIfNull(provider);
        var inputs = months.ToArray(); var periods = dependents.ToArray();
        if (inputs.Any(x => x is null) || periods.Any(x => x is null)) throw new ArgumentNullException("collection item");
        if (inputs.Length != 12 || inputs.Any(x => x.Year != year) || inputs.Select(x => x.Month).Distinct().Count() != 12)
            throw new ArgumentException("Exactly twelve distinct months of the requested year are required.", nameof(months));
        inputs = inputs.OrderBy(x => x.Month).ToArray();
        foreach (var group in periods.GroupBy(x => x.DependentId, StringComparer.Ordinal))
        {
            var sorted = group.OrderBy(x => x.EligibleFromMonth).ToArray();
            for (var i = 1; i < sorted.Length; i++)
                if (sorted[i].EligibleFromMonth <= sorted[i - 1].EligibleToMonth) throw new ArgumentException("Dependent periods overlap.", nameof(dependents));
        }
        var snapshots = Enumerable.Range(1, 12).Select(m => provider.GetForMonth(year, m)).ToArray();
        var annual = snapshots[0].AnnualSalaryRules ?? throw new InvalidOperationException("Annual salary rule is missing.");
        var policy = snapshots[0].RoundingPolicy ?? throw new InvalidOperationException("Rounding policy is missing.");
        if (annual.Year != year || annual.TaxRules.FromYear != year) throw new ArgumentException("Annual rule year mismatch.");
        for (int i = 0; i < 12; i++)
        {
            var s = snapshots[i]; var a = s.AnnualSalaryRules ?? throw new InvalidOperationException($"Annual salary rule missing for month {i + 1}.");
            if (s.RoundingPolicy is null) throw new InvalidOperationException($"Rounding policy missing for month {i + 1}.");
            if (s.FromYear != year || a.Year != year || !SameTax(a.TaxRules, annual.TaxRules) || a.DependentPeriodBasis != annual.DependentPeriodBasis || a.SettlementBasis != annual.SettlementBasis || a.PeriodBasis != annual.PeriodBasis || a.TransitionBasis != annual.TransitionBasis || s.RoundingPolicy != policy)
                throw new ArgumentException($"Annual rule or policy mismatch for month {i + 1}.");
            if (!SameTax(s, snapshots[0])) throw new NotSupportedException($"Tax changes during the year are unsupported (month {i + 1}).");
            AnnualSalaryRuleSet.RequirePeriod(s.Period, $"{year} monthly", "monthly tax rule");
            AnnualSalaryRuleSet.CheckBasis(s.TaxBasis, $"{year} monthly");
            foreach (var bracket in s.Brackets) AnnualSalaryRuleSet.CheckBasis(bracket.Basis, $"{year} monthly");
            if (s.PersonalDeductionBasis != a.TaxRules.PersonalDeductionBasis || s.DependentDeductionBasis != a.TaxRules.DependentDeductionBasis || s.TaxableIncomeBasis != a.TaxRules.TaxableIncomeBasis)
                throw new ArgumentException("Monthly and annual deduction or income basis mismatch.");
            if (s.PersonalDeduction * 12m != annual.AnnualPersonalDeduction || s.DependentDeduction != annual.DependentMonthDeduction)
                throw new ArgumentException("Monthly and annual deductions mismatch.");
            if (s.Brackets.Count != annual.TaxRules.Brackets.Count || s.Brackets.Zip(annual.TaxRules.Brackets).Any(pair =>
                pair.First.Rate != pair.Second.Rate || pair.First.UpperBound * 12m != pair.Second.UpperBound))
                throw new ArgumentException("Monthly and annual tax brackets mismatch.");
        }
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var input in inputs)
            foreach (var bonus in input.Bonuses)
            {
                if (bonus is null) throw new ArgumentNullException("bonus item");
                if (!ids.Add(bonus.Id)) throw new ArgumentException("Duplicate bonus ID in year.");
            }
        foreach (var input in inputs)
            foreach (var extra in input.Extras)
            {
                if (extra is null) throw new ArgumentNullException("extra item");
                if (!ids.Add(extra.Id)) throw new ArgumentException("Duplicate payment ID in year.");
            }
        var results = new List<SalaryMonthResult>(); decimal gross = 0, exempt = 0, insurance = 0, withheld = 0; int dependentMonths = 0;
        var first = new DateOnly(year, 1, 1); var last = new DateOnly(year, 12, 1);
        foreach (var input in inputs)
        {
            var date = new DateOnly(year, input.Month, 1); var rule = snapshots[input.Month - 1];
            var active = periods.Where(p => p.EligibleFromMonth <= date && p.EligibleToMonth >= date)
                .Select(p => new DependentMonth(p.DependentId, p.EligibleFromMonth, p.EligibleToMonth,
                    p.EligibleFromMonth < first ? first : p.EligibleFromMonth, p.EligibleToMonth > last ? last : p.EligibleToMonth, date, annual.DependentPeriodBasis)).ToArray();
            InsuranceResult? detail = null; decimal amount;
            if (input.InsuranceInput is not null) { detail = InsuranceCalculator.Calculate(input.InsuranceInput, rule, year, input.Month); amount = detail.DeductibleTotal; }
            else amount = input.InsuranceAmount!.Value;
            var breakdown = SalaryIncomePreparation.Prepare(input, rule);
            var bonusResult = breakdown.Bonus;
            var salary = MonthlySalaryCalculator.Calculate(breakdown.Gross, amount, active.Length, rule, breakdown.Exempt);
            results.Add(new SalaryMonthResult(year, input.Month, rule, new ReadOnlyCollection<DependentMonth>(active), salary, input.ActualWithheldTax, detail)
                { BaseGrossIncome = input.GrossIncome, BonusTotal = bonusResult.BonusTotal, Bonuses = bonusResult.Lines, Extras = breakdown.Lines });
            gross += breakdown.Gross; exempt += breakdown.Exempt; insurance += amount; withheld += input.ActualWithheldTax; dependentMonths = checked(dependentMonths + active.Length);
        }
        decimal dependentDeduction = dependentMonths * annual.DependentMonthDeduction;
        decimal before = gross - exempt - insurance - annual.AnnualPersonalDeduction - dependentDeduction;
        var retirementDetails = retirement is null ? null : RetirementEvaluator.Evaluate(retirement, provider, before);
        if (retirementDetails is not null)
        {
            var deductions = insurance + annual.AnnualPersonalDeduction + dependentDeduction + retirementDetails.DeductibleTotal;
            before = gross - exempt - deductions;
        }
        var contributionDetails = contributions is null ? null : CharitableContributionEvaluator.Evaluate(contributions, provider, before);
        if (contributionDetails is not null)
        {
            var deductions = insurance + annual.AnnualPersonalDeduction + dependentDeduction + (retirementDetails?.DeductibleTotal ?? 0m) + contributionDetails.EligibleTotal;
            before = gross - exempt - deductions;
        }
        decimal taxable = Math.Max(0, before); var pit = PitCalculator.Calculate(taxable, annual.TaxRules);
        decimal delta = pit.TotalTax.Value - withheld;
        return new(year, new ReadOnlyCollection<SalaryMonthResult>(results), annual, gross, insurance, annual.AnnualPersonalDeduction,
            dependentDeduction, dependentMonths, before, taxable, pit, withheld, delta, Math.Max(0, delta), Math.Max(0, -delta), policy) { ContributionDetails = contributionDetails, RetirementDetails = retirementDetails };
    }
}
