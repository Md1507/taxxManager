using TaxManager.Core;
using Xunit;

namespace TaxManager.Core.Tests;

public class MultipleSalaryPayersTests
{
    private const MultipleSalaryScope Scope = MultipleSalaryScope.VietnameseResidentCalendarYearDomesticVndOnlyOrdinarySalary;
    private const SalaryReceiptSubject Subject = SalaryReceiptSubject.OrdinaryEmploymentFullyTaxableGross;
    private static SalaryReceipt Receipt(string id, int month, decimal gross, decimal? actual) => new(id, new(2026, month, 15), gross, actual, Subject);
    private static MultipleSalaryYearInput Input(IEnumerable<MultipleSalaryMonth> months, params DependentPeriod[] dependents) => new(2026, Scope, true, true, months, dependents);
    private static MultipleSalaryResult Calc(MultipleSalaryYearInput input, InsuranceRegion region = InsuranceRegion.I) => MultipleSalaryCalculator.Calculate(input, TaxRuleSet.CreateVietnam2026MultipleSalaryProvider(region));
    private static MultipleSalaryMonth[] Simultaneous(decimal actualA, decimal actualB) => Enumerable.Range(1, 12).Select(m => new MultipleSalaryMonth(2026, m, 1_000_000m,
        [new SalaryPayerMonth("A", [Receipt($"a{m}", m, 20_000_000m, actualA)]), new SalaryPayerMonth("B", [Receipt($"b{m}", m, 10_000_000m, actualB)])])).ToArray();

    [Theory]
    [InlineData(600000, 300000, 0, 4320000)]
    [InlineData(300000, 200000, 480000, 0)]
    public void SimultaneousPayersDeductOnce(int a, int b, int additional, int overpaid)
    {
        var result = Calc(Input(Simultaneous(a, b), new DependentPeriod("child", new(2026, 7, 1), new(2026, 12, 1))));
        Assert.Equal(360_000_000m, result.Annual.GrossIncome); Assert.Equal(12_000_000m, result.Annual.Insurance);
        Assert.Equal(186_000_000m, result.Annual.PersonalDeduction); Assert.Equal(37_200_000m, result.Annual.DependentDeduction);
        Assert.Equal(124_800_000m, result.Annual.TaxableIncome); Assert.Equal(6_480_000m, result.Annual.RawTax);
        Assert.Equal(additional, result.Annual.AdditionalPayable); Assert.Equal(overpaid, result.Annual.EstimatedOverpaid);
        Assert.Equal(20_000_000m, result.Months[0].Payers[0].Gross); Assert.Equal(a, result.Months[0].Payers[0].ActualWithheldTax);
        Assert.Equal(result.Annual.RawTax, Calc(result.Input, InsuranceRegion.IV).Annual.RawTax);
    }

    [Fact]
    public void SwitchingAndZeroMonthsAreAnnualNotSumOfMonthlyTax()
    {
        var switching = Enumerable.Range(1, 12).Select(m => new MultipleSalaryMonth(2026, m, 0, [new SalaryPayerMonth(m <= 6 ? "A" : "B", [Receipt($"p{m}", m, 30_000_000m, 0)])]));
        var result = Calc(Input(switching.Reverse()));
        Assert.Equal(360_000_000m, result.Annual.GrossIncome); Assert.Equal(174_000_000m, result.Annual.TaxableIncome);
        Assert.Equal(11_400_000m, result.Annual.AdditionalPayable); Assert.Equal(Enumerable.Range(1, 12), result.Months.Select(x => x.Month));
        var zeroFirst = Enumerable.Range(1, 12).Select(m => new MultipleSalaryMonth(2026, m, 0, m <= 6 ? [] : [new SalaryPayerMonth("B", [Receipt($"p{m}", m, 40_000_000m, 1_950_000m)])]));
        var zeroResult = Calc(Input(zeroFirst));
        Assert.Equal(2_700_000m, zeroResult.Annual.RawTax); Assert.Equal(9_000_000m, zeroResult.Annual.EstimatedOverpaid);
        Assert.Equal(-186_000_000m, Calc(Input(Enumerable.Range(1, 12).Select(m => new MultipleSalaryMonth(2026, m, 0, [])))).Annual.TaxableIncomeBeforeClamp);
    }

    [Fact]
    public void UnknownAndDuplicatesAreRejectedButActualCanExceedGross()
    {
        var months = Simultaneous(0, 0);
        months[0] = new(2026, 1, 0, [new("A", [Receipt("unknown", 1, 0, null)])]);
        Assert.Contains("unknown", Assert.Throws<ArgumentException>(() => Calc(Input(months))).Message);
        months[0] = new(2026, 1, 0, [new("A", [Receipt("big", 1, 0, 99_000_000m)])]);
        Assert.Equal(99_000_000m, Calc(Input(months)).Annual.TotalWithheldTax);
        Assert.Throws<ArgumentException>(() => new MultipleSalaryMonth(2026, 1, 0, [new(" A ", [Receipt("1", 1, 0, 0)]), new("A", [Receipt("2", 1, 0, 0)])]));
        months[0] = new(2026, 1, 0, [new("A", [Receipt("a2", 1, 0, 0)])]);
        Assert.Throws<ArgumentException>(() => Input(months));
    }

