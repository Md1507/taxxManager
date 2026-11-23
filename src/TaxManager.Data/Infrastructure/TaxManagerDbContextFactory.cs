using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TaxManager.Data.Infrastructure;

public sealed class TaxManagerDbContextFactory : IDesignTimeDbContextFactory<TaxManagerDbContext>
{
    public TaxManagerDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TaxManagerDbContext>()
            // Model generation has no operational database or key configuration.
            .UseSqlite("Data Source=:memory:;Foreign Keys=True")
            .Options;
        return new TaxManagerDbContext(options);
    }
}
