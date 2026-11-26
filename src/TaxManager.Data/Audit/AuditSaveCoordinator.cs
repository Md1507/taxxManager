using System.Collections;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using TaxManager.Data.Dependents;
using TaxManager.Data.Income;
using TaxManager.Data.Infrastructure;
using TaxManager.Data.Profiles;
using TaxManager.Data.Protection;
using TaxManager.Data.CalculationHistory;
using TaxManager.Data.Forecasting;

namespace TaxManager.Data.Audit;

internal sealed class AuditSaveCoordinator
{
    private readonly TaxManagerDbContext context;
    private readonly ProtectionSession protection;
    private readonly HashSet<object> pendingAcceptance = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<object> databaseOriginals = new(ReferenceEqualityComparer.Instance);

    internal AuditSaveCoordinator(TaxManagerDbContext context, ProtectionSession protection)
    {
        this.context = context;
        this.protection = protection;
        context.ChangeTracker.Tracked += (_, e) =>
        {
            // Clear does not raise StateChanged. A new tracking lifetime (even for the
            // same CLR object) must not inherit pending acceptance or loaded originals.
            pendingAcceptance.Remove(e.Entry.Entity);
            databaseOriginals.Remove(e.Entry.Entity);
            // Attach establishes an Unchanged baseline, even for a key-only stub.
            // Bind notifications now so only subsequent public setter calls signal writes.
            protection.BindFields(e.Entry.Entity);
            if (e.FromQuery && ProtectionSession.IsBusiness(e.Entry.Entity))
                databaseOriginals.Add(e.Entry.Entity);
        };
        context.ChangeTracker.StateChanged += (_, e) =>
        {
            if (e.NewState == EntityState.Detached)
            {
                pendingAcceptance.Remove(e.Entry.Entity);
                databaseOriginals.Remove(e.Entry.Entity);
            }
            else if (e.NewState == EntityState.Unchanged && pendingAcceptance.Remove(e.Entry.Entity) &&
                     ProtectionSession.IsBusiness(e.Entry.Entity))
                databaseOriginals.Add(e.Entry.Entity);
        };
    }

