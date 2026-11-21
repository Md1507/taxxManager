namespace TaxManager.Core;

public enum InsuranceRegion { I = 1, II, III, IV }
public enum InsuranceSubject { VietnameseFullMonthEmployee = 1 }

public sealed record SalaryRoundingPolicy
{
    public string Id { get; }
    public string Version { get; }
    public LegalBasis Basis { get; }
    public SalaryRoundingPolicy(string id, string version, LegalBasis basis)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id); ArgumentException.ThrowIfNullOrWhiteSpace(version);
        ArgumentNullException.ThrowIfNull(basis);
        if (id != "no-rounding") throw new NotSupportedException("Only the explicitly selected no-rounding policy is supported.");
        Id = id; Version = version; Basis = basis;
    }
}

public sealed record InsuranceComponentRule
{
    public decimal Rate { get; }
    public decimal? Floor { get; }
    public decimal Cap { get; }
    public LegalBasis RateBasis { get; }
    public LegalBasis? FloorBasis { get; }
    public LegalBasis CapFormulaBasis { get; }
    public LegalBasis CapValueBasis { get; }
    public InsuranceComponentRule(decimal rate, decimal? floor, decimal cap, LegalBasis rateBasis,
        LegalBasis? floorBasis, LegalBasis capFormulaBasis, LegalBasis capValueBasis)
    {
        if (rate is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(rate));
        if (floor < 0 || cap < 0 || floor > cap) throw new ArgumentException("Invalid insurance floor or cap.");
        ArgumentNullException.ThrowIfNull(rateBasis); ArgumentNullException.ThrowIfNull(capFormulaBasis); ArgumentNullException.ThrowIfNull(capValueBasis);
        if (floor.HasValue && floorBasis is null) throw new ArgumentNullException(nameof(floorBasis));
        Rate = rate; Floor = floor; Cap = cap; RateBasis = rateBasis; FloorBasis = floorBasis; CapFormulaBasis = capFormulaBasis; CapValueBasis = capValueBasis;
    }
}

public sealed record InsuranceRuleSet
{
    public string Id { get; }
    public string Version { get; }
    public InsuranceSubject Subject { get; }
    public InsuranceRegion Region { get; }
    public DateOnly PeriodFrom { get; }
    public DateOnly PeriodTo { get; }
    public decimal ScopeMinimumSalary { get; }
    public LegalBasis ScopeMinimumBasis { get; }
    public InsuranceComponentRule Social { get; }
    public InsuranceComponentRule Health { get; }
    public InsuranceComponentRule Unemployment { get; }
    public InsuranceRuleSet(string id, string version, InsuranceSubject subject, InsuranceRegion region, DateOnly periodFrom, DateOnly periodTo,
        decimal scopeMinimumSalary, LegalBasis scopeMinimumBasis, InsuranceComponentRule social, InsuranceComponentRule health, InsuranceComponentRule unemployment)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id); ArgumentException.ThrowIfNullOrWhiteSpace(version);
        if (!Enum.IsDefined(subject)) throw new ArgumentOutOfRangeException(nameof(subject));
        if (!Enum.IsDefined(region)) throw new ArgumentOutOfRangeException(nameof(region));
        if (periodFrom > periodTo || periodFrom.Day != 1 || periodTo.Day != DateTime.DaysInMonth(periodTo.Year, periodTo.Month)) throw new ArgumentException("Insurance periods must cover whole months.");
        ArgumentNullException.ThrowIfNull(scopeMinimumBasis); ArgumentNullException.ThrowIfNull(social); ArgumentNullException.ThrowIfNull(health); ArgumentNullException.ThrowIfNull(unemployment);
        if (unemployment.Floor.HasValue)
            throw new ArgumentException("A separate unemployment floor is not supported.", nameof(unemployment));
        if (scopeMinimumSalary < 0 || social.Floor > scopeMinimumSalary || social.Cap < scopeMinimumSalary || health.Cap != social.Cap || health.Floor != social.Floor || unemployment.Cap < scopeMinimumSalary)
            throw new ArgumentException("Inconsistent full-month guard, floors or caps.");
        Id = id; Version = version; Subject = subject; Region = region; PeriodFrom = periodFrom; PeriodTo = periodTo;
        ScopeMinimumSalary = scopeMinimumSalary; ScopeMinimumBasis = scopeMinimumBasis; Social = social; Health = health; Unemployment = unemployment;
    }
}
