using TaxManager.Core;
using Xunit;

namespace TaxManager.Core.Tests;

public class CharitableContributionRuleTests
{
    static CharitableContributionRuleSet Seed() => TaxRuleSet.CreateVietnam2026CharitableContributionProvider(InsuranceRegion.I).GetForMonth(2026, 1).CharitableContributionRules!;
    static CharitableContributionRuleSet Copy(CharitableContributionRuleSet s, IEnumerable<ContributionRecipientRule>? recipients = null, IEnumerable<ContributionProofRule>? proofs = null, LegalBasis? conditions = null, DateOnly? from = null, DateOnly? to = null, string version = "1") => new(s.Id, version, s.Year, from ?? s.PeriodFrom, to ?? s.PeriodTo, s.Scope, s.Timing, s.Mode, recipients ?? s.Recipients, proofs ?? s.Proofs, conditions ?? s.ConditionsBasis, s.SelfPaidBasis, s.DeductionBasis, s.NoCarryoverBasis, s.PeriodBasis, s.ApplicabilityBasis, s.TransitionBasis, s.OverpaymentContextBasis);
    static LegalBasis Basis(string id = "test", string version = "1", string article = "49", string document = "253/2026/ND-CP", string url = "https://example.org/law", string period = "2026", DateOnly? from = null, DateOnly? to = null) => new(VerificationStatus.Unverified, id, version, article: article, documentNumber: document, sourceUrl: url, period: period, effectiveFrom: from, effectiveTo: to);
    [Fact]
    public void BasisAndCollectionGuards()
    {
        var s = Seed(); _ = Copy(s, conditions: Basis());
        Assert.Throws<ArgumentException>(() => Copy(s, conditions: LegalBasis.Missing));
        Assert.Throws<ArgumentException>(() => Copy(s, conditions: Basis(id: "")));
        Assert.Throws<ArgumentException>(() => Copy(s, conditions: Basis(article: "")));
        Assert.Throws<ArgumentException>(() => Copy(s, conditions: Basis(document: "")));
        Assert.Throws<ArgumentException>(() => Copy(s, conditions: Basis(url: "http://example.org/law")));
        Assert.Throws<ArgumentException>(() => Copy(s, conditions: Basis(period: "2025")));
        Assert.Throws<ArgumentException>(() => Copy(s, conditions: Basis(version: "2")));
        Assert.Throws<ArgumentException>(() => Copy(s, conditions: Basis(from: new(2026, 1, 2))));
        Assert.Throws<ArgumentException>(() => Copy(s, conditions: Basis(to: new(2026, 12, 30))));
        Assert.Throws<ArgumentException>(() => Copy(s, from: new(2026, 12, 31), to: new(2026, 1, 1)));
        Assert.Throws<ArgumentException>(() => Copy(s, recipients: []));
        Assert.Throws<ArgumentException>(() => Copy(s, recipients: [s.Recipients[0], s.Recipients[0]]));
        Assert.Throws<ArgumentException>(() => Copy(s, proofs: [new((ContributionEvidenceKind)99, Basis())]));
        var recipients = s.Recipients.ToList(); var copy = Copy(s, recipients: recipients); recipients.Clear();
        Assert.Equal(3, copy.Recipients.Count); Assert.Throws<NotSupportedException>(() => ((IList<ContributionRecipientRule>)copy.Recipients).Clear());
    }
    [Fact]
    public void NewFactoriesPreserveOldFactoriesAndMetadata()
    {
        var old = TaxRuleSet.CreateVietnam2026AnnualSalaryProvider(InsuranceRegion.I);
        Assert.Null(old.GetForMonth(2026, 1).CharitableContributionRules);
        var fresh = TaxRuleSet.CreateVietnam2026CharitableContributionProvider(InsuranceRegion.I);
        foreach (var m in new[] { 1, 6, 7, 12 })
        {
            var s = fresh.GetForMonth(2026, m); var rule = s.CharitableContributionRules!;
            var original = old.GetForMonth(2026, m).AnnualSalaryRules!;
            Assert.Equal(original.AnnualPersonalDeduction, s.AnnualSalaryRules!.AnnualPersonalDeduction);
            Assert.Equal(original.DependentMonthDeduction, s.AnnualSalaryRules.DependentMonthDeduction);
            Assert.Equal(original.TaxRules.Brackets, s.AnnualSalaryRules.TaxRules.Brackets);
            Assert.Equal(original.PeriodBasis, s.AnnualSalaryRules.PeriodBasis);
            Assert.All(rule.Recipients.Select(x => x.Basis).Concat(rule.Proofs.Select(x => x.Basis)), b => { Assert.Equal(VerificationStatus.Unverified, b.Status); Assert.Null(b.VerifiedOn); });
            Assert.Equal(new DateOnly(2026, 1, 1), rule.PeriodFrom); Assert.Equal(new DateOnly(2026, 12, 31), rule.PeriodTo);
        }
        Assert.NotNull(TaxRuleSet.CreateVietnam2026MultipleSalaryCharitableContributionProvider(InsuranceRegion.I).GetForMonth(2026, 1).MultipleSalaryRules);
    }
    static TaxRuleSet WithRule(TaxRuleSet s, CharitableContributionRuleSet? rule) => new(s.Id, s.Version, s.Period, s.FromYear, s.PersonalDeduction, s.DependentDeduction, s.Brackets, s.PersonalDeductionBasis, s.DependentDeductionBasis, s.TaxableIncomeBasis, s.TaxBasis, s.InsuranceRules, s.RoundingPolicy, s.AnnualSalaryRules, s.CashBonusRules, s.SalaryExtraRules, s.CasualWithholdingRules, s.MultipleSalaryRules, rule);
    [Fact]
    public void EmptyInputStillChecksEverySnapshotAndCoverage()
    {
        var provider = TaxRuleSet.CreateVietnam2026CharitableContributionProvider(InsuranceRegion.I);
        var source = provider.GetForMonth(2026, 1); var s = Seed();
        var months = Enumerable.Range(1, 12).Select(m => new SalaryMonthInput(2026, m, 0, 0, 0)).ToArray();
        var input = new CharitableContributionYearInput(2026, ContributionScope.VietnameseResidentDomesticVndCalendarYear, true, true, []);
        _ = AnnualSalaryCalculator.Calculate(2026, months, [], provider, input);
        var gap = new TaxRuleProvider([new(source, new(2026, 1, 1), new(2026, 6, 30)), new(WithRule(source, null), new(2026, 7, 1), new(2026, 12, 31))]);
        Assert.Throws<InvalidOperationException>(() => AnnualSalaryCalculator.Calculate(2026, months, [], gap, input));
        var partial = Copy(s, from: new(2026, 1, 2));
        var shortCoverage = new TaxRuleProvider([new(WithRule(source, partial), new(2026, 1, 1), new(2026, 12, 31))]);
        Assert.Throws<ArgumentException>(() => AnnualSalaryCalculator.Calculate(2026, months, [], shortCoverage, input));
        var missingMonth = new TaxRuleProvider([new(source, new(2026, 1, 1), new(2026, 6, 30)), new(source, new(2026, 8, 1), new(2026, 12, 31))]);
        Assert.Throws<NotSupportedException>(() => AnnualSalaryCalculator.Calculate(2026, months, [], missingMonth, input));
    }
    [Fact]
    public void CustomTaxAndRecipientProofEntriesAreUsed()
    {
        var source = TaxRuleSet.CreateVietnam2026CharitableContributionProvider(InsuranceRegion.I).GetForMonth(2026, 1);
        var prior = source.AnnualSalaryRules!; var tax = prior.TaxRules;
        var customAnnual = new TaxRuleSet(tax.Id, tax.Version, tax.Period, tax.FromYear, 120_000_000m, 2_000_000m,
            [new(60_000_000m, .1m, tax.TaxBasis), new(null, .2m, tax.TaxBasis)], tax.PersonalDeductionBasis, tax.DependentDeductionBasis, tax.TaxableIncomeBasis, tax.TaxBasis);
        var annual = new AnnualSalaryRuleSet(2026, customAnnual, prior.DependentPeriodBasis, prior.SettlementBasis, prior.PeriodBasis, prior.TransitionBasis);
        var rule = Copy(Seed(), recipients: [Seed().Recipients[1]], proofs: [Seed().Proofs[1]]);
        var custom = new TaxRuleSet(source.Id, source.Version, source.Period, source.FromYear, 10_000_000m, 2_000_000m,
            [new(5_000_000m, .1m, source.TaxBasis), new(null, .2m, source.TaxBasis)], source.PersonalDeductionBasis, source.DependentDeductionBasis, source.TaxableIncomeBasis, source.TaxBasis, source.InsuranceRules, source.RoundingPolicy, annual, charitableContributionRules: rule);
        var provider = new TaxRuleProvider([new(custom, new(2026, 1, 1), new(2026, 12, 31))]);
        var months = Enumerable.Range(1, 12).Select(m => new SalaryMonthInput(2026, m, 30_000_000m, 3_000_000m, 1_000_000m));
        var proof = new ContributionEvidence(ContributionEvidenceKind.NonCashCreditInstitutionPayment, "bank", new(2026, 1, 1), false, true);
        CharitableContribution Item(ContributionRecipientKind kind, ContributionEvidence p) => new("x", new(2026, 1, 1), 16_000_000m, "org", kind, true, true, true, [p]);
        CharitableContributionYearInput Input(CharitableContribution c) => new(2026, ContributionScope.VietnameseResidentDomesticVndCalendarYear, true, true, [c]);
        var dependents = new[] { new DependentPeriod("child", new(2026, 7, 1), new(2026, 12, 1)) };
        var result = AnnualSalaryCalculator.Calculate(2026, months, dependents, provider, Input(Item(ContributionRecipientKind.CharitableHumanitarianStudyPromotionFund, proof)));
        Assert.Equal(200_000_000m, result.TaxableIncome); Assert.Equal(34_000_000m, result.RawTax); Assert.Equal(2_000_000m, result.EstimatedOverpaid);
        Assert.Throws<ArgumentException>(() => AnnualSalaryCalculator.Calculate(2026, months, dependents, provider, Input(Item(ContributionRecipientKind.LawfullyEstablishedFundraisingOrganization, proof))));
        var receipt = new ContributionEvidence(ContributionEvidenceKind.LegalReceiptCopy, "receipt", new(2026, 1, 1), true, false);
        Assert.Throws<ArgumentException>(() => AnnualSalaryCalculator.Calculate(2026, months, dependents, provider, Input(Item(ContributionRecipientKind.CharitableHumanitarianStudyPromotionFund, receipt))));
    }
    [Fact]
    public void ContributionsRetainTheirHalfYearRuleSnapshots()
    {
        var seed = Seed();
        LegalBasis Reversion(LegalBasis b) => new(b.Status, b.Id, "2", point: b.Point, clause: b.Clause, article: b.Article, documentNumber: b.DocumentNumber, summary: b.Summary, sourceUrl: b.SourceUrl, period: b.Period);
        var second = new CharitableContributionRuleSet(seed.Id, "2", 2026, new(2026, 7, 1), new(2026, 12, 31), seed.Scope, seed.Timing, seed.Mode,
            seed.Recipients.Select(x => new ContributionRecipientRule(x.Kind, Reversion(x.Basis), Reversion(x.LawBasis))), seed.Proofs.Select(x => new ContributionProofRule(x.Kind, Reversion(x.Basis))),
            Reversion(seed.ConditionsBasis), Reversion(seed.SelfPaidBasis), Reversion(seed.DeductionBasis), Reversion(seed.NoCarryoverBasis), Reversion(seed.PeriodBasis), Reversion(seed.ApplicabilityBasis), Reversion(seed.TransitionBasis), Reversion(seed.OverpaymentContextBasis));
        var old = TaxRuleSet.CreateVietnam2026CharitableContributionProvider(InsuranceRegion.I);
        var first = old.GetForMonth(2026, 1); var last = WithRule(old.GetForMonth(2026, 7), second);
        var provider = new TaxRuleProvider([new(first, new(2026, 1, 1), new(2026, 6, 30)), new(last, new(2026, 7, 1), new(2026, 12, 31))]);
        var evidence = new ContributionEvidence(ContributionEvidenceKind.LegalReceiptCopy, "x", new(2026, 1, 1), true, false);
        CharitableContribution Item(string id, int month) => new(id, new(2026, month, 1), 1, "org", ContributionRecipientKind.CharitableHumanitarianStudyPromotionFund, true, true, true, [evidence]);
        var input = new CharitableContributionYearInput(2026, ContributionScope.VietnameseResidentDomesticVndCalendarYear, true, true, [Item("a", 1), Item("b", 7)]);
        var months = Enumerable.Range(1, 12).Select(m => new SalaryMonthInput(2026, m, 0, 0, 0));
        var lines = AnnualSalaryCalculator.Calculate(2026, months, [], provider, input).ContributionDetails!.Lines;
        Assert.Same(first, lines[0].RuleSet); Assert.Same(last, lines[1].RuleSet); Assert.Equal("1", lines[0].RecipientRule.Basis.Version); Assert.Equal("2", lines[1].RecipientRule.Basis.Version);
    }
    [Fact]
    public void RuleEnumsCollectionsAndBasisRejectInvalidEntriesIndependently()
    {
        var s = Seed();
        CharitableContributionRuleSet Make(ContributionScope scope, ContributionTiming timing, ContributionMode mode,
            IEnumerable<ContributionRecipientRule> recipients, IEnumerable<ContributionProofRule> proofs, LegalBasis basis) =>
            new(s.Id, s.Version, s.Year, s.PeriodFrom, s.PeriodTo, scope, timing, mode, recipients, proofs, basis,
                s.SelfPaidBasis, s.DeductionBasis, s.NoCarryoverBasis, s.PeriodBasis, s.ApplicabilityBasis, s.TransitionBasis, s.OverpaymentContextBasis);
        CharitableContributionRuleSet MakeDefault() => Make(s.Scope, s.Timing, s.Mode, s.Recipients, s.Proofs, Basis());
        Assert.Equal(3, MakeDefault().Recipients.Count);
        foreach (var value in new[] { 0, 99 })
        {
            Assert.Throws<ArgumentException>(() => Make((ContributionScope)value, s.Timing, s.Mode, s.Recipients, s.Proofs, Basis()));
            Assert.Throws<ArgumentException>(() => Make(s.Scope, (ContributionTiming)value, s.Mode, s.Recipients, s.Proofs, Basis()));
            Assert.Throws<ArgumentException>(() => Make(s.Scope, s.Timing, (ContributionMode)value, s.Recipients, s.Proofs, Basis()));
            Assert.Throws<ArgumentException>(() => Copy(s, recipients: [new((ContributionRecipientKind)value, Basis(), Basis())]));
            Assert.Throws<ArgumentException>(() => Copy(s, proofs: [new((ContributionEvidenceKind)value, Basis())]));
        }
        Assert.Throws<ArgumentException>(() => Copy(s, recipients: [null!]));
        Assert.Throws<ArgumentException>(() => Copy(s, proofs: [null!]));
        Assert.Throws<ArgumentException>(() => Copy(s, proofs: [s.Proofs[0], s.Proofs[0]]));
        Assert.Throws<ArgumentNullException>(() => Make(s.Scope, s.Timing, s.Mode, null!, s.Proofs, Basis()));
        Assert.Throws<ArgumentNullException>(() => Make(s.Scope, s.Timing, s.Mode, s.Recipients, null!, Basis()));
        Assert.Throws<ArgumentNullException>(() => Make(s.Scope, s.Timing, s.Mode, s.Recipients, s.Proofs, null!));
        Assert.Throws<ArgumentException>(() => Copy(s, conditions: Basis(version: "")));
        Assert.Throws<ArgumentException>(() => Copy(s, from: new(2025, 12, 31)));
        Assert.Throws<ArgumentException>(() => Copy(s, to: new(2027, 1, 1)));
        var proofs = s.Proofs.ToList(); var copied = Copy(s, proofs: proofs); proofs.Clear();
        Assert.Equal(2, copied.Proofs.Count);
        Assert.Throws<NotSupportedException>(() => ((IList<ContributionProofRule>)copied.Proofs).Clear());
        // The 2027 fixture has dates and basis period consistent with each other.
        LegalBasis Future() => Basis(period: "2027");
        Assert.Throws<ArgumentException>(() => new CharitableContributionRuleSet(s.Id, s.Version, 2027, new(2027, 1, 1), new(2027, 12, 31), s.Scope, s.Timing, s.Mode,
            [new(s.Recipients[0].Kind, Future(), Future())], [new(s.Proofs[0].Kind, Future())], Future(), Future(), Future(), Future(), Future(), Future(), Future(), Future()));
    }