    internal async Task<int> Save(bool async, bool accept, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        context.ChangeTracker.DetectChanges();
        if (context.ChangeTracker.Entries<ForecastSnapshot>().Any(e => e.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Forecast snapshots are append-only.");
        if (context.ChangeTracker.Entries<CalculationRecord>().Any(e =>
            e.State is EntityState.Modified or EntityState.Deleted ||
            e.State == EntityState.Added && !ReferenceEquals(e.Entity, context.AuthorizedHistoryAppend)))
            throw new InvalidOperationException("Calculation history can only be appended by the calculation service; use profile purge for erasure.");
        var tracked = context.ChangeTracker.Entries().Select(e => e.Entity).ToHashSet(ReferenceEqualityComparer.Instance);
        pendingAcceptance.IntersectWith(tracked);
        databaseOriginals.IntersectWith(tracked);
        if (pendingAcceptance.Count > 0)
            throw new InvalidOperationException("AcceptAllChanges or clear/reopen the context before another save after SaveChanges(false).");
        if (context.ChangeTracker.Entries().Any(e => e.Entity is AuditEvent or DataProtectionMetadata &&
                e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Audit and protection metadata are application append-only.");

        var hasBusiness = context.ChangeTracker.Entries().Any(e => ProtectionSession.IsBusiness(e.Entity) && HasWriteIntent(e));
        if (!context.ChangeTracker.HasChanges()) return 0;
        var owned = context.Database.CurrentTransaction is null
            ? (async ? await context.Database.BeginTransactionAsync(token).ConfigureAwait(false) : context.Database.BeginTransaction())
            : null;
        var transaction = context.Database.CurrentTransaction!;
        var savepoint = "audit_" + Guid.NewGuid().ToString("N");
        var events = new List<AuditEvent>();
        var savepointCreated = false;
        var commitStarted = false;
        try
        {
            SqliteWriteRequirements.Validate(context);
            if (owned is null)
            {
                if (!transaction.SupportsSavepoints)
                    throw new InvalidOperationException("The caller transaction must support savepoints.");
                if (async) await transaction.CreateSavepointAsync(savepoint, token).ConfigureAwait(false);
                else transaction.CreateSavepoint(savepoint);
                savepointCreated = true;
            }
            // Promote even a deferred caller transaction before collecting before-state or cascades.
            if (async) await context.Database.ExecuteSqlRawAsync("UPDATE TaxRuleSnapshot SET RuleId = RuleId WHERE 0", token).ConfigureAwait(false);
            else context.Database.ExecuteSqlRaw("UPDATE TaxRuleSnapshot SET RuleId = RuleId WHERE 0");
            if (hasBusiness)
            {
                protection.Ensure(initialize: true);
                ExpandCascades();
                foreach (var entry in context.ChangeTracker.Entries().Where(e =>
                             ProtectionSession.IsBusiness(e.Entity) &&
                             e.State is EntityState.Added or EntityState.Modified).ToArray())
                    protection.BindFields(entry.Entity);
            }
            var changes = hasBusiness ? Collect(token) : [];
            context.ValidateEntitiesForSave();
            if (changes.Count > 0)
            {
                var caller = context.AuditContext;
                if (caller is null || string.IsNullOrWhiteSpace(caller.Actor) || string.IsNullOrWhiteSpace(caller.Context) ||
                    caller.OperationId == Guid.Empty)
                    throw new InvalidOperationException("Explicit audit actor and context are required for business changes.");
                var operation = caller.OperationId ?? Guid.NewGuid();
                if (context.Set<AuditEvent>().AsNoTracking().Any(e => e.OperationId == operation))
                    throw new InvalidOperationException("OperationId is already recorded; reconcile using a fresh context before retrying.");
                var now = context.Clock.GetUtcNow().ToUniversalTime();
                for (var ordinal = 0; ordinal < changes.Count; ordinal++)
                {
                    token.ThrowIfCancellationRequested();
                    var change = changes[ordinal];
                    var e = new AuditEvent
                    {
                        Id = Guid.NewGuid(), OperationId = operation, Ordinal = ordinal,
                        ProfileId = ProtectionSession.ProfileId(change.Entry.Entity),
                        Entity = change.Entry.Entity.GetType().Name, EntityKey = ProtectionSession.RowKey(change.Entry.Entity),
                        Action = change.Entry.State switch { EntityState.Added => "Add", EntityState.Deleted => "Delete", _ => "Update" },
                        TimestampUtc = now
                    };
                    e.Payload = protection.ProtectAudit(e, new AuditPayload(caller.Actor, caller.Context,
                        caller.Correlation, change.Before, change.After));
                    if (change.Entry.State != EntityState.Deleted) protection.Encrypt(change.Entry);
                    context.Add(e);
                    events.Add(e);
                }
            }
            token.ThrowIfCancellationRequested();
            var result = async
                ? await context.SaveCoreAsync(token).ConfigureAwait(false)
                : context.SaveCore();
            token.ThrowIfCancellationRequested();
            if (owned is not null)
            {
                commitStarted = true;
                if (async) await owned.CommitAsync(token).ConfigureAwait(false); else owned.Commit();
            }
            else
            {
                if (async) await transaction.ReleaseSavepointAsync(savepoint, token).ConfigureAwait(false);
                else transaction.ReleaseSavepoint(savepoint);
            }
            foreach (var e in events) context.Entry(e).State = EntityState.Detached;
            pendingAcceptance.UnionWith(context.ChangeTracker.Entries()
                .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                .Select(e => e.Entity));
            if (accept) context.ChangeTracker.AcceptAllChanges();
            return result - events.Count;
        }
        catch (Exception failure)
        {
            // Rollback must not use a canceled token. A commit exception can have an unknown outcome:
            // never retry automatically; reconcile the supplied OperationId with a new context.
            try
            {
                if (owned is not null)
                {
                    if (async) await owned.RollbackAsync(CancellationToken.None).ConfigureAwait(false); else owned.Rollback();
                }
                else if (savepointCreated)
                {
                    if (async) await transaction.RollbackToSavepointAsync(savepoint, CancellationToken.None).ConfigureAwait(false);
                    else transaction.RollbackToSavepoint(savepoint);
                }
            }
            catch when (commitStarted)
            {
                // A provider/interceptor can throw after the database committed. Preserve the
                // uncertainty instead of letting a failed rollback invite an unsafe blind retry.
            }
            finally
            {
                foreach (var e in events) context.Entry(e).State = EntityState.Detached;
            }
            if (commitStarted)
                throw new InvalidOperationException(
                    "Commit outcome may be unknown; reconcile OperationId with a fresh context before retrying.", failure);
            throw;
        }
        finally
        {
            if (owned is not null)
            {
                if (async) await owned.DisposeAsync().ConfigureAwait(false); else owned.Dispose();
            }
        }
    }

    private List<AuditChange> Collect(CancellationToken token)
    {
        var result = new List<AuditChange>();
        foreach (var entry in context.ChangeTracker.Entries().Where(e =>
                     ProtectionSession.IsBusiness(e.Entity) && HasWriteIntent(e)).ToArray())
        {
            token.ThrowIfCancellationRequested();
            if (entry.State == EntityState.Added)
            {
                TaxManagerDbContext.ValidateEntityForSave(entry.Entity);
                result.Add(new(entry, null, ProtectionSession.Snapshot(entry.Entity)));
                continue;
            }
            var stored = entry.GetDatabaseValues();
            if (stored is null) throw new InvalidOperationException("Business data changed or was deleted; reload before saving.");
            var beforeEntity = stored.ToObject();
            protection.Bind(beforeEntity);
            var before = ProtectionSession.Snapshot(beforeEntity);
            if (entry.State == EntityState.Modified && !databaseOriginals.Contains(entry.Entity))
            {
                // An Attach followed by selected setters is a partial update. Fill only
                // untouched fields from the locked DB before-state, including protected
                // amounts absent from a key-only stub. Detached Update marks all fields
                // modified and therefore retains its full replacement semantics.
                foreach (var property in entry.Properties.Where(p => !p.IsModified && !p.Metadata.IsPrimaryKey()))
                {
                    property.OriginalValue = stored[property.Metadata.Name];
                    property.CurrentValue = stored[property.Metadata.Name];
                }
            }
            if (entry.State != EntityState.Deleted) TaxManagerDbContext.ValidateEntityForSave(entry.Entity);
            var after = entry.State == EntityState.Deleted ? (JsonElement?)null : ProtectionSession.Snapshot(entry.Entity);
            if (after is not null && before.GetRawText() == after.Value.GetRawText())
            {
                // A detached Update or explicit Modified flag with equal values is a real no-op.
                entry.CurrentValues.SetValues(stored);
                entry.State = EntityState.Unchanged;
                databaseOriginals.Add(entry.Entity);
                continue;
            }
            // Attach/Update manufacture OriginalValues from the supplied object. Only
            // query-loaded or accepted entries have an original suitable for stale checks.
            if (databaseOriginals.Contains(entry.Entity) && entry.Properties.Any(p =>
                    !StructuralComparisons.StructuralEqualityComparer.Equals(p.OriginalValue, stored[p.Metadata.Name])))
                throw new InvalidOperationException("Business data changed since it was loaded; reload before saving.");
            if (entry.State != EntityState.Deleted) entry.State = EntityState.Modified;
            result.Add(new(entry, before, after));
        }
        return result;
    }

    private static bool HasWriteIntent(EntityEntry entry) =>
        entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted;

    private void ExpandCascades()
    {
        var profiles = context.ChangeTracker.Entries<UserProfile>().Where(e => e.State == EntityState.Deleted)
            .Select(e => e.Entity.Id).ToArray();
        foreach (var id in profiles)
        {
            context.MonthlyIncomes.Where(x => x.ProfileId == id).Load();
            context.Dependents.Where(x => x.ProfileId == id).Load();
            context.DependentEligibilityPeriods.Where(x => x.ProfileId == id).Load();
        }
        var dependents = context.ChangeTracker.Entries<Dependent>().Where(e => e.State == EntityState.Deleted ||
            profiles.Contains(e.Entity.ProfileId)).Select(e => (e.Entity.ProfileId, e.Entity.Id)).ToArray();
        foreach (var (profile, id) in dependents)
            context.DependentEligibilityPeriods.Where(x => x.ProfileId == profile && x.DependentId == id).Load();
        foreach (var entry in context.ChangeTracker.Entries().Where(e =>
                     e.Entity is MonthlyIncome or Dependent or DependentEligibilityPeriod).ToArray())
        {
            var cascade = profiles.Contains(ProtectionSession.ProfileId(entry.Entity)) ||
                entry.Entity is DependentEligibilityPeriod p && dependents.Contains((p.ProfileId, p.DependentId));
            if (cascade) context.Remove(entry.Entity);
        }
    }
}
