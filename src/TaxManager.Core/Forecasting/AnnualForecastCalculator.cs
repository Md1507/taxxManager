namespace TaxManager.Core.Forecasting;

public sealed record ForecastMonth(int Month, ForecastValue<decimal>? Gross,
    ForecastValue<decimal>? Insurance, ForecastValue<int>? Dependents,
    ForecastValue<decimal>? Withheld);

public enum ForecastInputKind { Gross, Insurance, Dependents }
public sealed record ForecastMissingInput(int Month, ForecastInputKind Kind);
public sealed record ForecastCompleteness(IReadOnlyList<ForecastMissingInput> MissingObligation,
    IReadOnlyList<int> MissingWithholdingMonths, int ActualObligationMonths, int WithholdingMonths);

// Excludes the annual engine's internal zero placeholder for missing withholding.
public sealed record AnnualForecastDetails(decimal GrossIncome, decimal BonusTotal,
    decimal BonusAlreadyInGross, decimal Insurance, decimal PersonalDeduction,
    decimal DependentDeduction, decimal TaxableIncome, PitResult Pit,
    AnnualSalaryRuleSet Rules, SalaryRoundingPolicy Policy, IReadOnlyList<TaxRuleSet> MonthlyRules);

public sealed record AnnualForecastResult(decimal Obligation, decimal? Difference,
    int ActualObligationMonths, int WithholdingMonths, int ActualWithholdingMonths,
    decimal ActualWithheld, decimal ForecastWithheld)
{
    public AnnualForecastDetails Details { get; init; } = null!;
}

public sealed record ForecastBonus(string Id, int Month, ForecastValue<decimal> Amount, bool IncludedInGross);
public sealed record AnnualForecastScenarios(AnnualForecastResult Base, AnnualForecastResult WithBonus);

public static class AnnualForecastCalculator
{
    public static ForecastCompleteness Assess(IEnumerable<ForecastMonth> months,
        IEnumerable<ForecastBonus>? bonuses = null)
    {
        ArgumentNullException.ThrowIfNull(months);
        var input = months.ToArray();
        if (input.Length != 12 || input.Any(m => m is null || m.Month is < 1 or > 12) ||
            input.Select(m => m.Month).Distinct().Count() != 12)
            throw new ArgumentException("Exactly twelve distinct months are required.", nameof(months));
        var bonusList = (bonuses ?? []).ToArray();
        if (bonusList.Any(b => b is null || b.Month is < 1 or > 12 || b.Amount is null ||
            b.Amount.Value < 0 || string.IsNullOrWhiteSpace(b.Id)) ||
            bonusList.Select(b => b.Id).Distinct(StringComparer.Ordinal).Count() != bonusList.Length)
            throw new ArgumentException("Bonus IDs, months and amounts must be valid and unique.", nameof(bonuses));
        var missing = new List<ForecastMissingInput>();
        foreach (var month in input.OrderBy(m => m.Month))
        {
            if (month.Gross is null) missing.Add(new(month.Month, ForecastInputKind.Gross));
            if (month.Insurance is null) missing.Add(new(month.Month, ForecastInputKind.Insurance));
            if (month.Dependents is null) missing.Add(new(month.Month, ForecastInputKind.Dependents));
        }
        return new ForecastCompleteness(missing.AsReadOnly(),
            Array.AsReadOnly(input.Where(m => m.Withheld is null).Select(m => m.Month).Order().ToArray()),
            input.Count(m => m.Gross?.Status == ForecastStatus.Actual &&
                m.Insurance?.Status == ForecastStatus.Actual && m.Dependents?.Status == ForecastStatus.Actual &&
                bonusList.Where(b => b.Month == m.Month).All(b => b.Amount.Status == ForecastStatus.Actual)),
            input.Count(m => m.Withheld is not null));
    }

    public static AnnualForecastResult Calculate(int year, IEnumerable<ForecastMonth> months, TaxRuleProvider provider)
        => CalculateCore(year, months, [], provider);

    public static AnnualForecastResult Calculate(int year, IEnumerable<ForecastMonth> months,
        IEnumerable<ForecastBonus> bonuses, TaxRuleProvider provider)
    {
        ArgumentNullException.ThrowIfNull(bonuses);
        return CalculateCore(year, months, bonuses, provider);
    }

    public static AnnualForecastScenarios CalculateScenarios(int year, IEnumerable<ForecastMonth> months,
        IEnumerable<ForecastBonus> existing, ForecastValue<decimal>? additional, int? bonusMonth, TaxRuleProvider provider)
    {
        ArgumentNullException.ThrowIfNull(months);
        ArgumentNullException.ThrowIfNull(existing);
        var input = months.ToArray();
        var bonuses = existing.ToArray();
        if ((additional is null) != (bonusMonth is null)) throw new ArgumentException("Bonus amount and month must be supplied together.");
        if (additional is not null && additional.Status != ForecastStatus.Assumed)
            throw new ArgumentException("An additional scenario bonus must be an assumption.", nameof(additional));
        var baseline = CalculateCore(year, input, bonuses, provider);
        if (additional is null) return new AnnualForecastScenarios(baseline, baseline);
        var scenarioBonuses = bonuses.Append(new ForecastBonus(Guid.NewGuid().ToString("N"), bonusMonth!.Value, additional, false));
        return new AnnualForecastScenarios(baseline, CalculateCore(year, input, scenarioBonuses, provider));
    }

