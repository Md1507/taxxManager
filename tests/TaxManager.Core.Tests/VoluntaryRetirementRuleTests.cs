using TaxManager.Core;
using Xunit;

namespace TaxManager.Core.Tests;

public class VoluntaryRetirementRuleTests
{
    static RetirementRuleSet Seed() => TaxRuleSet.CreateVietnam2026RetirementProvider(InsuranceRegion.I).GetForMonth(2026, 1).RetirementRules!;
    static RetirementRuleSet Copy(RetirementRuleSet s, decimal? cap = null, LegalBasis? basis = null,
        IEnumerable<RetirementKindRule>? kinds = null, IEnumerable<RetirementProofRule>? proofs = null, DateOnly? from = null, DateOnly? to = null) =>
        new(s.Id, s.Version, s.Year, from ?? s.PeriodFrom, to ?? s.PeriodTo, s.Scope, s.Timing, s.Mode, cap ?? s.MonthlyCap,
            kinds ?? s.Kinds, proofs ?? s.Proofs, basis ?? s.PoolCapBasis, s.DeductionBasis, s.LawBasis, s.PeriodBasis,
            s.ApplicabilityBasis, s.TransitionBasis, s.OverpaymentContextBasis);
    static LegalBasis Basis(string id = "fixture", string version = "1", string article = "46", string document = "253/2026/ND-CP",
        string url = "https://example.org/law", string period = "2026", DateOnly? from = null, DateOnly? to = null) =>
        new(VerificationStatus.Unverified, id, version, article: article, documentNumber: document, sourceUrl: url, period: period, effectiveFrom: from, effectiveTo: to);
    static TaxRuleSet WithRule(TaxRuleSet s, RetirementRuleSet? rule) => new(s.Id, s.Version, s.Period, s.FromYear, s.PersonalDeduction, s.DependentDeduction,
        s.Brackets, s.PersonalDeductionBasis, s.DependentDeductionBasis, s.TaxableIncomeBasis, s.TaxBasis, s.InsuranceRules, s.RoundingPolicy,
        s.AnnualSalaryRules, s.CashBonusRules, s.SalaryExtraRules, s.CasualWithholdingRules, s.MultipleSalaryRules, s.CharitableContributionRules, rule);

