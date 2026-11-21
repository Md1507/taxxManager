using TaxManager.Core;
using Xunit;
namespace TaxManager.Core.Tests;

public class CasualWithholdingTests
{
    static readonly TaxRuleProvider Provider = TaxRuleSet.CreateVietnam2026CasualWithholdingProvider();
    static CasualPayment Pay(decimal paid, CasualRequestMode mode = CasualRequestMode.Normal, decimal? actual = null, CasualCommitment? commitment = null, string id = "A", DateOnly? date = null, string payer = "P") => new(id, payer, date ?? new DateOnly(2026,7,1), paid, CasualPaymentScope.ResidentNonBusinessFullyTaxableVndInVietnam, CasualPaymentSubject.NoContract, mode, actual, commitment);
    [Theory]
    [InlineData(0, 0)] [InlineData(4999999, 0)] [InlineData(5000000, 500000)] [InlineData(5000001, 500000.1)] [InlineData(2500000, 0)] [InlineData(6000000, 600000)]
    public void NormalBoundaries(decimal paid, decimal expected) => Assert.Equal(expected, CasualWithholdingCalculator.Calculate(Pay(paid), Provider).ExpectedWithheld);
    [Fact] public void VoluntaryAndCommitment()
    {
        Assert.Equal(250000m, CasualWithholdingCalculator.Calculate(Pay(2500000, CasualRequestMode.VoluntaryBelowThreshold), Provider).ExpectedWithheld);
        Assert.Throws<ArgumentException>(() => CasualWithholdingCalculator.Calculate(Pay(5000000, CasualRequestMode.VoluntaryBelowThreshold), Provider));
        var c = new CasualCommitment(true,true,true);
        var r = CasualWithholdingCalculator.Calculate(Pay(6000000, CasualRequestMode.CallerConfirmedCommitment, commitment:c), Provider);
        Assert.Equal(0m,r.ExpectedWithheld); Assert.Equal(CasualDecision.TemporarilyNotWithheldOnCallerCommitment,r.Decision); Assert.Contains(r.Rules.CommitmentBasis,r.DecisionBases);
        Assert.Throws<ArgumentException>(() => Pay(6000000, CasualRequestMode.CallerConfirmedCommitment));
        Assert.Throws<ArgumentException>(() => Pay(6000000, CasualRequestMode.CallerConfirmedCommitment, commitment:new(true,false,true)));
        Assert.Throws<ArgumentException>(() => Pay(6000000, commitment:c));
    }
    [Fact] public void ActualAndBatch()
    {
        var r=CasualWithholdingCalculator.Calculate(Pay(6000000,actual:400000),Provider);
        Assert.Equal(600000m,r.ExpectedWithheld); Assert.Equal(400000m,r.ActualWithheld); Assert.Equal(200000m,r.WithholdingDifference);
        Assert.Null(CasualWithholdingCalculator.Calculate(Pay(6000000),Provider).WithholdingDifference);
        var list=new List<CasualPayment>{Pay(3000000,id:" A "),Pay(3000000,id:"a")}; var batch=new CasualWithholdingBatch(" P ",list); list.Clear();
        var result=CasualWithholdingCalculator.CalculateBatch(batch,Provider); Assert.Equal(0m,result.TotalExpected); Assert.Null(result.TotalActual); Assert.Equal(2,result.Lines.Count);
        Assert.Throws<NotSupportedException>(() => ((IList<CasualWithholdingPaymentResult>)result.Lines).Clear());
        Assert.Throws<ArgumentException>(() => new CasualWithholdingBatch("P",[Pay(1,id:"A"),Pay(1,id:" A ")]));
        Assert.Throws<ArgumentException>(() => new CasualWithholdingBatch("P",[Pay(1,payer:"Q")]));
        Assert.Throws<OverflowException>(() => CasualWithholdingCalculator.CalculateBatch(new("P",[Pay(decimal.MaxValue,id:"A"),Pay(decimal.MaxValue,id:"B")]),Provider));
    }
    [Fact] public void DatesAndInput()
    {
        CasualWithholdingCalculator.Calculate(Pay(0,date:new(2026,12,31)),Provider);
        foreach (var date in new[]{new DateOnly(2026,6,30),new DateOnly(2027,1,1),new DateOnly(2025,12,31)}) Assert.Throws<NotSupportedException>(() => CasualWithholdingCalculator.Calculate(Pay(0,date:date),Provider));
        Assert.Throws<ArgumentOutOfRangeException>(() => Pay(-1)); Assert.Throws<ArgumentOutOfRangeException>(() => Pay(0,actual:-1));
        Assert.Throws<ArgumentException>(() => new CasualPayment(" ","P",new(2026,7,1),0, CasualPaymentScope.ResidentNonBusinessFullyTaxableVndInVietnam,CasualPaymentSubject.NoContract,CasualRequestMode.Normal));
        Assert.Throws<ArgumentException>(() => new CasualPayment("A","P",new(2026,7,1),0, 0,CasualPaymentSubject.NoContract,CasualRequestMode.Normal));
        Assert.Throws<ArgumentException>(() => new CasualPayment("A","P",new(2026,7,1),0, CasualPaymentScope.ResidentNonBusinessFullyTaxableVndInVietnam,0,CasualRequestMode.Normal));
    }
    [Fact] public void SeedAndProvenance()
    {
        var r=CasualWithholdingCalculator.Calculate(Pay(5000000),Provider);
        Assert.Equal(VerificationStatus.Missing,r.InputBasis.Status); Assert.Equal(VerificationStatus.Missing,r.DeclarationInputBasis.Status);
        Assert.All(r.DecisionBases,b => { Assert.Equal(VerificationStatus.Unverified,b.Status); Assert.Null(b.VerifiedOn); });
        Assert.Null(r.RuleSet.InsuranceRules); Assert.Equal("50",r.Rules.ThresholdBasis.Article);
        Assert.Throws<NotSupportedException>(() => ((IList<CasualPaymentSubject>)r.Rules.SupportedSubjects).Clear());
    }
    [Fact] public void CommitmentRequiresEveryConfirmationAndExclusiveMode()
    {
        Assert.Equal(0m, CasualWithholdingCalculator.Calculate(Pay(6000000, CasualRequestMode.CallerConfirmedCommitment, commitment:new(true,true,true)), Provider).ExpectedWithheld);
        foreach (var c in new[] { new CasualCommitment(false,true,true), new CasualCommitment(true,false,true), new CasualCommitment(true,true,false) })
            Assert.Throws<ArgumentException>(() => Pay(6000000, CasualRequestMode.CallerConfirmedCommitment, commitment:c));
        Assert.Throws<ArgumentException>(() => Pay(1, CasualRequestMode.CallerConfirmedCommitment));
        Assert.Throws<ArgumentException>(() => Pay(1, CasualRequestMode.VoluntaryBelowThreshold, commitment:new(true,true,true)));
    }
    [Fact] public void BatchActualAndIdentityAreIndependentPerPayment()
    {
        var known = CasualWithholdingCalculator.CalculateBatch(new(" P ", [Pay(6000000,actual:400000,id:"A"), Pay(3000000,actual:0,id:"a")]), Provider);
        Assert.Equal(400000m, known.TotalActual); Assert.Equal(200000m, known.TotalDifference);
        Assert.Equal(new[]{"A","a"}, known.Lines.Select(x => x.Input.PaymentId));
        var unknown = CasualWithholdingCalculator.CalculateBatch(new("P", [Pay(6000000,actual:400000,id:"A"), Pay(3000000,id:"B")]), Provider);
        Assert.Null(unknown.TotalActual); Assert.Null(unknown.TotalDifference);
        Assert.Equal(-100000m, CasualWithholdingCalculator.Calculate(Pay(6000000,actual:700000),Provider).WithholdingDifference);
        Assert.Equal(-6400000m, CasualWithholdingCalculator.Calculate(Pay(6000000,actual:7000000),Provider).WithholdingDifference);
        Assert.Throws<ArgumentException>(() => new CasualWithholdingBatch("P", [Pay(1,id:"A"),Pay(1,id:" A ",date:new(2026,8,1))]));
        Assert.Throws<ArgumentException>(() => new CasualWithholdingBatch("P", [Pay(1),Pay(1,id:"B",payer:"Q")]));
    }
    [Fact] public void ExplicitInputsAndSubjectsAreRequired()
    {
        foreach (var subject in new[]{CasualPaymentSubject.NoContract, CasualPaymentSubject.ContractUnderThreeMonths, CasualPaymentSubject.PostTerminationArticle50Paragraph2})
        {
            var p = new CasualPayment(" A "," P ",new(2026,7,1),6000000,CasualPaymentScope.ResidentNonBusinessFullyTaxableVndInVietnam,subject,CasualRequestMode.Normal,label:"bonus 2025");
            Assert.Equal(600000m,CasualWithholdingCalculator.Calculate(p,Provider).ExpectedWithheld); Assert.Equal("A",p.PaymentId); Assert.Equal("P",p.PayerId);
        }
        Assert.Throws<ArgumentException>(() => new CasualPayment("A","P",new(2026,7,1),1,CasualPaymentScope.ResidentNonBusinessFullyTaxableVndInVietnam,(CasualPaymentSubject)99,CasualRequestMode.Normal));
        Assert.Throws<ArgumentException>(() => new CasualPayment("A","P",new(2026,7,1),1,CasualPaymentScope.ResidentNonBusinessFullyTaxableVndInVietnam,CasualPaymentSubject.NoContract,(CasualRequestMode)99));
        Assert.Throws<ArgumentException>(() => Pay(1,payer:" "));
        Assert.Throws<ArgumentNullException>(() => CasualWithholdingCalculator.Calculate(null!,Provider));
    }
}