    [Fact]
    public void CollectionsAreCopiedAndFractionIsPreserved()
    {
        var payments = new List<SalaryReceipt> { Receipt("x", 1, 186_000_000.5m, 0) };
        var payers = new List<SalaryPayerMonth> { new("A", payments) };
        var months = Enumerable.Range(1, 12).Select(m => new MultipleSalaryMonth(2026, m, 0, m == 1 ? payers : [])).ToList();
        var input = Input(months); payments.Clear(); payers.Clear(); months.Clear();
        var result = Calc(input);
        Assert.Equal(.025m, result.Annual.RawTax); Assert.Single(result.Months[0].Payers[0].Payments);
        Assert.Throws<NotSupportedException>(() => ((IList<MultipleSalaryMonth>)input.Months).Clear());
    }

    [Fact]
    public void OverflowAndConfirmationAreNotHidden()
    {
        var months = Simultaneous(0, 0);
        months[0] = new(2026, 1, 0, [new("A", [Receipt("one", 1, decimal.MaxValue, 0), Receipt("two", 1, 1, 0)])]);
        Assert.Throws<OverflowException>(() => Calc(Input(months)));
        Assert.Throws<ArgumentException>(() => Calc(new(2026, Scope, false, true, Simultaneous(0, 0), [])));
        Assert.Throws<ArgumentException>(() => Calc(new(2026, Scope, true, false, Simultaneous(0, 0), [])));
    }

