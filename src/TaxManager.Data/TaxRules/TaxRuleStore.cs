using TaxManager.Core;
using TaxManager.Data.Infrastructure;

namespace TaxManager.Data.TaxRules;

/// <summary>Explicit access to the monthly manual-insurance timeline.</summary>
public sealed class TaxRuleStore(TaxManagerDbContext context)
{
    public Task SeedReferencesAsync(CancellationToken cancellationToken = default) =>
        new ReferenceTaxRuleSeeder(context).SeedAsync(cancellationToken);

    public Task ImportOfficialAsync(IEnumerable<TaxRuleRegistration> registrations,
        CancellationToken cancellationToken = default) =>
        new TaxRuleImporter(context).ImportOfficialAsync(registrations, cancellationToken);

    public Task<TaxRuleProvider> LoadOfficialAsync(CancellationToken cancellationToken = default) =>
        new TaxRuleProviderLoader(context).LoadOfficialAsync(cancellationToken);

    public Task<TaxRuleProvider> LoadReferencesAsync(CancellationToken cancellationToken = default) =>
        new TaxRuleProviderLoader(context).LoadReferencesAsync(cancellationToken);
}
