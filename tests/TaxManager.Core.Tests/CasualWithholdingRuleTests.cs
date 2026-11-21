using TaxManager.Core;
using Xunit;
namespace TaxManager.Core.Tests;
public class CasualWithholdingRuleTests
{
    static LegalBasis Basis(string? id="test", string? version="1", string? article="50", string? url="https://example.org/test", string? period="2026", DateOnly? from=null, DateOnly? to=null) => new(VerificationStatus.Unverified,id,version,article:article,sourceUrl:url,period:period,effectiveFrom:from,effectiveTo:to);
    static CasualWithholdingRuleSet Rule(LegalBasis? basis=null, int year=2026, DateOnly? from=null, DateOnly? to=null, CasualTiming timing=CasualTiming.ReceivedOrPaidDate, decimal threshold=4000000m, decimal rate=.08m, IEnumerable<CasualPaymentSubject>? subjects=null, CasualRuleMode mode=CasualRuleMode.FlatPerCompletePayment, string version="1") { var b=basis ?? Basis(version:version); return new("test",version,year,from ?? new(2026,7,1),to ?? new(2026,12,31),timing,mode,threshold,rate,subjects ?? [CasualPaymentSubject.NoContract],b,b,b,b,b,b,b); }
    static TaxRuleProvider Provider(CasualWithholdingRuleSet r) { var s=TaxRuleSet.Vietnam2026; var snapshot=new TaxRuleSet("test","1","2026 casual withholding H2",2026,s.PersonalDeduction,s.DependentDeduction,s.Brackets,casualWithholdingRules:r); return new([new(snapshot,new(2026,7,1),new(2026,12,31))]); }
    static CasualPayment Payment(DateOnly? date=null) => new("A","P",date ?? new(2026,7,1),4000000,CasualPaymentScope.ResidentNonBusinessFullyTaxableVndInVietnam,CasualPaymentSubject.NoContract,CasualRequestMode.Normal);
    [Fact] public void CustomRuleAndSnapshot() { var r=Rule(); var result=CasualWithholdingCalculator.Calculate(Payment(),Provider(r)); Assert.Equal(320000m,result.ExpectedWithheld); Assert.Same(r,result.Rules); Assert.Contains(r.RateBasis,result.DecisionBases); }
    [Fact] public void GuardBasisIndependently()
    {
        Rule();
        foreach(var b in new[]{ LegalBasis.Missing, Basis(id:""), Basis(article:""), Basis(version:"2"), Basis(url:"http://example.org"), Basis(period:"2025"), Basis(from:new(2026,7,2)), Basis(to:new(2026,12,30)) }) Assert.ThrowsAny<ArgumentException>(()=>Rule(b));
        Assert.Throws<ArgumentException>(()=>Rule(timing:0));
        Assert.Throws<ArgumentException>(()=>Rule(year:2027,from:new(2027,7,1),to:new(2027,12,31),basis:Basis(period:"2027")));
        Assert.Throws<ArgumentException>(()=>Rule(threshold:0)); Assert.Throws<ArgumentException>(()=>Rule(rate:0));
    }
    [Fact] public void CoverageAndCopies()
    {
        var subjects=new List<CasualPaymentSubject>{CasualPaymentSubject.NoContract}; var r=Rule(subjects:subjects); subjects.Clear();
        Assert.Single(r.SupportedSubjects); Assert.Equal(320000m,CasualWithholdingCalculator.Calculate(Payment(),Provider(r)).ExpectedWithheld);
        var partial=Rule(from:new(2026,7,2)); Assert.Throws<NotSupportedException>(()=>CasualWithholdingCalculator.Calculate(Payment(new(2026,7,3)),Provider(partial)));
        Assert.Throws<NotSupportedException>(()=>CasualWithholdingCalculator.Calculate(Payment(),new TaxRuleProvider([])));
    }
    static TaxRuleSet Snapshot(CasualWithholdingRuleSet? rule, string period="2026 casual withholding H2", int year=2026, string version="1")
    {
        var s=TaxRuleSet.Vietnam2026;
        return new("test",version,period,year,s.PersonalDeduction,s.DependentDeduction,s.Brackets,casualWithholdingRules:rule);
    }
    static TaxRuleProvider Registered(TaxRuleSet snapshot, DateOnly? from=null, DateOnly? to=null) => new([new(snapshot,from ?? new(2026,7,1),to ?? new(2026,12,31))]);
    [Fact] public void RuleGuardsHaveValidControl()
    {
        Assert.Equal(320000m,CasualWithholdingCalculator.Calculate(Payment(),Provider(Rule())).ExpectedWithheld);
        Assert.Throws<ArgumentException>(() => Rule(mode:CasualRuleMode.Unspecified));
        Assert.Throws<ArgumentException>(() => Rule(timing:CasualTiming.Unspecified));
        Assert.Throws<ArgumentException>(() => Rule(year:2027,from:new(2027,7,1),to:new(2027,12,31),basis:Basis(period:"2027")));
        Assert.Throws<ArgumentException>(() => Rule(subjects:[]));
        Assert.Throws<ArgumentException>(() => Rule(subjects:[(CasualPaymentSubject)99]));
        Assert.Throws<ArgumentException>(() => Rule(rate:1.01m));
        Assert.Throws<ArgumentException>(() => Rule(basis:Basis(version:"")));
        Assert.Throws<ArgumentException>(() => Rule(basis:Basis(url:null)));
    }
    [Fact] public void SnapshotAndCoverageGuardsHaveValidControl()
    {
        var r=Rule(); var valid=Snapshot(r);
        Assert.Same(valid,CasualWithholdingCalculator.Calculate(Payment(),Registered(valid)).RuleSet);
        Assert.Throws<NotSupportedException>(()=>CasualWithholdingCalculator.Calculate(Payment(),Registered(Snapshot(null))));
        Assert.Throws<NotSupportedException>(()=>CasualWithholdingCalculator.Calculate(Payment(),Registered(Snapshot(r,period:"2026 monthly"))));
        Assert.Throws<NotSupportedException>(()=>CasualWithholdingCalculator.Calculate(Payment(),Registered(Snapshot(r,year:2025))));
        Assert.Throws<NotSupportedException>(()=>CasualWithholdingCalculator.Calculate(Payment(),Registered(valid,new(2026,8,1))));
        var endEarly=Rule(to:new(2026,7,30));
        Assert.Throws<NotSupportedException>(()=>CasualWithholdingCalculator.Calculate(Payment(),Registered(Snapshot(endEarly))));
        var unsupported=new CasualPayment("A","P",new(2026,7,1),4000000,CasualPaymentScope.ResidentNonBusinessFullyTaxableVndInVietnam,CasualPaymentSubject.ContractUnderThreeMonths,CasualRequestMode.Normal);
        Assert.Throws<NotSupportedException>(()=>CasualWithholdingCalculator.Calculate(unsupported,Registered(valid)));
    }
    [Fact] public void SnapshotVersionAndBasesFollowPaymentDate()
    {
        var first=Rule(to:new(2026,7,31)); var second=Rule(from:new(2026,8,1),version:"2");
        var s1=Snapshot(first,version:"1"); var s2=Snapshot(second,version:"2");
        var registrations=new List<TaxRuleRegistration>{new(s1,new(2026,7,1),new(2026,7,31)),new(s2,new(2026,8,1),new(2026,12,31))};
        var provider=new TaxRuleProvider(registrations); registrations.Clear();
        var july=CasualWithholdingCalculator.Calculate(Payment(),provider); var august=CasualWithholdingCalculator.Calculate(Payment(new(2026,8,1)),provider);
        Assert.Same(s1,july.RuleSet); Assert.Same(s2,august.RuleSet); Assert.Equal("2",august.Rules.Version);
        Assert.Contains(second.RateBasis,august.DecisionBases); Assert.DoesNotContain(first.RateBasis,august.DecisionBases);
        Assert.Equal(VerificationStatus.Missing,august.SubjectInputBasis.Status); Assert.Equal(VerificationStatus.Missing,august.ActualInputBasis.Status);
        Assert.Throws<NotSupportedException>(()=>((IList<LegalBasis>)august.DecisionBases).Clear());
    }
}