    [Fact]
    public void InputGuardsHaveValidControls()
    {
        var valid = Simultaneous(0, 0); _ = Calc(Input(valid));
        Assert.Throws<ArgumentException>(() => Input(valid.Take(11)));
        Assert.Throws<ArgumentException>(() => Input(valid.Concat([valid[0]])));
        var duplicateMonth = valid.ToArray(); duplicateMonth[11] = duplicateMonth[0];
        Assert.Throws<ArgumentException>(() => Input(duplicateMonth));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MultipleSalaryYearInput(2027, Scope, true, true, valid, []));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MultipleSalaryYearInput(2026, 0, true, true, valid, []));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SalaryReceipt("x", new(2026, 1, 1), 0, 0, (SalaryReceiptSubject)99));
        Assert.Throws<ArgumentException>(() => new SalaryReceipt(" ", new(2026, 1, 1), 0, 0, Subject));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SalaryReceipt("x", default, 0, 0, Subject));
        Assert.Throws<ArgumentOutOfRangeException>(() => Receipt("x", 1, -1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Receipt("x", 1, 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MultipleSalaryMonth(2026, 1, -1, []));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MultipleSalaryMonth(2026, 13, 0, []));
        Assert.Throws<ArgumentException>(() => new SalaryPayerMonth("A", []));
        Assert.Throws<ArgumentNullException>(() => new SalaryPayerMonth("A", null!));
        Assert.Throws<ArgumentException>(() => new SalaryPayerMonth("A", [null!]));
        Assert.Throws<ArgumentNullException>(() => new MultipleSalaryMonth(2026, 1, 0, null!));
        Assert.Throws<ArgumentException>(() => new MultipleSalaryMonth(2026, 1, 0, [null!]));
        Assert.Throws<ArgumentNullException>(() => new MultipleSalaryYearInput(2026, Scope, true, true, null!, []));
        Assert.Throws<ArgumentNullException>(() => new MultipleSalaryYearInput(2026, Scope, true, true, valid, null!));
        Assert.Throws<ArgumentNullException>(() => MultipleSalaryCalculator.Calculate(null!, TaxRuleSet.CreateVietnam2026MultipleSalaryProvider(InsuranceRegion.I)));
        Assert.Throws<ArgumentNullException>(() => MultipleSalaryCalculator.Calculate(Input(valid), null!));
        var wrongDate = valid.ToArray(); wrongDate[0] = new(2026, 1, 0, [new("A", [Receipt("x", 2, 0, 0)])]);
        Assert.Throws<ArgumentException>(() => Input(wrongDate));
    }

    [Fact]
    public void MultipleReceiptsAndCaseDistinctIdentifiersRemainSeparate()
    {
        var months = Simultaneous(0, 0);
        months[0] = new(2026, 1, 0, [new("A", [Receipt("x", 1, 2, 100), Receipt("X", 1, 3, 200)]), new("a", [Receipt("y", 1, 4, 300)])]);
        var result = Calc(Input(months));
        Assert.Equal(9m, result.Months[0].Gross); Assert.Equal(600m, result.Months[0].ActualWithheldTax);
        Assert.Equal(5m, result.Months[0].Payers[0].Gross); Assert.Equal(2, result.Months[0].Payers[0].Payments.Count);
    }

    [Fact]
    public void OnePayerAndDependentPeriodsFollowAnnualRules()
    {
        var months = Enumerable.Range(1, 12).Select(m => new MultipleSalaryMonth(2026, m, 0, [new SalaryPayerMonth("A", [Receipt($"p{m}", m, 30_000_000m, 0)])])).ToArray();
        Assert.Equal(11_400_000m, Calc(Input(months)).Annual.RawTax);
        var periods = new[] { new DependentPeriod("d", new(2025, 12, 1), new(2026, 2, 1)), new DependentPeriod("d", new(2026, 4, 1), new(2027, 1, 1)) };
        var result = Calc(Input(months, periods));
        Assert.Equal(11, result.Annual.DependentMonths); Assert.Empty(result.Months[2].AnnualMonth.Dependents);
        Assert.Throws<ArgumentException>(() => Calc(Input(months, new DependentPeriod("d", new(2025, 1, 1), new(2025, 3, 1)), new DependentPeriod("d", new(2025, 3, 1), new(2025, 4, 1)))));
    }

    [Fact]
    public void DifferentOverflowPathsAreExposed()
    {
        var months = Enumerable.Range(1, 12).Select(m => new MultipleSalaryMonth(2026, m, 0, [])).ToArray();
        months[0] = new(2026, 1, 0, [new("A", [Receipt("a", 1, 0, decimal.MaxValue)]), new("B", [Receipt("b", 1, 0, 1)])]);
        Assert.Throws<OverflowException>(() => Calc(Input(months)));
        months[0] = new(2026, 1, decimal.MaxValue, []); months[1] = new(2026, 2, 1, []);
        Assert.Throws<OverflowException>(() => Calc(Input(months)));
        months[0] = new(2026, 1, 0, [new("A", [Receipt("a", 1, decimal.MaxValue, 0)])]);
        months[1] = new(2026, 2, 0, [new("B", [Receipt("b", 2, 1, 0)])]);
        Assert.Throws<OverflowException>(() => Calc(Input(months)));
    }

    [Fact]
    public void AnnualInsuranceSumOverflowIsDistinctFromMonthlySubtraction()
    {
        const decimal amount = 40_000_000_000_000_000_000_000_000_000m;
        var months = Enumerable.Range(1, 12).Select(m => new MultipleSalaryMonth(2026, m, 0, [])).ToArray();
        months[0] = new(2026, 1, amount, []);
        months[1] = new(2026, 2, 1, []);
        Assert.Equal(amount + 1, Calc(Input(months)).Annual.Insurance);
        months[1] = new(2026, 2, amount, []);
        Assert.Throws<OverflowException>(() => Calc(Input(months)));
    }

    [Fact]
    public void CopiesDependentsAndRegistrationAndRejectsMutation()
    {
        var periods = new List<DependentPeriod> { new("child", new(2026, 1, 1), new(2026, 1, 1)) };
        var input = new MultipleSalaryYearInput(2026, Scope, true, true, Simultaneous(0, 0), periods);
        var seed = TaxRuleSet.CreateVietnam2026MultipleSalaryProvider(InsuranceRegion.I);
        var registrations = new List<TaxRuleRegistration> { new(seed.GetForMonth(2026, 1), new(2026, 1, 1), new(2026, 6, 30)), new(seed.GetForMonth(2026, 7), new(2026, 7, 1), new(2026, 12, 31)) };
        var provider = new TaxRuleProvider(registrations); periods.Clear(); registrations.Clear();
        var result = MultipleSalaryCalculator.Calculate(input, provider);
        Assert.Equal(1, result.Annual.DependentMonths);
        Assert.Throws<NotSupportedException>(() => ((IList<DependentPeriod>)input.Dependents).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<MultipleSalaryMonthResult>)result.Months).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<SalaryPayerMonthResult>)result.Months[0].Payers).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<SalaryReceipt>)result.Months[0].Payers[0].Payments).Clear());
    }

    [Fact]
    public void MissingAndMismatchedYearInputsAreRejected()
    {
        var months = Simultaneous(0, 0);
        Assert.Throws<ArgumentException>(() => new MultipleSalaryYearInput(2026, Scope, true, true, months.Append(null!), []));
        Assert.Throws<ArgumentException>(() => new MultipleSalaryYearInput(2026, Scope, true, true, months, [null!]));
        var wrong = months.ToArray(); wrong[0] = new(2025, 1, 0, []);
        Assert.Throws<ArgumentException>(() => Input(wrong));
        var dates = months.ToArray(); dates[0] = new(2026, 1, 0, [new("A", [new("id", new(2025, 1, 1), 0, 0, Subject)])]);
        Assert.Throws<ArgumentException>(() => Input(dates));
        _ = Calc(Input(months));
    }
}