    private static AnnualForecastResult CalculateCore(int year, IEnumerable<ForecastMonth> months,
        IEnumerable<ForecastBonus> bonuses, TaxRuleProvider provider)
    {
        ArgumentNullException.ThrowIfNull(months);
        ArgumentNullException.ThrowIfNull(provider);
        var input = months.ToArray();
        if (input.Length != 12 || input.Any(m => m is null || m.Month is < 1 or > 12) ||
            input.Select(m => m.Month).Distinct().Count() != 12)
            throw new ArgumentException("Exactly twelve distinct months are required.", nameof(months));
        if (input.Any(m => m.Gross is null || m.Insurance is null || m.Dependents is null))
            throw new InvalidOperationException("Gross, insurance and dependents are required for every month.");
        if (input.Any(m => m.Gross!.Value < 0 || m.Insurance!.Value < 0 || m.Dependents!.Value < 0 || m.Withheld?.Value < 0))
            throw new ArgumentOutOfRangeException(nameof(months));
        var bonusList = bonuses.ToArray();
        if (bonusList.Any(b => b is null || b.Month is < 1 or > 12 || b.Amount is null || b.Amount.Value < 0 || string.IsNullOrWhiteSpace(b.Id)) ||
            bonusList.Select(b => b.Id).Distinct(StringComparer.Ordinal).Count() != bonusList.Length)
            throw new ArgumentException("Bonus IDs, months and amounts must be valid and unique.", nameof(bonuses));
        foreach (var month in input)
        {
            if (bonusList.Where(b => b.Month == month.Month && b.IncludedInGross).Sum(b => b.Amount.Value) > month.Gross!.Value)
                throw new ArgumentException("Bonuses already included in gross cannot exceed gross.", nameof(bonuses));
            var included = bonusList.Where(b => b.Month == month.Month && b.IncludedInGross).ToArray();
            if (included.Length > 0)
            {
                var check = new SalaryMonthInput(year, month.Month, month.Gross!.Value, 0m, month.Insurance!.Value,
                    bonuses: included.Select(b => new CashBonus(b.Id, b.Amount.Value, new DateOnly(year, month.Month, 1),
                        CashBonusTreatment.TaxableCashFromCurrentEmployer)));
                _ = CashBonusCalculation.Prepare(check, provider.GetForMonth(year, month.Month));
            }
        }

        // The existing annual engine requires a withholding number even though tax obligation
        // does not depend on it. A zero used only inside this adapter is never returned or saved
        // as an input; the delta is withheld until every month has an explicit value.
        var calculatorInputs = input.Select(m => new SalaryMonthInput(year, m.Month,
            m.Gross!.Value, m.Withheld?.Value ?? 0m, m.Insurance!.Value,
            bonuses: bonusList.Where(b => b.Month == m.Month && !b.IncludedInGross)
                .Select(b => new CashBonus(b.Id, b.Amount.Value, new DateOnly(year, m.Month, 1), CashBonusTreatment.TaxableCashFromCurrentEmployer)))).ToArray();
        var dependents = input.SelectMany(m => Enumerable.Range(0, m.Dependents!.Value)
            .Select(index => new DependentPeriod($"{m.Month}-{index}",
                new DateOnly(year, m.Month, 1), new DateOnly(year, m.Month, 1)))).ToArray();
        var calculated = AnnualSalaryCalculator.Calculate(year, calculatorInputs, dependents, provider);
        var actual = input.Count(m => m.Gross!.Status == ForecastStatus.Actual &&
            m.Insurance!.Status == ForecastStatus.Actual && m.Dependents!.Status == ForecastStatus.Actual &&
            bonusList.Where(b => b.Month == m.Month).All(b => b.Amount.Status == ForecastStatus.Actual));
        var withholding = input.Count(m => m.Withheld is not null);
        return new AnnualForecastResult(calculated.Pit.TotalTax.Value,
            withholding == 12 ? calculated.Delta : null, actual, withholding,
            input.Count(m => m.Withheld?.Status == ForecastStatus.Actual),
            input.Where(m => m.Withheld?.Status == ForecastStatus.Actual).Sum(m => m.Withheld!.Value),
            input.Where(m => m.Withheld is not null && m.Withheld.Status != ForecastStatus.Actual).Sum(m => m.Withheld!.Value))
            { Details = new AnnualForecastDetails(calculated.GrossIncome, calculated.BonusTotal,
                bonusList.Where(b => b.IncludedInGross).Sum(b => b.Amount.Value), calculated.Insurance,
                calculated.PersonalDeduction, calculated.DependentDeduction, calculated.TaxableIncome,
                calculated.Pit, calculated.Rules, calculated.Policy,
                Array.AsReadOnly(calculated.Months.Select(m => m.RuleSet).ToArray())) };
    }
}
