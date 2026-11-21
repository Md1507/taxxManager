namespace TaxManager.Core;

public sealed record InsuranceInput(InsuranceSubject Subject, InsuranceRegion Region, decimal SalaryBase);
public sealed record InsuranceComponentResult(decimal AppliedBase, InsuranceComponentRule Rule, decimal Raw, decimal Applied);
public sealed record InsuranceResult(InsuranceInput Input, LegalBasis InputBasis, TaxRuleSet TaxRuleSet, InsuranceRuleSet Rules,
    SalaryRoundingPolicy Policy, InsuranceComponentResult Social, InsuranceComponentResult Health, InsuranceComponentResult Unemployment,
    decimal TotalRaw, decimal TotalApplied, decimal DeductibleTotal);

public static class InsuranceCalculator
{
    public static InsuranceResult Calculate(InsuranceInput input, TaxRuleSet ruleSet, int year, int month)
    {
        ArgumentNullException.ThrowIfNull(input); ArgumentNullException.ThrowIfNull(ruleSet);
        if (year is < 1 or > 9999) throw new ArgumentOutOfRangeException(nameof(year));
        if (month is < 1 or > 12) throw new ArgumentOutOfRangeException(nameof(month));
        if (!Enum.IsDefined(input.Region)) throw new ArgumentOutOfRangeException(nameof(input.Region));
        if (input.Subject != InsuranceSubject.VietnameseFullMonthEmployee) throw new NotSupportedException("Unsupported insurance subject.");
        if (input.SalaryBase < 0) throw new ArgumentOutOfRangeException(nameof(input.SalaryBase));
        var rules = ruleSet.InsuranceRules ?? throw new InvalidOperationException("Insurance rules are missing.");
        var policy = ruleSet.RoundingPolicy ?? throw new InvalidOperationException("Rounding policy is missing.");
        var date = new DateOnly(year, month, 1);
        if (date < rules.PeriodFrom || date > rules.PeriodTo || input.Subject != rules.Subject || input.Region != rules.Region)
            throw new ArgumentException("Insurance snapshot does not match the requested period, subject or region.");
        if (input.SalaryBase < rules.ScopeMinimumSalary)
            throw new NotSupportedException($"Salary base below full-month minimum {rules.ScopeMinimumSalary} for region {input.Region}, {year:D4}-{month:D2}.");
        InsuranceComponentResult Component(decimal amount, InsuranceComponentRule rule)
        {
            decimal raw = amount * rule.Rate;
            // The user selected NoRounding; raw decimal is the applied amount.
            return new(amount, rule, raw, raw);
        }
        var social = Component(Math.Min(input.SalaryBase, rules.Social.Cap), rules.Social);
        var health = Component(social.AppliedBase, rules.Health);
        var unemployment = Component(Math.Min(input.SalaryBase, rules.Unemployment.Cap), rules.Unemployment);
        var total = social.Raw + health.Raw + unemployment.Raw;
        return new(input, LegalBasis.Missing, ruleSet, rules, policy, social, health, unemployment, total, total, total);
    }
}
