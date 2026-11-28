using System.Security.Cryptography;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.DataProtection;
using TaxManager.Web.Demo;
using TaxManager.Web.Accounts;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace TaxManager.Web.Backup;

/// <summary>Host-owned stores; each browser has its own selection and one expiring preview.</summary>
public sealed class BackupWorkspaces : IDisposable
{
    private readonly Dictionary<string, WorkspaceState> states = new();
    private readonly Dictionary<SyntheticDatabase, WorkspaceState> restored = new();
    private readonly ITimer timer;
    private readonly TimeProvider clock;
    public BackupWorkspaces(TimeProvider clock)
    {
        this.clock = clock;
        timer = clock.CreateTimer(_ => Sweep(), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }
    public WorkspaceState Get(string owner)
    {
        lock (states)
        {
            if (states.TryGetValue(owner, out var state)) return state;
            if (states.Count >= 128) throw new InvalidOperationException("Demo browser limit reached. Restart the host.");
            return states[owner] = new WorkspaceState();
        }
    }
    public void Accept(WorkspaceState state)
    {
        lock (states)
        {
            if (restored.Count >= 8) throw new InvalidOperationException("Demo restored-store limit reached.");
            var preview = state.Preview ?? throw new InvalidOperationException("Preview is missing.");
            if (preview.ExpiresAt <= clock.GetUtcNow()) throw new InvalidOperationException("Preview expired.");
            restored.Add(preview.Store, state);
            state.Active = preview.Store;
            state.Preview = null;
            state.Generation++;
        }
    }
    public void Cancel(WorkspaceState state)
    {
        var preview = state.Preview;
        state.Preview = null;
        if (preview is null) return;
        preview.Store.Dispose();
        BackupStore.DeleteRoot(preview.Store.RootPath);
    }
    public string[] AccountRoots(string userId)
    {
        lock (states)
        {
            var owned = states.Where(pair => pair.Key.StartsWith(userId + ":", StringComparison.Ordinal))
                .Select(pair => pair.Value).ToHashSet();
            return restored.Where(pair => owned.Contains(pair.Value)).Select(pair => pair.Key.RootPath)
                .Concat(owned.Where(state => state.Preview is not null).Select(state => state.Preview!.Store.RootPath))
                .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }
    }
    public void ForgetAccount(string userId)
    {
        lock (states)
        {
            var owned = states.Where(pair => pair.Key.StartsWith(userId + ":", StringComparison.Ordinal)).ToArray();
            var ownedStates = owned.Select(pair => pair.Value).ToHashSet();
            foreach (var pair in restored.Where(pair => ownedStates.Contains(pair.Value)).ToArray())
            {
                pair.Key.Dispose();
                restored.Remove(pair.Key);
            }
            foreach (var pair in owned)
            {
                pair.Value.Preview?.Store.Dispose();
                pair.Value.Preview = null;
                pair.Value.Active = null;
                pair.Value.Generation++;
                states.Remove(pair.Key);
            }
        }
    }
    private void Sweep()
    {
        lock (states)
            foreach (var state in states.Values)
                if (state.Gate.Wait(0))
                    try
                    {
                        if (state.Preview is { } preview && preview.ExpiresAt <= clock.GetUtcNow()) Cancel(state);
                    }
                    catch (IOException) { /* TEMP cleanup can be retried by the OS/operator; never activate a failed preview. */ }
                    catch (UnauthorizedAccessException) { }
                    finally { state.Gate.Release(); }
    }
    public void Dispose()
    {
        timer.Dispose();
        lock (states)
        {
            foreach (var state in states.Values) Cancel(state);
            foreach (var store in restored.Keys) store.Dispose(); // Confirmed roots remain durable for explicit reopen.
        }
    }
}

public sealed class WorkspaceState
{
    public SemaphoreSlim Gate { get; } = new(1, 1);
    public long Generation { get; set; }
    public SyntheticDatabase? Active { get; set; }
    public RestorePreview? Preview { get; set; }
}
public sealed record RestorePreview(string Handle, DateTimeOffset ExpiresAt, SyntheticDatabase Store, BackupSummary Summary, int Duplicates, string Fingerprint);

/// <summary>Request-bound access. Root SyntheticDatabase remains the default outside HTTP requests.</summary>
public sealed class SelectedDatabase
{
    private const string CookieName = "taxmanager.workspace";
    private readonly SyntheticDatabase original;
    public string Owner { get; }
    public WorkspaceState State { get; }
    public bool IsRestored => State.Active is not null;
    public SyntheticDatabase Database => State.Active ?? original;
    public string Stamp => Owner + ":" + State.Generation;
    public TaxManager.Data.Infrastructure.TaxManagerDbContext Open() => Database.Open();

    public SelectedDatabase(SyntheticDatabase original, BackupWorkspaces workspaces, IHttpContextAccessor accessor, IDataProtectionProvider protection,
        AccountMode mode, IServiceProvider services)
    {
        this.original = original;
        var http = accessor.HttpContext ?? throw new InvalidOperationException("A request is required.");
        var protector = protection.CreateProtector("TaxManager.BackupWorkspace.v1");
        string? owner = null;
        if (http.Request.Cookies.TryGetValue(CookieName, out var cookie))
            try
            {
                var decoded = protector.Unprotect(cookie);
                if (Guid.TryParseExact(decoded, "N", out _)) owner = decoded;
            }
            catch (CryptographicException) { }
        var browser = owner ?? Guid.NewGuid().ToString("N");
        if (mode.Enabled)
        {
            var id = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (http.User.Identity?.IsAuthenticated != true || id is null)
                throw new InvalidOperationException("Cần đăng nhập trước khi mở kho.");
            var account = services.GetRequiredService<LocalIdentityDb>().Users.AsNoTracking().SingleOrDefault(user => user.Id == id)
                ?? throw new InvalidOperationException("Không còn tài khoản.");
            this.original = services.GetRequiredService<LocalAccountStores>().Get(account.Id, account.StoreRoot);
            Owner = id + ":" + browser;
        }
        else Owner = browser;
        if (owner is null) http.Response.Cookies.Append(CookieName, protector.Protect(browser), new CookieOptions
        {
            HttpOnly = true, SameSite = SameSiteMode.Strict, Secure = http.Request.IsHttps, IsEssential = true, Path = "/"
        });
        State = workspaces.Get(Owner);
    }
}

/// <summary>Old tabs cannot submit a form against a newly selected store with matching entity IDs.</summary>
public sealed class WorkspaceAntiforgery(IHttpContextAccessor accessor, AccountMode mode) : IAntiforgeryAdditionalDataProvider
{
    private string Stamp(HttpContext context) => mode.Enabled && context.Request.Path.StartsWithSegments("/tai-khoan")
        ? "accounts:" + (context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous") : Current(context).Stamp;
    public string GetAdditionalData(HttpContext context) => Stamp(context);
    public bool ValidateAdditionalData(HttpContext context, string additionalData) => Stamp(context) == additionalData;
    private SelectedDatabase Current(HttpContext context) =>
        (accessor.HttpContext ?? context).RequestServices.GetRequiredService<SelectedDatabase>();
}
