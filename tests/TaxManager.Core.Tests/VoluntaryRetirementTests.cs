using TaxManager.Core;
using Xunit;

namespace TaxManager.Core.Tests;

public class VoluntaryRetirementTests
{
    const RetirementScope Scope = RetirementScope.VietnameseResidentDomesticVndCalendarYear;
    const RetirementKind Voluntary = RetirementKind.VoluntaryRetirementInsurance;
    const RetirementKind Life = RetirementKind.LifeInsurance;
    const RetirementKind Supplementary = RetirementKind.SupplementaryRetirementUnderSocialInsuranceLaw;
    static RetirementEvidence Receipt(decimal amount, bool confirmed = true, string issuer = "insurer") =>
        new(RetirementEvidenceKind.ReceiptCopy, "receipt", new(2026, 1, 2), issuer, amount, confirmed);
    static RetirementEvidence PayerProof(decimal amount, string payer = "company") =>
        new(RetirementEvidenceKind.IncomePayerConfirmation, "payer proof", new(2026, 1, 2), payer, amount, true);
    static RetirementContribution Item(string id, int month, decimal amount, RetirementKind kind = Voluntary,
        RetirementPaidBy paidBy = RetirementPaidBy.Self, string? payer = null, IEnumerable<RetirementEvidence>? evidence = null,
        bool product = true, bool paid = true, bool attribution = true, bool noDouble = true) =>
        new(id, new(2026, month, 15), new(2026, month, 1), amount, kind, paidBy,
            payer ?? (paidBy == RetirementPaidBy.Self ? "person" : "company"), "insurer", "contract",
            product, paid, attribution, noDouble, evidence ?? [Receipt(amount)]);
    static RetirementYearInput Input(params RetirementContribution[] items) => new(2026, Scope, "person", true, true, true,
        Enumerable.Range(1, 12).Select(m => new RetirementMonthInput(2026, m, items.Where(x => x.ContributionMonth.Month == m))));
    static SalaryMonthInput[] Salary(decimal gross = 30_000_000m, decimal withheld = 900_000m, decimal insurance = 1_000_000m) =>
        Enumerable.Range(1, 12).Select(m => new SalaryMonthInput(2026, m, gross, withheld, insurance)).ToArray();
    static DependentPeriod[] Dependents() => [new("child", new(2026, 7, 1), new(2026, 12, 1))];
    static TaxRuleProvider Provider() => TaxRuleSet.CreateVietnam2026RetirementProvider(InsuranceRegion.I);
    static AnnualSalaryResult Calc(RetirementYearInput input) => AnnualSalaryCalculator.CalculateWithRetirement(2026, Salary(), Dependents(), Provider(), input);

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(2999999, 2999999, 0)]
    [InlineData(3000000, 3000000, 0)]
    [InlineData(3000001, 3000000, 1)]
    public void MonthlyBoundary(decimal amount, decimal deductible, decimal capped)
    {
        var r = Calc(Input(Item("a", 1, amount))).RetirementDetails!;
        Assert.Equal(amount, r.DeclaredTotal);
        Assert.Equal(amount, r.EligibleTotal);
        Assert.Equal(deductible, r.DeductibleTotal);
        Assert.Equal(capped, r.CappedAmount);
        Assert.Equal(0m, r.ExcludedTotal);
    }

    [Fact]
    public void ThreeKindsAndMultiplePayersShareOneMonthlyCap()
    {
        var r = Calc(Input(Item("a", 1, 2_000_000m, Supplementary),
            Item("b", 1, 2_000_000m, Voluntary, RetirementPaidBy.Employer, evidence: [Receipt(2_000_000m), PayerProof(2_000_000m)]),
            Item("c", 1, 2_000_000m, Life, RetirementPaidBy.Employer, "company2", [Receipt(2_000_000m), PayerProof(2_000_000m, "company2")]))).RetirementDetails!;
        Assert.Equal(6_000_000m, r.DeclaredTotal); Assert.Equal(6_000_000m, r.EligibleTotal);
        Assert.Equal(3_000_000m, r.DeductibleTotal); Assert.Equal(3_000_000m, r.CappedAmount);
        Assert.Equal(0m, r.Months[1].DeductibleTotal);
    }

    [Fact]
    public void AnnualTotalIsSumOfMonthlyCapsNotAnAnnualPool()
    {
        var items = Enumerable.Range(1, 12).Select(m => Item($"i{m}", m, 4_000_000m)).ToArray();
        var r = Calc(Input(items)); var retirement = r.RetirementDetails!;
        Assert.Equal(48_000_000m, retirement.DeclaredTotal); Assert.Equal(48_000_000m, retirement.EligibleTotal);
        Assert.Equal(36_000_000m, retirement.DeductibleTotal); Assert.Equal(12_000_000m, retirement.CappedAmount);
        Assert.Equal(360_000_000m, r.GrossIncome); Assert.Equal(12_000_000m, r.Insurance);
        Assert.Equal(186_000_000m, r.PersonalDeduction); Assert.Equal(37_200_000m, r.DependentDeduction);
        Assert.Equal(271_200_000m, r.TotalDeductions); Assert.Equal(88_800_000m, r.TaxableIncomeBeforeClamp);
        Assert.Equal(88_800_000m, r.TaxableIncome); Assert.Equal(4_440_000m, r.RawTax);
        Assert.Equal(10_800_000m, r.TotalWithheldTax); Assert.Equal(-6_360_000m, r.Delta);
        Assert.Equal(0m, r.AdditionalPayable); Assert.Equal(6_360_000m, r.EstimatedOverpaid);
    }

    [Fact]
    public void MissingProofAndEachConfirmationExcludeOnlyAffectedLines()
    {
        var lines = Calc(Input(Item("valid", 1, 100m), Item("product", 1, 100m, product: false),
            Item("paid", 1, 100m, paid: false), Item("attribution", 1, 100m, attribution: false),
            Item("double", 1, 100m, noDouble: false), Item("receipt", 1, 100m, evidence: []),
            Item("unconfirmed", 1, 100m, evidence: [Receipt(100m, false)]),
            Item("employer", 1, 100m, paidBy: RetirementPaidBy.Employer, evidence: [Receipt(100m)]))).RetirementDetails!.Months[0].Lines;
        Assert.Equal(100m, lines[0].EligibleAmount); Assert.Empty(lines[0].Reasons);
        Assert.All(lines.Skip(1), line => { Assert.Equal(0m, line.EligibleAmount); Assert.NotEmpty(line.Reasons); });
    }

    [Fact]
    public void FractionAndNoIncomeRemainExact()
    {
        var months = Enumerable.Range(1, 12).Select(m => new SalaryMonthInput(2026, m, m == 1 ? 186_000_001.5m : 0m, 0, 0));
        var r = AnnualSalaryCalculator.CalculateWithRetirement(2026, months, [], Provider(), Input(Item("a", 1, 1)));
        Assert.Equal(.5m, r.TaxableIncome); Assert.Equal(.025m, r.RawTax);
        var zero = AnnualSalaryCalculator.CalculateWithRetirement(2026, Salary(0, 0, 0), [], Provider(), Input(Item("a", 1, 1)));
        Assert.Equal(0m, zero.RawTax); Assert.Equal(0m, zero.EstimatedOverpaid); Assert.Equal(0m, zero.AdditionalPayable);
    }

    [Fact]
    public void DeclaredOverflowIsNotHiddenByExcludedItems()
    {
        var valid = Calc(Input(Item("a", 1, decimal.MaxValue - 1m, product: false), Item("b", 1, 1m, product: false)));
        Assert.Equal(decimal.MaxValue, valid.RetirementDetails!.DeclaredTotal);
        Assert.Throws<OverflowException>(() => Calc(Input(Item("a", 1, decimal.MaxValue, product: false), Item("b", 1, 1m, product: false))));
    }

    [Fact]
    public void CharityUsesIncomeAfterRetirementAndDoesNotAlterMonthlyResults()
    {
        var retirement = Input(Enumerable.Range(1, 12).Select(m => Item($"i{m}", m, 4_000_000m)).ToArray());
        CharitableContributionYearInput Charity(decimal amount) => new(2026, ContributionScope.VietnameseResidentDomesticVndCalendarYear, true, true,
            [new("charity", new(2026, 1, 1), amount, "fund", ContributionRecipientKind.CharitableHumanitarianStudyPromotionFund,
                true, true, true, [new(ContributionEvidenceKind.LegalReceiptCopy, "receipt", new(2026, 1, 1), true, false)])]);
        var provider = Provider(); var salary = Salary(); var pensionOnly = Calc(retirement);
        var both = AnnualSalaryCalculator.CalculateWithRetirement(2026, salary, Dependents(), provider, retirement, Charity(10_000_000m));
        Assert.Equal(281_200_000m, both.TotalDeductions); Assert.Equal(78_800_000m, both.TaxableIncomeBeforeClamp);
        Assert.Equal(3_940_000m, both.RawTax); Assert.Equal(-6_860_000m, both.Delta); Assert.Equal(6_860_000m, both.EstimatedOverpaid);
        Assert.Equal(10_000_000m, both.IncomeReduction); Assert.Equal(0m, both.ContributionDetails!.UnusedAgainstIncome);
        Assert.Equal(pensionOnly.Months[0].Salary.Pit.TotalTax.Value, both.Months[0].Salary.Pit.TotalTax.Value);
        var excess = AnnualSalaryCalculator.CalculateWithRetirement(2026, Salary(withheld: 0), Dependents(), provider, retirement, Charity(100_000_000m));
        Assert.Equal(-11_200_000m, excess.TaxableIncomeBeforeClamp); Assert.Equal(0m, excess.RawTax);
        Assert.Equal(88_800_000m, excess.IncomeReduction); Assert.Equal(11_200_000m, excess.ContributionDetails!.UnusedAgainstIncome);
        Assert.Equal(0m, excess.EstimatedOverpaid); Assert.Equal(0m, excess.AdditionalPayable);
    }

    [Fact]
    public void MultipleSalaryUsesOnePersonalPool()
    {
        var months = Enumerable.Range(1, 12).Select(m => new MultipleSalaryMonth(2026, m, 1_000_000m,
            [new("A", [new($"a{m}", new(2026, m, 1), 20_000_000m, 600_000m, SalaryReceiptSubject.OrdinaryEmploymentFullyTaxableGross)]),
             new("B", [new($"b{m}", new(2026, m, 1), 10_000_000m, 300_000m, SalaryReceiptSubject.OrdinaryEmploymentFullyTaxableGross)])]));
        var input = new MultipleSalaryYearInput(2026, MultipleSalaryScope.VietnameseResidentCalendarYearDomesticVndOnlyOrdinarySalary, true, true, months, Dependents());
        var retirement = Input(Enumerable.Range(1, 12).Select(m => Item($"p{m}", m, 4_000_000m)).ToArray());
        var result = MultipleSalaryCalculator.CalculateWithRetirement(input, TaxRuleSet.CreateVietnam2026MultipleSalaryRetirementProvider(InsuranceRegion.I), retirement);
        Assert.Equal(36_000_000m, result.Annual.RetirementDeduction); Assert.Equal(12_000_000m, result.Annual.Insurance);
        Assert.Equal(88_800_000m, result.Annual.TaxableIncome); Assert.Equal(4_440_000m, result.Annual.RawTax);
        Assert.Equal(6_360_000m, result.Annual.EstimatedOverpaid); Assert.Same(result.Annual.Months[0], result.Months[0].AnnualMonth);
    }

    [Fact]
    public void InputsCopyCollectionsAndRejectInvalidShape()
    {
        var proofs = new List<RetirementEvidence> { Receipt(1) };
        var entry = Item(" a ", 1, 1, evidence: proofs); proofs.Clear(); Assert.Single(entry.Evidence);
        Assert.Throws<NotSupportedException>(() => ((IList<RetirementEvidence>)entry.Evidence).Clear());
        var entries = new List<RetirementContribution> { entry }; var january = new RetirementMonthInput(2026, 1, entries);
        entries.Clear(); Assert.Single(january.Contributions);
        Assert.Throws<NotSupportedException>(() => ((IList<RetirementContribution>)january.Contributions).Clear());
        var months = Enumerable.Range(1, 12).Select(m => m == 1 ? january : new RetirementMonthInput(2026, m, [])).ToList();
        var year = new RetirementYearInput(2026, Scope, "person", true, true, true, months); months.Clear();
        Assert.Equal(12, year.Months.Count); Assert.Throws<NotSupportedException>(() => ((IList<RetirementMonthInput>)year.Months).Clear());
        Assert.Throws<ArgumentException>(() => Input(Item("a", 1, 1), Item(" a ", 2, 1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => Item("a", 1, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Item("a", 1, 1, (RetirementKind)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => Item("a", 1, 1, paidBy: (RetirementPaidBy)99));
        Assert.Throws<ArgumentException>(() => new RetirementYearInput(2026, Scope, "person", true, true, true, [january]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RetirementYearInput(2027, Scope, "person", true, true, true,
            Enumerable.Range(1, 12).Select(m => new RetirementMonthInput(2027, m, []))));
    }

    [Fact]
    public void GlobalConfirmationsAndProofMatchingAreIndependent()
    {
        var control = Enumerable.Range(1, 12).Select(m => new RetirementMonthInput(2026, m, m == 1 ? [Item("x", 1, 7)] : [])).ToArray();
        Assert.Equal(7m, Calc(new(2026, Scope, "person", true, true, false, control)).RetirementDeduction);
        Assert.Throws<ArgumentException>(() => Calc(new(2026, Scope, "person", false, true, true, control)));
        Assert.Throws<ArgumentException>(() => Calc(new(2026, Scope, "person", true, false, true, control)));
        var employer = Enumerable.Range(1, 12).Select(m => new RetirementMonthInput(2026, m, m == 1 ?
            [Item("x", 1, 7, paidBy: RetirementPaidBy.Employer, evidence: [Receipt(7), PayerProof(7)])] : [])).ToArray();
        Assert.Equal(7m, Calc(new(2026, Scope, "person", true, true, true, employer)).RetirementDeduction);
        Assert.Throws<ArgumentException>(() => Calc(new(2026, Scope, "person", true, true, false, employer)));
        var bad = Calc(Input(Item("issuer", 1, 7, evidence: [Receipt(7, issuer: "wrong")]),
            Item("amount", 1, 7, evidence: [Receipt(6)]),
            Item("payer", 1, 7, paidBy: RetirementPaidBy.Employer, evidence: [Receipt(7), PayerProof(7, "wrong")]))).RetirementDetails!.Months[0].Lines;
        Assert.Contains(RetirementExclusion.MissingReceipt, bad[0].Reasons);
        Assert.Contains(RetirementExclusion.MissingReceipt, bad[1].Reasons);
        Assert.Contains(RetirementExclusion.MissingIncomePayerConfirmation, bad[2].Reasons);
    }

    [Fact]
    public void DatesAndIdentifiersCannotAttributeOtherPeriodsOrPeople()
    {
        RetirementContribution Build(DateOnly paid, DateOnly month, string id = "x", string payer = "person") =>
            new(id, paid, month, 1, Voluntary, RetirementPaidBy.Self, payer, "insurer", "contract", true, true, true, true, [Receipt(1)]);
        _ = Build(new(2026, 1, 1), new(2026, 1, 1));
        Assert.Throws<ArgumentException>(() => Build(default, new(2026, 1, 1)));
        Assert.Throws<ArgumentException>(() => Build(new(2026, 1, 1), default));
        Assert.Throws<ArgumentException>(() => Build(new(2026, 1, 1), new(2026, 1, 2)));
        Assert.Throws<ArgumentException>(() => Build(new(2026, 2, 1), new(2026, 1, 1)));
        Assert.Throws<ArgumentException>(() => Build(new(2025, 1, 1), new(2026, 1, 1)));
        Assert.Throws<ArgumentException>(() => Build(new(2026, 1, 1), new(2026, 1, 1), id: " "));
        Assert.Throws<ArgumentException>(() => Input(Build(new(2026, 1, 1), new(2026, 1, 1), payer: "other")));
        Assert.Throws<ArgumentException>(() => new RetirementMonthInput(2026, 2, [Build(new(2026, 1, 1), new(2026, 1, 1))]));
        Assert.Throws<ArgumentNullException>(() => new RetirementMonthInput(2026, 1, null!));
        Assert.Throws<ArgumentException>(() => new RetirementMonthInput(2026, 1, [null!]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RetirementEvidence((RetirementEvidenceKind)99, "x", new(2026, 1, 1), "issuer", 1, true));
    }

    [Fact]
    public void ActualZeroMeansAdditionalTaxAndNoManufacturedRefund()
    {
        var input = Input(Item("first", 1, 3_000_000m));
        var r = AnnualSalaryCalculator.CalculateWithRetirement(2026, Salary(withheld: 0), Dependents(), Provider(), input);
        Assert.Equal(3_000_000m, r.RetirementDeduction); Assert.Equal(121_800_000m, r.TaxableIncome);
        Assert.Equal(6_180_000m, r.RawTax); Assert.Equal(6_180_000m, r.Delta);
        Assert.Equal(6_180_000m, r.AdditionalPayable); Assert.Equal(0m, r.EstimatedOverpaid);
        Assert.Equal(0m, r.TotalWithheldTax); Assert.Equal(12_000_000m, r.Insurance);
    }

    [Fact]
    public void ProofCombinationsAndUnusedIncomeAreExplicit()
    {
        var items = new[] { Item("self", 1, 10m, evidence: [Receipt(10), PayerProof(10, "other")]),
            Item("both", 1, 11m, paidBy: RetirementPaidBy.Employer, evidence: [Receipt(11), PayerProof(11)]),
            Item("noReceipt", 1, 12m, paidBy: RetirementPaidBy.Employer, evidence: [PayerProof(12)]),
            Item("wrongAmount", 1, 13m, paidBy: RetirementPaidBy.Employer, evidence: [Receipt(13), PayerProof(12)]),
            Item("notConfirmed", 1, 14m, paidBy: RetirementPaidBy.Employer, evidence: [Receipt(14), new(RetirementEvidenceKind.IncomePayerConfirmation, "p", new(2026, 1, 1), "company", 14, false)]) };
        var r = AnnualSalaryCalculator.CalculateWithRetirement(2026, Salary(0, 0, 0), [], Provider(), Input(items)).RetirementDetails!;
        Assert.Equal(60m, r.DeclaredTotal); Assert.Equal(21m, r.EligibleTotal); Assert.Equal(39m, r.ExcludedTotal);
        Assert.Equal(21m, r.DeductibleTotal); Assert.Equal(0m, r.IncomeReduction); Assert.Equal(21m, r.UnusedAgainstIncome);
        var lines = r.Months[0].Lines;
        Assert.Empty(lines[0].Reasons); Assert.Empty(lines[1].Reasons);
        Assert.Contains(RetirementExclusion.MissingReceipt, lines[2].Reasons);
        Assert.Contains(RetirementExclusion.MissingIncomePayerConfirmation, lines[3].Reasons);
        Assert.Contains(RetirementExclusion.MissingIncomePayerConfirmation, lines[4].Reasons);
        Assert.Equal(VerificationStatus.Missing, lines[0].EvidenceBasis.Status);
    }

    [Fact]
    public void EachFailedConfirmationHasItsOwnReason()
    {
        var r = Calc(Input(Item("valid", 1, 1), Item("product", 1, 1, product: false),
            Item("paid", 1, 1, paid: false), Item("attribution", 1, 1, attribution: false),
            Item("duplicate", 1, 1, noDouble: false))).RetirementDetails!.Months[0].Lines;
        Assert.Empty(r[0].Reasons); Assert.Equal(1m, r[0].EligibleAmount);
        Assert.Equal(new[] { RetirementExclusion.QualifiedProductNotConfirmed }, r[1].Reasons);
        Assert.Equal(new[] { RetirementExclusion.ActuallyPaidNotConfirmed }, r[2].Reasons);
        Assert.Equal(new[] { RetirementExclusion.MonthlyAttributionNotConfirmed }, r[3].Reasons);
        Assert.Equal(new[] { RetirementExclusion.AlreadyDeductedNotRuledOut }, r[4].Reasons);
    }

    [Fact]
    public void ResultCollectionsAreReadOnlyAndDeclaredOverflowCrossesMonths()
    {
        var result = Calc(Input(Item("a", 1, decimal.MaxValue - 1m, product: false), Item("b", 2, 1m, product: false))).RetirementDetails!;
        Assert.Equal(decimal.MaxValue, result.DeclaredTotal); Assert.Equal(0m, result.EligibleTotal);
        Assert.Throws<NotSupportedException>(() => ((IList<RetirementMonthResult>)result.Months).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<RetirementLineResult>)result.Months[0].Lines).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<RetirementExclusion>)result.Months[0].Lines[0].Reasons).Clear());
        Assert.Throws<OverflowException>(() => Calc(Input(Item("a", 1, decimal.MaxValue, product: false), Item("b", 2, 1m, product: false))));
    }

    [Fact]
    public void CombinedDeductionOverflowIsNotHidden()
    {
        var retirement = Input(Item("p", 1, 3_000_000m));
        CharitableContributionYearInput Charity(decimal amount) => new(2026, ContributionScope.VietnameseResidentDomesticVndCalendarYear, true, true,
            [new("gift", new(2026, 1, 1), amount, "fund", ContributionRecipientKind.CharitableHumanitarianStudyPromotionFund,
                true, true, true, [new(ContributionEvidenceKind.LegalReceiptCopy, "r", new(2026, 1, 1), true, false)])]);
        var salary = Salary(0, 0, 0); var provider = Provider();
        var control = AnnualSalaryCalculator.CalculateWithRetirement(2026, salary, [], provider, retirement, Charity(decimal.MaxValue - 189_000_000m));
        Assert.Equal(decimal.MaxValue, control.TotalDeductions); Assert.Equal(decimal.MinValue, control.TaxableIncomeBeforeClamp);
        Assert.Throws<OverflowException>(() => AnnualSalaryCalculator.CalculateWithRetirement(2026, salary, [], provider, retirement,
            Charity(decimal.MaxValue - 189_000_000m + 1m)));
    }
}
