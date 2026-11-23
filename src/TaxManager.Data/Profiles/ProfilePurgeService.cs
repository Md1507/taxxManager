using Microsoft.EntityFrameworkCore;
using TaxManager.Data.Audit;
using TaxManager.Data.Infrastructure;

namespace TaxManager.Data.Profiles;

/// <summary>Explicit erasure from active tables, not physical erasure of backups or free pages.</summary>
public sealed class ProfilePurgeService(TaxManagerDbContext context)
{
    public async Task PurgeAsync(Guid profileId, AuditWriteContext caller, CancellationToken token = default)
    {
        if (profileId == Guid.Empty || string.IsNullOrWhiteSpace(caller.Actor) || string.IsNullOrWhiteSpace(caller.Context))
            throw new ArgumentException("Purge requires an explicit target, actor and purpose.");
        if (context.ChangeTracker.Entries().Any())
            throw new InvalidOperationException("Purge requires a fresh context with no tracked entities.");
        token.ThrowIfCancellationRequested();
        var owned = context.Database.CurrentTransaction is null ? await context.Database.BeginTransactionAsync(token) : null;
        var transaction = context.Database.CurrentTransaction!;
        var savepoint = "purge_" + Guid.NewGuid().ToString("N");
        var created = false;
        try
        {
            SqliteWriteRequirements.Validate(context);
            if (owned is null)
            {
                if (!transaction.SupportsSavepoints) throw new InvalidOperationException("Purge requires savepoints.");
                await transaction.CreateSavepointAsync(savepoint, token);
                created = true;
            }
            await context.Database.ExecuteSqlRawAsync("UPDATE TaxRuleSnapshot SET RuleId = RuleId WHERE 0", token);
            context.ValidateProtection();
            // No new receipt/payload is retained that would link the target after purge.
            await context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM AuditEvents WHERE ProfileId = {profileId}", token);
            await context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM UserProfiles WHERE Id = {profileId}", token);
            token.ThrowIfCancellationRequested();
            if (owned is not null) await owned.CommitAsync(token);
            else await transaction.ReleaseSavepointAsync(savepoint, token);
        }
        catch
        {
            if (owned is not null) await owned.RollbackAsync(CancellationToken.None);
            else if (created) await transaction.RollbackToSavepointAsync(savepoint, CancellationToken.None);
            throw;
        }
        finally { if (owned is not null) await owned.DisposeAsync(); }
    }
}
