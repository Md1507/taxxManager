using System.Collections.ObjectModel;

namespace TaxManager.Core;

public enum VerificationStatus { Missing, Unverified, Verified }

public sealed record LegalBasis
{
    public string? Id { get; }
    public string? Version { get; }
    public string? Point { get; }
    public string? Clause { get; }
    public string? Article { get; }
    public string? DocumentNumber { get; }
    public string? Summary { get; }
    public string? SourceUrl { get; }
    public string? Period { get; }
    public DateOnly? EffectiveFrom { get; }
    public DateOnly? EffectiveTo { get; }
    public VerificationStatus Status { get; }
    public DateOnly? VerifiedOn { get; }

    public LegalBasis(VerificationStatus status, string? id = null, string? version = null,
        string? point = null, string? clause = null, string? article = null, string? documentNumber = null,
        string? summary = null, string? sourceUrl = null, string? period = null,
        DateOnly? effectiveFrom = null, DateOnly? effectiveTo = null, DateOnly? verifiedOn = null)
    {
        if (!Enum.IsDefined(status)) throw new ArgumentOutOfRangeException(nameof(status));
        if (effectiveFrom > effectiveTo) throw new ArgumentException("Effective dates are reversed.");
        if (status != VerificationStatus.Verified && verifiedOn != null)
            throw new ArgumentException("Only verified sources can have a verification date.");
        if (status == VerificationStatus.Verified &&
            (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(version) || verifiedOn is null ||
             !Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
             !(uri.Host.Equals("vbpl.vn", StringComparison.OrdinalIgnoreCase) || uri.Host.EndsWith(".vbpl.vn", StringComparison.OrdinalIgnoreCase))))
            throw new ArgumentException("Verified basis requires an ID, version, verification date and HTTPS vbpl.vn URL.");
        Id = id; Version = version; Point = point; Clause = clause; Article = article;
        DocumentNumber = documentNumber; Summary = summary; SourceUrl = sourceUrl; Period = period;
        EffectiveFrom = effectiveFrom; EffectiveTo = effectiveTo; Status = status; VerifiedOn = verifiedOn;
    }
    public static LegalBasis Missing { get; } = new(VerificationStatus.Missing);
}

public sealed record Bracket
{
    public decimal? UpperBound { get; }
    public decimal Rate { get; }
    public LegalBasis Basis { get; }
    public Bracket(decimal? upperBound, decimal rate, LegalBasis? basis = null)
    {
        if (rate is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(rate));
        UpperBound = upperBound; Rate = rate; Basis = basis ?? LegalBasis.Missing;
    }
}

public sealed class TaxRuleSet
{
    public string Id { get; }
    public string Version { get; }
    public string Period { get; }
    public int FromYear { get; }
    public decimal PersonalDeduction { get; }
    public decimal DependentDeduction { get; }
    public IReadOnlyList<Bracket> Brackets { get; }
    public LegalBasis PersonalDeductionBasis { get; }
    public LegalBasis DependentDeductionBasis { get; }
    public LegalBasis TaxableIncomeBasis { get; }
    public LegalBasis TaxBasis { get; }

    public TaxRuleSet(string id, string version, string period, int fromYear, decimal personalDeduction,
        decimal dependentDeduction, IEnumerable<Bracket> brackets, LegalBasis? personalDeductionBasis = null,
        LegalBasis? dependentDeductionBasis = null, LegalBasis? taxableIncomeBasis = null, LegalBasis? taxBasis = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id); ArgumentException.ThrowIfNullOrWhiteSpace(version);
        ArgumentException.ThrowIfNullOrWhiteSpace(period); ArgumentNullException.ThrowIfNull(brackets);
        if (fromYear < 1) throw new ArgumentOutOfRangeException(nameof(fromYear));
        if (personalDeduction < 0) throw new ArgumentOutOfRangeException(nameof(personalDeduction));
        if (dependentDeduction < 0) throw new ArgumentOutOfRangeException(nameof(dependentDeduction));
        var copy = brackets.ToArray();
        if (copy.Length == 0 || copy.Any(b => b is null)) throw new ArgumentException("Brackets cannot be empty or contain null.", nameof(brackets));
        decimal previous = 0;
        for (int i = 0; i < copy.Length; i++)
        {
            var upper = copy[i].UpperBound;
            if (i == copy.Length - 1 ? upper is not null : upper is null || upper <= previous)
                throw new ArgumentException("Finite bounds must increase and the last bracket must be unbounded.", nameof(brackets));
            if (upper.HasValue) previous = upper.Value;
        }
        Id = id; Version = version; Period = period; FromYear = fromYear;
        PersonalDeduction = personalDeduction; DependentDeduction = dependentDeduction;
        Brackets = new ReadOnlyCollection<Bracket>(copy);
        PersonalDeductionBasis = personalDeductionBasis ?? LegalBasis.Missing;
        DependentDeductionBasis = dependentDeductionBasis ?? LegalBasis.Missing;
        TaxableIncomeBasis = taxableIncomeBasis ?? LegalBasis.Missing; TaxBasis = taxBasis ?? LegalBasis.Missing;
    }

    // PLAN data only; FromYear is a label, not a legal effective date.
    public static TaxRuleSet Vietnam2026 { get; } = CreateVietnam2026();
    private static TaxRuleSet CreateVietnam2026()
    {
        var basis = new LegalBasis(VerificationStatus.Unverified, id: "plan-2026", version: "reference-1", period: "2026 reference");
        return new TaxRuleSet("vietnam-2026-reference", "1", "2026 reference", 2026, 15_500_000m, 6_200_000m,
            [new(10_000_000m, .05m, basis), new(30_000_000m, .10m, basis), new(60_000_000m, .20m, basis),
             new(100_000_000m, .30m, basis), new(null, .35m, basis)], basis, basis, basis, basis);
    }
}
