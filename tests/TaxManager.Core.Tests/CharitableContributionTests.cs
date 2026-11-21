using TaxManager.Core;
using Xunit;

namespace TaxManager.Core.Tests;

public class CharitableContributionTests
{
    const ContributionScope Scope = ContributionScope.VietnameseResidentDomesticVndCalendarYear;
    const ContributionRecipientKind Fund = ContributionRecipientKind.CharitableHumanitarianStudyPromotionFund;
    static ContributionEvidence Receipt() => new(ContributionEvidenceKind.LegalReceiptCopy, "receipt", new(2025, 12, 31), true, false);
    static ContributionEvidence Bank() => new(ContributionEvidenceKind.NonCashCreditInstitutionPayment, "bank", new(2027, 1, 1), false, true);
    static CharitableContribution Item(string id = "x", decimal amount = 10_000_000m, ContributionRecipientKind kind = Fund, bool org = true, bool purpose = true, bool nonprofit = true, ContributionEvidence[]? evidence = null, int month = 1) => new(id, new(2026, month, 1), amount, "org", kind, org, purpose, nonprofit, evidence ?? [Receipt()]);
    static CharitableContributionYearInput Input(IEnumerable<CharitableContribution> items, bool self = true, bool unique = true) => new(2026, Scope, self, unique, items);
    static SalaryMonthInput[] Months(decimal withheld = 900_000m) => Enumerable.Range(1, 12).Select(m => new SalaryMonthInput(2026, m, 30_000_000m, withheld, 1_000_000m)).ToArray();
    static DependentPeriod[] Dependents() => [new("child", new(2026, 7, 1), new(2026, 12, 1))];
    static AnnualSalaryResult Calc(CharitableContributionYearInput input) => AnnualSalaryCalculator.Calculate(2026, Months(), Dependents(), TaxRuleSet.CreateVietnam2026CharitableContributionProvider(InsuranceRegion.I), input);

    [Theory]
    [InlineData(0, 124800000, 6480000, 0, 0)]
    [InlineData(10000000, 114800000, 5740000, 10000000, 0)]
    [InlineData(124800000, 0, 0, 124800000, 0)]
    [InlineData(130000000, -5200000, 0, 124800000, 5200000)]
    public void AnnualCalculation(decimal amount, decimal before, decimal tax, decimal reduction, decimal unused)
    {
        var r = Calc(Input([Item(amount: amount)]));
        Assert.Equal(before, r.TaxableIncomeBeforeClamp); Assert.Equal(Math.Max(0, before), r.TaxableIncome); Assert.Equal(tax, r.RawTax);
        Assert.Equal(10_800_000m, r.TotalWithheldTax); Assert.Equal(tax - 10_800_000m, r.Delta); Assert.Equal(reduction, r.IncomeReduction);
        Assert.Equal(unused, r.ContributionDetails!.UnusedAgainstIncome); Assert.Equal(235_200_000m + amount, r.TotalDeductions);
    }

    [Theory]
    [InlineData(ContributionRecipientKind.CareInstitutionForSpeciallyDisadvantagedChildrenDisabledOrUnsupportedElderly)]
    [InlineData(ContributionRecipientKind.CharitableHumanitarianStudyPromotionFund)]
    [InlineData(ContributionRecipientKind.LawfullyEstablishedFundraisingOrganization)]
    public void RecipientsAndIndependentProofs(ContributionRecipientKind kind)
    {
        var r = Calc(Input([Item("a", kind: kind, evidence: [Receipt()]), Item("b", kind: kind, evidence: [Bank()], month: 7), Item("c", kind: kind, evidence: [Receipt(), Bank()])]));
        Assert.Equal(30_000_000m, r.EligibleCharitableContribution); Assert.All(r.ContributionDetails!.Lines, x => Assert.Empty(x.Reasons));
        Assert.NotSame(r.ContributionDetails.Lines[0].RuleSet, r.ContributionDetails.Lines[1].RuleSet);
        Assert.Equal(VerificationStatus.Unverified, r.ContributionDetails.Lines[0].RecipientRule.Basis.Status);
        Assert.Equal(VerificationStatus.Missing, r.ContributionDetails.Lines[1].EvidenceBasis.Status);
    }

