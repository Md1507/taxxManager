using System.Reflection;
using TaxManager.Core;

namespace TaxManager.Data.CalculationHistory;

public sealed record CalculationEngineSnapshot(string Name, int ContractVersion, string AssemblyVersion,
    string InformationalVersion, Guid ModuleVersionId)
{
    internal static CalculationEngineSnapshot Current()
    {
        var assembly = typeof(MonthlySalaryCalculator).Assembly;
        return new(typeof(MonthlySalaryCalculator).FullName!, 1, assembly.GetName().Version!.ToString(),
            assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion,
            assembly.ManifestModule.ModuleVersionId);
    }
}