    [Fact]
    public void BasisAndCollectionGuardsHaveAValidControl()
    {
        var s = Seed(); Assert.Equal(3_000_000m, Copy(s, basis: Basis()).MonthlyCap);
        Assert.Throws<ArgumentException>(() => Copy(s, basis: LegalBasis.Missing));
        Assert.Throws<ArgumentException>(() => Copy(s, basis: Basis(id: "")));
        Assert.Throws<ArgumentException>(() => Copy(s, basis: Basis(version: "2")));
        Assert.Throws<ArgumentException>(() => Copy(s, basis: Basis(article: "")));
        Assert.Throws<ArgumentException>(() => Copy(s, basis: Basis(document: "")));
        Assert.Throws<ArgumentException>(() => Copy(s, basis: Basis(url: "http://example.org/law")));
        Assert.Throws<ArgumentException>(() => Copy(s, basis: Basis(period: "2025")));
        Assert.Throws<ArgumentException>(() => Copy(s, basis: Basis(from: new(2026, 1, 2))));
        Assert.Throws<ArgumentException>(() => Copy(s, basis: Basis(to: new(2026, 12, 30))));
        Assert.Throws<ArgumentOutOfRangeException>(() => Copy(s, cap: -1));
        Assert.Throws<ArgumentException>(() => Copy(s, kinds: [s.Kinds[0], s.Kinds[0], s.Kinds[2]]));
        Assert.Throws<ArgumentException>(() => Copy(s, proofs: [s.Proofs[0], s.Proofs[0]]));
        var kinds = s.Kinds.ToList(); var proofs = s.Proofs.ToList(); var copy = Copy(s, kinds: kinds, proofs: proofs);
        kinds.Clear(); proofs.Clear(); Assert.Equal(3, copy.Kinds.Count); Assert.Equal(2, copy.Proofs.Count);
        Assert.Throws<NotSupportedException>(() => ((IList<RetirementKindRule>)copy.Kinds).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<RetirementProofRule>)copy.Proofs).Clear());
    }

    [Fact]
    public void SeedAndOldFactoriesKeepTheirProvenance()
    {
        Assert.Null(TaxRuleSet.CreateVietnam2026AnnualSalaryProvider(InsuranceRegion.I).GetForMonth(2026, 1).RetirementRules);
        var s = Seed();
        var bases = s.Kinds.Select(x => x.Basis).Concat(s.Proofs.Select(x => x.Basis)).Concat(
            [s.PoolCapBasis, s.DeductionBasis, s.LawBasis, s.PeriodBasis, s.ApplicabilityBasis, s.TransitionBasis, s.OverpaymentContextBasis]);
        Assert.All(bases, b => { Assert.Equal(VerificationStatus.Unverified, b.Status); Assert.Null(b.VerifiedOn); });
        Assert.Equal(3_000_000m, s.MonthlyCap); Assert.Equal(new DateOnly(2026, 1, 1), s.PeriodFrom); Assert.Equal(new DateOnly(2026, 12, 31), s.PeriodTo);
    }

    [Fact]
    public void CustomMonthlyCapIsUsedAndMissingSnapshotRejectedEvenWhenEmpty()
    {
        var old = TaxRuleSet.CreateVietnam2026RetirementProvider(InsuranceRegion.I); var first = old.GetForMonth(2026, 1); var last = old.GetForMonth(2026, 7);
        var custom = new TaxRuleProvider([new(WithRule(first, Copy(Seed(), cap: 2_000_000m)), new(2026, 1, 1), new(2026, 6, 30)),
            new(WithRule(last, Copy(Seed(), cap: 2_000_000m)), new(2026, 7, 1), new(2026, 12, 31))]);
        var salary = Enumerable.Range(1, 12).Select(m => new SalaryMonthInput(2026, m, 30_000_000m, 0, 1_000_000m));
        RetirementYearInput Input(bool filled) => new(2026, RetirementScope.VietnameseResidentDomesticVndCalendarYear, "person", true, true, true,
            Enumerable.Range(1, 12).Select(m => new RetirementMonthInput(2026, m, filled ?
                [new($"x{m}", new(2026, m, 1), new(2026, m, 1), 3_000_000m, RetirementKind.VoluntaryRetirementInsurance,
                    RetirementPaidBy.Self, "person", "insurer", "contract", true, true, true, true,
                    [new(RetirementEvidenceKind.ReceiptCopy, "receipt", new(2026, m, 1), "insurer", 3_000_000m, true)])] : [])));
        var result = AnnualSalaryCalculator.CalculateWithRetirement(2026, salary, [], custom, Input(true));
        Assert.Equal(24_000_000m, result.RetirementDeduction); Assert.Equal(138_000_000m, result.TaxableIncome);
        Assert.Equal(7_800_000m, result.RawTax);
        var missing = new TaxRuleProvider([new(first, new(2026, 1, 1), new(2026, 6, 30)), new(WithRule(last, null), new(2026, 7, 1), new(2026, 12, 31))]);
        Assert.Throws<InvalidOperationException>(() => AnnualSalaryCalculator.CalculateWithRetirement(2026, salary, [], missing, Input(false)));
    }

    [Fact]
    public void DifferentHalfYearVersionsAndCapsRemainAttachedToEachMonth()
    {
        var source = TaxRuleSet.CreateVietnam2026RetirementProvider(InsuranceRegion.I); var h1 = source.GetForMonth(2026, 1); var h2 = source.GetForMonth(2026, 7);
        var seed = Seed(); LegalBasis B(string id) => Basis(id: id, version: "2");
        var second = new RetirementRuleSet(seed.Id, "2", 2026, new(2026, 7, 1), new(2026, 12, 31), seed.Scope, seed.Timing, seed.Mode, 2_000_000m,
            seed.Kinds.Select(x => new RetirementKindRule(x.Kind, B("kind-" + x.Kind))), seed.Proofs.Select(x => new RetirementProofRule(x.Kind, B("proof-" + x.Kind))),
            B("cap"), B("deduction"), B("law"), B("period"), B("applicability"), B("transition"), B("overpayment"));
        var firstSnapshot = WithRule(h1, Copy(seed, from: new(2026, 1, 1), to: new(2026, 6, 30)));
        var secondSnapshot = WithRule(h2, second);
        var provider = new TaxRuleProvider([new(firstSnapshot, new(2026, 1, 1), new(2026, 6, 30)), new(secondSnapshot, new(2026, 7, 1), new(2026, 12, 31))]);
        var inputs = Enumerable.Range(1, 12).Select(m => new RetirementMonthInput(2026, m,
            [new($"i{m}", new(2026, m, 15), new(2026, m, 1), 4_000_000m, RetirementKind.LifeInsurance, RetirementPaidBy.Self,
                "person", "insurer", "contract", true, true, true, true,
                [new(RetirementEvidenceKind.ReceiptCopy, "receipt", new(2026, m, 16), "insurer", 4_000_000m, true)])]));
        var input = new RetirementYearInput(2026, RetirementScope.VietnameseResidentDomesticVndCalendarYear, "person", true, true, true, inputs);
        var salary = Enumerable.Range(1, 12).Select(m => new SalaryMonthInput(2026, m, 0, 0, 0));
        var result = AnnualSalaryCalculator.CalculateWithRetirement(2026, salary, [], provider, input).RetirementDetails!;
        Assert.Equal(30_000_000m, result.DeductibleTotal);
        Assert.Same(firstSnapshot, result.Months[5].RuleSet); Assert.Same(secondSnapshot, result.Months[6].RuleSet);
        Assert.Equal("1", result.Months[5].Rules.Version); Assert.Equal("2", result.Months[6].Rules.Version);
        Assert.Equal("2", result.Months[6].Lines[0].KindRule.Basis.Version);
    }

    [Fact]
    public void CustomCapAndCustomTaxAreBothApplied()
    {
        var source = TaxRuleSet.CreateVietnam2026RetirementProvider(InsuranceRegion.I); var first = source.GetForMonth(2026, 1); var last = source.GetForMonth(2026, 7);
        var oldAnnual = first.AnnualSalaryRules!; var oldTax = oldAnnual.TaxRules;
        var annualTax = new TaxRuleSet("custom-year", "1", "2026 annual", 2026, 120_000_000m, 2_000_000m,
            [new(60_000_000m, .10m, oldTax.TaxBasis), new(null, .20m, oldTax.TaxBasis)], oldTax.PersonalDeductionBasis, oldTax.DependentDeductionBasis, oldTax.TaxableIncomeBasis, oldTax.TaxBasis);
        var annual = new AnnualSalaryRuleSet(2026, annualTax, oldAnnual.DependentPeriodBasis, oldAnnual.SettlementBasis, oldAnnual.PeriodBasis, oldAnnual.TransitionBasis);
        TaxRuleSet Make(TaxRuleSet s) => new("custom-month", "1", "2026 monthly", 2026, 10_000_000m, 2_000_000m,
            [new(5_000_000m, .10m, s.TaxBasis), new(null, .20m, s.TaxBasis)], s.PersonalDeductionBasis, s.DependentDeductionBasis, s.TaxableIncomeBasis, s.TaxBasis,
            s.InsuranceRules, s.RoundingPolicy, annual, s.CashBonusRules, s.SalaryExtraRules, s.CasualWithholdingRules, s.MultipleSalaryRules, s.CharitableContributionRules, Copy(Seed(), cap: 2_000_000m));
        var provider = new TaxRuleProvider([new(Make(first), new(2026, 1, 1), new(2026, 6, 30)), new(Make(last), new(2026, 7, 1), new(2026, 12, 31))]);
        var items = Enumerable.Range(1, 12).Select(m => new RetirementMonthInput(2026, m,
            [new($"p{m}", new(2026, m, 1), new(2026, m, 1), 3_000_000m, RetirementKind.VoluntaryRetirementInsurance, RetirementPaidBy.Self,
                "person", "insurer", "contract", true, true, true, true, [new(RetirementEvidenceKind.ReceiptCopy, "r", new(2026, m, 1), "insurer", 3_000_000m, true)])]));
        var input = new RetirementYearInput(2026, RetirementScope.VietnameseResidentDomesticVndCalendarYear, "person", true, true, true, items);
        var salary = Enumerable.Range(1, 12).Select(m => new SalaryMonthInput(2026, m, 30_000_000m, 3_000_000m, 1_000_000m));
        var dependents = new[] { new DependentPeriod("child", new(2026, 7, 1), new(2026, 12, 1)) };
        var r = AnnualSalaryCalculator.CalculateWithRetirement(2026, salary, dependents, provider, input);
        Assert.Equal(24_000_000m, r.RetirementDeduction); Assert.Equal(192_000_000m, r.TaxableIncome);
        Assert.Equal(32_400_000m, r.RawTax); Assert.Equal(3_600_000m, r.EstimatedOverpaid);
        var charity = new CharitableContributionYearInput(2026, ContributionScope.VietnameseResidentDomesticVndCalendarYear, true, true,
            [new("gift", new(2026, 1, 1), 16_000_000m, "fund", ContributionRecipientKind.CharitableHumanitarianStudyPromotionFund, true, true, true,
                [new(ContributionEvidenceKind.LegalReceiptCopy, "receipt", new(2026, 1, 1), true, false)])]);
        var both = AnnualSalaryCalculator.CalculateWithRetirement(2026, salary, dependents, provider, input, charity);
        Assert.Equal(176_000_000m, both.TaxableIncome); Assert.Equal(29_200_000m, both.RawTax);
        Assert.Equal(6_800_000m, both.EstimatedOverpaid); Assert.Equal(16_000_000m, both.IncomeReduction);
    }

    [Fact]
    public void RuleGuardsCheckEachUnsupportedValueAndFutureYear()
    {
        var s = Seed();
        RetirementRuleSet Make(RetirementScope scope, RetirementTiming timing, RetirementMode mode, IEnumerable<RetirementKindRule> kinds, IEnumerable<RetirementProofRule> proofs) =>
            new(s.Id, s.Version, s.Year, s.PeriodFrom, s.PeriodTo, scope, timing, mode, s.MonthlyCap, kinds, proofs, s.PoolCapBasis,
                s.DeductionBasis, s.LawBasis, s.PeriodBasis, s.ApplicabilityBasis, s.TransitionBasis, s.OverpaymentContextBasis);
        Assert.Equal(3, Make(s.Scope, s.Timing, s.Mode, s.Kinds, s.Proofs).Kinds.Count);
        foreach (int value in new[] { 0, 99 })
        {
            Assert.Throws<ArgumentException>(() => Make((RetirementScope)value, s.Timing, s.Mode, s.Kinds, s.Proofs));
            Assert.Throws<ArgumentException>(() => Make(s.Scope, (RetirementTiming)value, s.Mode, s.Kinds, s.Proofs));
            Assert.Throws<ArgumentException>(() => Make(s.Scope, s.Timing, (RetirementMode)value, s.Kinds, s.Proofs));
            Assert.Throws<ArgumentException>(() => Make(s.Scope, s.Timing, s.Mode, [new((RetirementKind)value, Basis()), s.Kinds[1], s.Kinds[2]], s.Proofs));
            Assert.Throws<ArgumentException>(() => Make(s.Scope, s.Timing, s.Mode, s.Kinds, [new((RetirementEvidenceKind)value, Basis()), s.Proofs[1]]));
        }
        Assert.Throws<ArgumentNullException>(() => Make(s.Scope, s.Timing, s.Mode, null!, s.Proofs));
        Assert.Throws<ArgumentNullException>(() => Make(s.Scope, s.Timing, s.Mode, s.Kinds, null!));
        Assert.Throws<ArgumentException>(() => Make(s.Scope, s.Timing, s.Mode, [null!, s.Kinds[1], s.Kinds[2]], s.Proofs));
        Assert.Throws<ArgumentException>(() => Make(s.Scope, s.Timing, s.Mode, s.Kinds, [null!, s.Proofs[1]]));
        Assert.Throws<ArgumentException>(() => Copy(s, from: new(2026, 12, 31), to: new(2026, 1, 1)));
        LegalBasis Future(string id) => Basis(id: id, period: "2027");
        Assert.Throws<ArgumentException>(() => new RetirementRuleSet(s.Id, "1", 2027, new(2027, 1, 1), new(2027, 12, 31), s.Scope, s.Timing, s.Mode, s.MonthlyCap,
            s.Kinds.Select(x => new RetirementKindRule(x.Kind, Future("k" + x.Kind))), s.Proofs.Select(x => new RetirementProofRule(x.Kind, Future("p" + x.Kind))),
            Future("cap"), Future("d"), Future("law"), Future("period"), Future("a"), Future("t"), Future("o")));
    }

    [Fact]
    public void EmptyMonthsStillRequireCompleteRuleCoverage()
    {
        var source = TaxRuleSet.CreateVietnam2026RetirementProvider(InsuranceRegion.I); var first = source.GetForMonth(2026, 1);
        var partial = WithRule(first, Copy(Seed(), from: new(2026, 1, 2)));
        var shortProvider = new TaxRuleProvider([new(partial, new(2026, 1, 1), new(2026, 12, 31))]);
        var months = Enumerable.Range(1, 12).Select(m => new SalaryMonthInput(2026, m, 0, 0, 0));
        var input = new RetirementYearInput(2026, RetirementScope.VietnameseResidentDomesticVndCalendarYear, "person", true, true, true,
            Enumerable.Range(1, 12).Select(m => new RetirementMonthInput(2026, m, [])));
        _ = AnnualSalaryCalculator.CalculateWithRetirement(2026, months, [], source, input);
        Assert.Throws<ArgumentException>(() => AnnualSalaryCalculator.CalculateWithRetirement(2026, months, [], shortProvider, input));
    }

    [Fact]
    public void NullAndMissingRequiredBasisAreRejected()
    {
        var s = Seed();
        RetirementRuleSet Build(LegalBasis basis) => new(s.Id, s.Version, s.Year, s.PeriodFrom, s.PeriodTo, s.Scope, s.Timing, s.Mode, s.MonthlyCap,
            s.Kinds, s.Proofs, basis, s.DeductionBasis, s.LawBasis, s.PeriodBasis, s.ApplicabilityBasis, s.TransitionBasis, s.OverpaymentContextBasis);
        Assert.Equal(s.MonthlyCap, Build(Basis()).MonthlyCap);
        Assert.Throws<ArgumentNullException>(() => Build(null!));
        Assert.Throws<ArgumentException>(() => Build(LegalBasis.Missing));
        Assert.Throws<ArgumentNullException>(() => Copy(s, kinds: [new(s.Kinds[0].Kind, null!), s.Kinds[1], s.Kinds[2]]));
        Assert.Throws<ArgumentException>(() => Copy(s, proofs: [new(s.Proofs[0].Kind, LegalBasis.Missing), s.Proofs[1]]));
    }

    [Fact]
    public void RootPeriodYearAndProviderGapCannotPassEvenForZeroMonths()
    {
        var source = TaxRuleSet.CreateVietnam2026RetirementProvider(InsuranceRegion.I); var first = source.GetForMonth(2026, 1);
        TaxRuleSet Root(string period, int year) => new(first.Id, first.Version, period, year, first.PersonalDeduction, first.DependentDeduction,
            first.Brackets, first.PersonalDeductionBasis, first.DependentDeductionBasis, first.TaxableIncomeBasis, first.TaxBasis, first.InsuranceRules,
            first.RoundingPolicy, first.AnnualSalaryRules, first.CashBonusRules, first.SalaryExtraRules, first.CasualWithholdingRules,
            first.MultipleSalaryRules, first.CharitableContributionRules, first.RetirementRules);
        TaxRuleProvider Wrap(TaxRuleSet root) => new([new(root, new(2026, 1, 1), new(2026, 12, 31))]);
        var gap = new TaxRuleProvider([new(first, new(2026, 1, 1), new(2026, 6, 30))]);
        var months = Enumerable.Range(1, 12).Select(m => new SalaryMonthInput(2026, m, 0, 0, 0));
        var input = new RetirementYearInput(2026, RetirementScope.VietnameseResidentDomesticVndCalendarYear, "person", true, true, true,
            Enumerable.Range(1, 12).Select(m => new RetirementMonthInput(2026, m, [])));
        void Calc(TaxRuleProvider p) => AnnualSalaryCalculator.CalculateWithRetirement(2026, months, [], p, input);
        Calc(Wrap(first));
        Assert.Throws<ArgumentException>(() => Calc(Wrap(Root("2026 annual", 2026))));
        Assert.Throws<ArgumentException>(() => Calc(Wrap(Root(first.Period, 2025))));
        Assert.Throws<NotSupportedException>(() => Calc(gap));
    }
}