    [Fact]
    public void RootYearAndPeriodMustMatchEvenForEmptyInput()
    {
        var source = TaxRuleSet.CreateVietnam2026CharitableContributionProvider(InsuranceRegion.I).GetForMonth(2026, 1);
        TaxRuleSet Root(string period, int year) => new(source.Id, source.Version, period, year, source.PersonalDeduction, source.DependentDeduction, source.Brackets,
            source.PersonalDeductionBasis, source.DependentDeductionBasis, source.TaxableIncomeBasis, source.TaxBasis, source.InsuranceRules, source.RoundingPolicy,
            source.AnnualSalaryRules, source.CashBonusRules, source.SalaryExtraRules, source.CasualWithholdingRules, source.MultipleSalaryRules, source.CharitableContributionRules);
        var months = Enumerable.Range(1, 12).Select(m => new SalaryMonthInput(2026, m, 0, 0, 0)).ToArray();
        var input = new CharitableContributionYearInput(2026, ContributionScope.VietnameseResidentDomesticVndCalendarYear, true, true, []);
        void Calculate(TaxRuleSet root) => AnnualSalaryCalculator.Calculate(2026, months, [], new TaxRuleProvider([new(root, new(2026, 1, 1), new(2026, 12, 31))]), input);
        Calculate(source);
        Assert.Throws<ArgumentException>(() => Calculate(Root(source.Period, 2025)));
        Assert.Throws<ArgumentException>(() => Calculate(Root("2026 annual", source.FromYear)));
    }
}
