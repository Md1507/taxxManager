using System.Text.Json;
using TaxManager.Core;

namespace TaxManager.Data.TaxRules;

/// <summary>Versioned, lossless representation of the supported manual-insurance annual graph.</summary>
public static class AnnualRuleGraphCodec
{
    private sealed record TaxDto(string Id, string Version, string Period, int FromYear,
        decimal PersonalDeduction, decimal DependentDeduction, Bracket[] Brackets,
        LegalBasis PersonalBasis, LegalBasis DependentBasis, LegalBasis IncomeBasis, LegalBasis TaxBasis)
    {
        internal static TaxDto From(TaxRuleSet rules) => new(rules.Id, rules.Version, rules.Period, rules.FromYear,
            rules.PersonalDeduction, rules.DependentDeduction, rules.Brackets.ToArray(), rules.PersonalDeductionBasis,
            rules.DependentDeductionBasis, rules.TaxableIncomeBasis, rules.TaxBasis);
        internal TaxRuleSet ToCore() => new(Id, Version, Period, FromYear, PersonalDeduction, DependentDeduction,
            Brackets, PersonalBasis, DependentBasis, IncomeBasis, TaxBasis);
    }

    private sealed record AnnualDto(int Year, TaxDto TaxRules, LegalBasis DependentPeriodBasis,
        LegalBasis SettlementBasis, LegalBasis PeriodBasis, LegalBasis TransitionBasis);
    private sealed record Envelope(int SchemaVersion, TaxDto Monthly, SalaryRoundingPolicy Rounding,
        AnnualDto Annual, CashBonusRuleSet? Bonus);

    public static string Encode(TaxRuleSet rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        if (rules.InsuranceRules is not null || rules.SalaryExtraRules is not null ||
            rules.CasualWithholdingRules is not null || rules.MultipleSalaryRules is not null ||
            rules.CharitableContributionRules is not null || rules.RetirementRules is not null)
            throw new NotSupportedException("Only manual-insurance annual and cash-bonus graphs are supported.");
        var annual = rules.AnnualSalaryRules ?? throw new ArgumentException("Annual salary rules are required.", nameof(rules));
        var rounding = rules.RoundingPolicy ?? throw new ArgumentException("Rounding policy is required.", nameof(rules));
        var envelope = new Envelope(1, TaxDto.From(rules), rounding,
            new AnnualDto(annual.Year, TaxDto.From(annual.TaxRules), annual.DependentPeriodBasis,
                annual.SettlementBasis, annual.PeriodBasis, annual.TransitionBasis), rules.CashBonusRules);
        return JsonSerializer.Serialize(envelope);
    }

    public static TaxRuleSet Decode(string payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);
        var graph = JsonSerializer.Deserialize<Envelope>(payload) ?? throw new JsonException("Annual graph is empty.");
        if (graph.SchemaVersion != 1 || graph.Monthly is null || graph.Annual is null || graph.Rounding is null ||
            graph.Monthly.PersonalBasis is null || graph.Monthly.DependentBasis is null ||
            graph.Monthly.IncomeBasis is null || graph.Monthly.TaxBasis is null || graph.Annual.TaxRules is null)
            throw new JsonException("Annual graph is incomplete or has an unsupported version.");
        var monthly = graph.Monthly.ToCore();
        var annual = graph.Annual;
        return new TaxRuleSet(monthly.Id, monthly.Version, monthly.Period, monthly.FromYear,
            monthly.PersonalDeduction, monthly.DependentDeduction, monthly.Brackets,
            monthly.PersonalDeductionBasis, monthly.DependentDeductionBasis, monthly.TaxableIncomeBasis,
            monthly.TaxBasis, roundingPolicy: graph.Rounding,
            annualSalaryRules: new AnnualSalaryRuleSet(annual.Year, annual.TaxRules.ToCore(),
                annual.DependentPeriodBasis, annual.SettlementBasis, annual.PeriodBasis, annual.TransitionBasis),
            cashBonusRules: graph.Bonus);
    }
}
