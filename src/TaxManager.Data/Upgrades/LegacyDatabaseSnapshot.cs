using TaxManager.Data.Dependents;
using TaxManager.Data.Income;
using TaxManager.Data.Profiles;
using TaxManager.Data.TaxRules;

namespace TaxManager.Data.Upgrades;

internal sealed record LegacyDatabaseSnapshot(
    List<UserProfile> Profiles, List<MonthlyIncome> Incomes, List<Dependent> Dependents,
    List<DependentEligibilityPeriod> Periods, List<TaxRuleLegalBasis> Bases,
    List<TaxRuleSnapshot> Rules, List<TaxRuleBracket> Brackets, List<TaxRulePeriod> Registrations);
