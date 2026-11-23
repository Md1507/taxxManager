namespace TaxManager.Data.Upgrades;

public sealed record UpgradeResult(string TargetPath, int Profiles, int Incomes, int Dependents, int Periods);
