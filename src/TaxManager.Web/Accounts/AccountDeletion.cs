using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaxManager.Web.Backup;
using TaxManager.Web.Demo;

namespace TaxManager.Web.Accounts;

/// <summary>A durable cleanup manifest keeps interrupted filesystem deletion retryable.</summary>
public sealed class AccountDeletion(UserManager<LocalUser> users, LocalIdentityDb registry,
    LocalAccountStores stores, BackupWorkspaces workspaces, SyntheticDatabase shared)
{
    public const string ClaimType = "taxmanager.account-deletion.v1";
    private const int MaxRoots = 1 + 128 + 8; // Original, one preview per browser, accepted restores (host limits).
    private sealed record Manifest(string UserId, string OriginalRoot, string[] Roots);

    public async Task<bool> IsPendingAsync(LocalUser user) =>
        (await users.GetClaimsAsync(user)).Any(claim => claim.Type == ClaimType);

    // Caller holds the account request gate and has rechecked credentials and intent.
    public async Task DeleteAsync(LocalUser user)
    {
        var claims = (await users.GetClaimsAsync(user)).Where(claim => claim.Type == ClaimType).ToArray();
        if (claims.Length > 1) throw new InvalidDataException("Ambiguous deletion manifest.");
        Manifest manifest;
        if (claims.Length == 0)
        {
            manifest = new(user.Id, user.StoreRoot, new[] { user.StoreRoot }.Concat(workspaces.AccountRoots(user.Id))
                .Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
            await ValidateAsync(manifest, user, pending: false);
            var saved = await users.AddClaimAsync(user, new Claim(ClaimType, JsonSerializer.Serialize(manifest)));
            if (!saved.Succeeded) throw new InvalidOperationException("Could not record deletion intent.");
        }
        else
        {
            manifest = JsonSerializer.Deserialize<Manifest>(claims[0].Value)
                ?? throw new InvalidDataException("Missing deletion manifest.");
            await ValidateAsync(manifest, user, pending: true);
        }

        stores.Forget(user.Id);
        workspaces.ForgetAccount(user.Id);
        foreach (var root in manifest.Roots)
        {
            ValidateRoot(root, pending: true);
            BackupStore.DeleteRoot(root);
        }
        if (!(await users.DeleteAsync(user)).Succeeded)
            throw new InvalidOperationException("Could not finish account deletion.");
    }

    private async Task ValidateAsync(Manifest manifest, LocalUser user, bool pending)
    {
        if (manifest.UserId != user.Id || manifest.OriginalRoot != user.StoreRoot || manifest.Roots is not { Length: > 0 } || manifest.Roots.Length > MaxRoots ||
            !manifest.Roots.Contains(user.StoreRoot, StringComparer.OrdinalIgnoreCase))
            throw new InvalidDataException("Invalid deletion manifest.");
        var protectedRoots = await registry.Users.AsNoTracking().Where(other => other.Id != user.Id)
            .Select(other => other.StoreRoot).ToArrayAsync();
        foreach (var root in manifest.Roots)
        {
            ValidateRoot(root, pending);
            var full = Path.GetFullPath(root);
            if (string.Equals(full, shared.RootPath, StringComparison.OrdinalIgnoreCase) ||
                protectedRoots.Any(other => string.Equals(full, Path.GetFullPath(other), StringComparison.OrdinalIgnoreCase)))
                throw new InvalidDataException("Deletion target is not owned by this account.");
        }
    }

    private static void ValidateRoot(string root, bool pending)
    {
        var full = Path.GetFullPath(root);
        var temp = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar);
        if (!Path.IsPathFullyQualified(root) || !string.Equals(Path.GetDirectoryName(full), temp, StringComparison.OrdinalIgnoreCase) ||
            !Path.GetFileName(full).StartsWith("taxmanager-web-demo-", StringComparison.Ordinal))
            throw new InvalidDataException("Deletion target must be a synthetic TEMP store.");
        for (DirectoryInfo? part = new(temp); part is not null; part = part.Parent)
            if ((part.Attributes & FileAttributes.ReparsePoint) != 0) throw new InvalidDataException("TEMP contains a link.");
        if (!Path.Exists(full))
        {
            if (!pending) throw new InvalidDataException("The store is missing before deletion started.");
            return;
        }
        RejectLinks(new DirectoryInfo(full));
        var marker = Path.Combine(full, "synthetic-demo.marker");
        if (File.Exists(marker) ? File.ReadAllText(marker) != "TaxManager synthetic demo v1" : !pending)
            throw new InvalidDataException("Synthetic store marker is invalid.");
    }

    private static void RejectLinks(FileSystemInfo item)
    {
        if ((item.Attributes & FileAttributes.ReparsePoint) != 0) throw new InvalidDataException("Deletion target contains a link.");
        if (item is DirectoryInfo directory)
            foreach (var child in directory.EnumerateFileSystemInfos()) RejectLinks(child);
    }
}
