using TaxManager.Data.Infrastructure;

namespace TaxManager.Data.TaxRules;

public sealed class ReferenceTaxRuleSeeder(TaxManagerDbContext context)
{
    public Task SeedAsync(CancellationToken cancellationToken = default) =>
        new TaxRuleImporter(context).SeedReferencesAsync(cancellationToken);
}
