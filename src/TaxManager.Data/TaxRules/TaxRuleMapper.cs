using TaxManager.Core;

namespace TaxManager.Data.TaxRules;

public static class TaxRuleMapper
{
    private static TaxRuleLegalBasis ToEntity(LegalBasis basis) => new()
    {
        Key = Guid.NewGuid(),
        Id = basis.Id,
        Version = basis.Version,
        Point = basis.Point,
        Clause = basis.Clause,
        Article = basis.Article,
        DocumentNumber = basis.DocumentNumber,
        Summary = basis.Summary,
        SourceUrl = basis.SourceUrl,
        Period = basis.Period,
        EffectiveFrom = basis.EffectiveFrom,
        EffectiveTo = basis.EffectiveTo,
        Status = (int)basis.Status,
        VerifiedOn = basis.VerifiedOn
    };

    private static LegalBasis ToCore(TaxRuleLegalBasis basis)
    {
        ArgumentNullException.ThrowIfNull(basis);
        return new LegalBasis((VerificationStatus)basis.Status, basis.Id, basis.Version,
            basis.Point, basis.Clause, basis.Article, basis.DocumentNumber, basis.Summary,
            basis.SourceUrl, basis.Period, basis.EffectiveFrom, basis.EffectiveTo, basis.VerifiedOn);
    }

    public static TaxRuleSnapshot ToEntity(TaxRuleSet rules)
    {
        TaxRuleValidator.ValidateSupported(rules);
        var entity = new TaxRuleSnapshot
        {
            RuleId = rules.Id,
            Version = rules.Version,
            Period = rules.Period,
            FromYear = rules.FromYear,
            PersonalDeduction = rules.PersonalDeduction,
            DependentDeduction = rules.DependentDeduction,
            AnnualGraph = rules.AnnualSalaryRules is null ? null : AnnualRuleGraphCodec.Encode(rules),
            PersonalBasis = ToEntity(rules.PersonalDeductionBasis),
            DependentBasis = ToEntity(rules.DependentDeductionBasis),
            IncomeBasis = ToEntity(rules.TaxableIncomeBasis),
            TaxBasis = ToEntity(rules.TaxBasis)
        };
        entity.Brackets = rules.Brackets.Select((b, index) => new TaxRuleBracket
        {
            RuleId = rules.Id,
            Version = rules.Version,
            Ordinal = index,
            UpperBound = b.UpperBound,
            Rate = b.Rate,
            Basis = ToEntity(b.Basis)
        }).ToList();
        return entity;
    }

    public static TaxRuleSet ToCore(TaxRuleSnapshot entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        var brackets = entity.Brackets.OrderBy(x => x.Ordinal).ToArray();
        if (brackets.Length == 0 || brackets.Where((b, i) => b.Ordinal != i).Any())
            throw new InvalidOperationException("Tax rule brackets are missing or out of order.");
        var result = new TaxRuleSet(entity.RuleId, entity.Version, entity.Period, entity.FromYear,
            entity.PersonalDeduction, entity.DependentDeduction,
            brackets.Select(b => new Bracket(b.UpperBound, b.Rate, ToCore(b.Basis))),
            ToCore(entity.PersonalBasis), ToCore(entity.DependentBasis),
            ToCore(entity.IncomeBasis), ToCore(entity.TaxBasis));
        TaxRuleValidator.ValidateSupported(result);
        if (entity.AnnualGraph is not null)
        {
            var annual = AnnualRuleGraphCodec.Decode(entity.AnnualGraph);
            if (!TaxRuleStoreSupport.SameRules(result, annual))
                throw new InvalidOperationException("Annual graph does not match its monthly snapshot.");
            TaxRuleValidator.ValidateSupported(annual);
            return annual;
        }
        return result;
    }
}
