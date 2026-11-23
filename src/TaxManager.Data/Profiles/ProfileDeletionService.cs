using Microsoft.EntityFrameworkCore;
using TaxManager.Data.Infrastructure;

namespace TaxManager.Data.Profiles;

/// <summary>Business deletion retains encrypted audit, including children not loaded by the caller.</summary>
public sealed class ProfileDeletionService(TaxManagerDbContext context)
{
    public async Task DeleteAsync(Guid profileId, CancellationToken token = default)
    {
        var profile = await context.UserProfiles.SingleOrDefaultAsync(p => p.Id == profileId, token);
        if (profile is null) return;
        context.Remove(profile);
        await context.SaveChangesAsync(token);
    }
}