    [Fact]
    public void MissingConditionsHaveExplicitReasons()
    {
        var lines = Calc(Input([Item("a", org: false), Item("b", purpose: false), Item("c", nonprofit: false), Item("d", evidence: []), Item("e", evidence: [new(ContributionEvidenceKind.LegalReceiptCopy, "x", new(2026, 1, 1), false, true)])], false, false)).ContributionDetails!.Lines;
        Assert.All(lines, x => { Assert.Equal(0m, x.EligibleAmount); Assert.Contains(ContributionExclusion.SelfPaidNotConfirmed, x.Reasons); Assert.Contains(ContributionExclusion.NotPreviouslyDeductedNotConfirmed, x.Reasons); });
        Assert.Contains(ContributionExclusion.OrganizationNotConfirmed, lines[0].Reasons); Assert.Contains(ContributionExclusion.PurposeNotConfirmed, lines[1].Reasons);
        Assert.Contains(ContributionExclusion.NonProfitNotConfirmed, lines[2].Reasons); Assert.Contains(ContributionExclusion.NoValidEvidence, lines[3].Reasons); Assert.Contains(ContributionExclusion.NoValidEvidence, lines[4].Reasons);
    }
    [Fact]
    public void InputValidationAndCopies()
    {
        var list = new List<CharitableContribution> { Item(" a "), Item("A") }; var input = Input(list); list.Clear(); Assert.Equal(2, input.Contributions.Count);
        Assert.Throws<NotSupportedException>(() => ((IList<CharitableContribution>)input.Contributions).Clear());
        Assert.Throws<ArgumentException>(() => Input([Item("a"), Item(" a ")]));
        Assert.Throws<ArgumentException>(() => Input([new("x", new(2025, 1, 1), 1, "o", Fund, true, true, true, [Receipt()])]));
        Assert.Throws<ArgumentOutOfRangeException>(() => Item(amount: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Item(kind: (ContributionRecipientKind)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ContributionEvidence(0, "a", new(2026, 1, 1), true, true));
        Assert.Throws<ArgumentException>(() => new ContributionEvidence(ContributionEvidenceKind.LegalReceiptCopy, " ", new(2026, 1, 1), true, false));
        Assert.Throws<ArgumentNullException>(() => Input(null!));
        Assert.Throws<ArgumentException>(() => Input([null!]));
        Assert.Throws<InvalidOperationException>(() => AnnualSalaryCalculator.Calculate(2026, Months(), Dependents(), TaxRuleSet.CreateVietnam2026AnnualSalaryProvider(InsuranceRegion.I), Input([])));
    }
    [Theory]
    [InlineData(0, 124800000, 6480000, 4320000)]
    [InlineData(10000000, 114800000, 5740000, 5060000)]
    [InlineData(124800000, 0, 0, 10800000)]
    [InlineData(130000000, -5200000, 0, 10800000)]
    public void MultiplePayersUseOneDeduction(decimal amount, decimal before, decimal tax, decimal overpaid)
    {
        var months = Enumerable.Range(1, 12).Select(m => new MultipleSalaryMonth(2026, m, 1_000_000m, [new("A", [new($"a{m}", new(2026, m, 1), 20_000_000m, 600_000m, SalaryReceiptSubject.OrdinaryEmploymentFullyTaxableGross)]), new("B", [new($"b{m}", new(2026, m, 1), 10_000_000m, 300_000m, SalaryReceiptSubject.OrdinaryEmploymentFullyTaxableGross)])]));
        var input = new MultipleSalaryYearInput(2026, MultipleSalaryScope.VietnameseResidentCalendarYearDomesticVndOnlyOrdinarySalary, true, true, months, Dependents());
        var r = MultipleSalaryCalculator.Calculate(input, TaxRuleSet.CreateVietnam2026MultipleSalaryCharitableContributionProvider(InsuranceRegion.I), Input([Item(amount: amount)]));
        Assert.Equal(before, r.Annual.TaxableIncomeBeforeClamp); Assert.Equal(Math.Max(0, before), r.Annual.TaxableIncome); Assert.Equal(tax, r.Annual.RawTax); Assert.Equal(overpaid, r.Annual.EstimatedOverpaid);
        Assert.Same(r.Annual.Months[0], r.Months[0].AnnualMonth);
    }
    [Fact]
    public void FractionAndOverflow()
    {
        var months = Enumerable.Range(1, 12).Select(m => new SalaryMonthInput(2026, m, m == 1 ? 186_000_001.5m : 0, 0, 0)).ToArray();
        var r = AnnualSalaryCalculator.Calculate(2026, months, [], TaxRuleSet.CreateVietnam2026CharitableContributionProvider(InsuranceRegion.I), Input([Item(amount: 1)]));
        Assert.Equal(.5m, r.TaxableIncome); Assert.Equal(.025m, r.RawTax);
        Assert.Throws<OverflowException>(() => Calc(Input([Item("a", decimal.MaxValue), Item("b", 1)])));
        Assert.Throws<OverflowException>(() => Calc(Input([Item(amount: decimal.MaxValue)])));
    }
    [Fact]
    public void EachConfirmationHasIndependentValidControl()
    {
        Assert.Equal(10_000_000m, Calc(Input([Item()])).EligibleCharitableContribution);
        Assert.Contains(ContributionExclusion.SelfPaidNotConfirmed, Calc(Input([Item()], self: false)).ContributionDetails!.Lines[0].Reasons);
        Assert.Contains(ContributionExclusion.NotPreviouslyDeductedNotConfirmed, Calc(Input([Item()], unique: false)).ContributionDetails!.Lines[0].Reasons);
        Assert.Single(Calc(Input([Item(org: false)])).ContributionDetails!.Lines[0].Reasons);
        Assert.Single(Calc(Input([Item(purpose: false)])).ContributionDetails!.Lines[0].Reasons);
        Assert.Single(Calc(Input([Item(nonprofit: false)])).ContributionDetails!.Lines[0].Reasons);
        Assert.Single(Calc(Input([Item(evidence: [])])).ContributionDetails!.Lines[0].Reasons);
        var badBank = new ContributionEvidence(ContributionEvidenceKind.NonCashCreditInstitutionPayment, "bank", new(2026, 1, 1), true, false);
        Assert.Contains(ContributionExclusion.NoValidEvidence, Calc(Input([Item(evidence: [badBank])])).ContributionDetails!.Lines[0].Reasons);
    }
    [Fact]
    public void GuardsDatesCollectionsAndNoIncome()
    {
        var proof = new List<ContributionEvidence> { Receipt() };
        var c = new CharitableContribution(" x ", new(2026, 12, 31), 1, " y ", Fund, true, true, true, proof, "2027");
        proof.Clear(); Assert.Single(c.Evidence); Assert.Equal("x", c.Id); Assert.Equal("y", c.RecipientId);
        Assert.Throws<NotSupportedException>(() => ((IList<ContributionEvidence>)c.Evidence).Clear());
        Assert.Throws<ArgumentOutOfRangeException>(() => new CharitableContribution("x", default, 0, "y", Fund, true, true, true, []));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ContributionEvidence(ContributionEvidenceKind.LegalReceiptCopy, "x", default, true, false));
        Assert.Throws<ArgumentNullException>(() => new CharitableContribution("x", new(2026, 1, 1), 0, "y", Fund, true, true, true, null!));
        Assert.Throws<ArgumentException>(() => new CharitableContribution("x", new(2026, 1, 1), 0, "y", Fund, true, true, true, [null!]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CharitableContributionYearInput(2027, Scope, true, true, []));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CharitableContributionYearInput(2026, 0, true, true, []));
        var months = Enumerable.Range(1, 12).Select(m => new SalaryMonthInput(2026, m, 0, 0, 0)).ToArray();
        var r = AnnualSalaryCalculator.Calculate(2026, months, [], TaxRuleSet.CreateVietnam2026CharitableContributionProvider(InsuranceRegion.I), Input([c]));
        Assert.Equal(-186_000_001m, r.TaxableIncomeBeforeClamp); Assert.Equal(0m, r.IncomeReduction); Assert.Equal(1m, r.ContributionDetails!.UnusedAgainstIncome);
        Assert.Equal(0m, r.RawTax); Assert.Equal(0m, r.EstimatedOverpaid);
    }
    [Fact]
    public void EmptyContributionsAndActualWithholdingPreserveMonthlyFigures()
    {
        var provider = TaxRuleSet.CreateVietnam2026CharitableContributionProvider(InsuranceRegion.I);
        var months = Months(0); var legacy = AnnualSalaryCalculator.Calculate(2026, months, Dependents(), provider);
        var empty = AnnualSalaryCalculator.Calculate(2026, months, Dependents(), provider, Input([]));
        Assert.Null(legacy.ContributionDetails); Assert.Equal(6_480_000m, empty.RawTax); Assert.Equal(6_480_000m, empty.AdditionalPayable);
        Assert.Equal(0m, empty.EstimatedOverpaid); Assert.Equal(0m, empty.TotalWithheldTax);
        var contributed = AnnualSalaryCalculator.Calculate(2026, months, Dependents(), provider, Input([Item(amount: 130_000_000m)]));
        Assert.Equal(0m, contributed.RawTax); Assert.Equal(0m, contributed.EstimatedOverpaid); Assert.Equal(0m, contributed.AdditionalPayable);
        Assert.Equal(legacy.Months[0].Salary.Pit.TotalTax.Value, contributed.Months[0].Salary.Pit.TotalTax.Value);
    }
    [Fact]
    public void DecimalBoundariesRemainExactAndOverflowIsReported()
    {
        var months = Enumerable.Range(1, 12).Select(m => new SalaryMonthInput(2026, m, 0, m == 1 ? decimal.MaxValue : 0, 0));
        var provider = TaxRuleSet.CreateVietnam2026CharitableContributionProvider(InsuranceRegion.I);
        var result = AnnualSalaryCalculator.Calculate(2026, months, [], provider, Input([Item(amount: decimal.MaxValue - 186_000_000m)]));
        Assert.Equal(decimal.MaxValue, result.TotalDeductions); Assert.Equal(decimal.MinValue, result.TaxableIncomeBeforeClamp);
        Assert.Equal(decimal.MinValue, result.Delta); Assert.Equal(decimal.MaxValue, result.EstimatedOverpaid);
        Assert.Throws<OverflowException>(() => AnnualSalaryCalculator.Calculate(2026, months, [], provider, Input([Item(amount: decimal.MaxValue - 186_000_000m + 1m)])));
    }
    [Fact]
    public void DeclaredOverflowIsNotHiddenByIneligibleLines()
    {
        var control = Calc(Input([Item("a", decimal.MaxValue - 1m, org: false), Item("b", 1m, org: false)]));
        Assert.Equal(decimal.MaxValue, control.ContributionDetails!.DeclaredTotal);
        Assert.Equal(0m, control.ContributionDetails.EligibleTotal);
        Assert.Throws<OverflowException>(() => Calc(Input([Item("a", decimal.MaxValue, org: false), Item("b", 1m, org: false)])));
    }
}
