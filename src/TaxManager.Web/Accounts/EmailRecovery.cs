using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace TaxManager.Web.Accounts;

public sealed record RecoveryEmailState(string? Confirmed, string? Pending);

public sealed class EmailRecovery(EmailRecoverySettings settings, IAccountEmailSender sender, UserManager<LocalUser> users,
    LocalIdentityDb registry, LocalAccountStores stores, AccountDeletion deletion, TimeProvider clock, IHttpContextAccessor accessor)
{
    private const string Provider = "TaxManager.EmailRecovery";
    private const string PendingKey = "PendingEmail";
    private const string ResetKey = "ResetRequest";
    private sealed record Request(string Email, string Nonce, DateTimeOffset IssuedAt);

    public async Task<RecoveryEmailState> ReadAsync(LocalUser user)
    {
        var pending = await ReadRequestAsync(user, PendingKey);
        return new(user.EmailConfirmed ? user.Email : null, pending?.Email);
    }

    // Called in the authenticated request gate, after credential confirmation.
    public async Task<bool> RequestEmailAsync(LocalUser user, string email, CancellationToken token)
    {
        if (!settings.Enabled || await deletion.IsPendingAsync(user)) return false;
        var previous = await ReadRequestAsync(user, PendingKey);
        if (previous is not null && clock.GetUtcNow() - previous.IssuedAt < TimeSpan.FromMinutes(1)) return false;
        var request = NewRequest(email.Trim());
        var code = await users.GenerateChangeEmailTokenAsync(user, request.Email);
        if (!(await users.SetAuthenticationTokenAsync(user, Provider, PendingKey, JsonSerializer.Serialize(request))).Succeeded) return false;
        try
        {
            await sender.SendAsync(new(request.Email, "Xác minh email khôi phục TaxManager", Message(
                "Xác minh email khôi phục cho tài khoản " + user.UserName + ".",
                Link("/tai-khoan/xac-minh-email", user.Id, request.Nonce, code))), token);
            return true;
        }
        catch (AccountEmailDeliveryException)
        {
            await users.RemoveAuthenticationTokenAsync(user, Provider, PendingKey);
            return false;
        }
    }

    public async Task CancelPendingAsync(LocalUser user) => await users.RemoveAuthenticationTokenAsync(user, Provider, PendingKey);

    public async Task<bool> RemoveEmailAsync(LocalUser user)
    {
        await using var transaction = await registry.Database.BeginTransactionAsync();
        if (!(await users.SetEmailAsync(user, null)).Succeeded) return false;
        if (!(await users.RemoveAuthenticationTokenAsync(user, Provider, PendingKey)).Succeeded ||
            !(await users.RemoveAuthenticationTokenAsync(user, Provider, ResetKey)).Succeeded) return false;
        await transaction.CommitAsync();
        return true;
    }

    public Task<bool> CanConfirmAsync(string userId, string nonce, string code) => WithUserAsync(userId, async user =>
        await ValidPendingAsync(user, PendingKey, nonce) is { } request &&
        await users.VerifyUserTokenAsync(user!, RecoveryTokenProvider.ProviderName, UserManager<LocalUser>.GetChangeEmailTokenPurpose(request.Email), Decode(code) ?? ""));

    public Task<bool> ConfirmAsync(string userId, string nonce, string code) => WithUserAsync(userId, async user =>
    {
        var request = await ValidPendingAsync(user, PendingKey, nonce);
        if (request is null || Decode(code) is not { } decoded) return false;
        await using var transaction = await registry.Database.BeginTransactionAsync();
        // Keep local registrations email-optional; serialize this check and the Identity update in SQLite.
        var normalized = users.NormalizeEmail(request.Email);
        if (await registry.Users.AnyAsync(other => other.Id != user!.Id && other.EmailConfirmed && other.NormalizedEmail == normalized)) return false;
        if (!(await users.ChangeEmailAsync(user!, request.Email, decoded)).Succeeded) return false;
        if (!(await users.RemoveAuthenticationTokenAsync(user!, Provider, PendingKey)).Succeeded ||
            !(await users.RemoveAuthenticationTokenAsync(user!, Provider, ResetKey)).Succeeded) return false;
        await transaction.CommitAsync();
        return true;
    });

    public async Task RequestResetAsync(string email, CancellationToken token)
    {
        if (!settings.Enabled) return;
        var normalized = users.NormalizeEmail(email.Trim());
        var ids = await registry.Users.AsNoTracking().Where(user => user.EmailConfirmed && user.NormalizedEmail == normalized)
            .Select(user => user.Id).Take(2).ToArrayAsync(token);
        // A legacy duplicate must not select an arbitrary account or reveal the ambiguity.
        if (ids.Length != 1) return;
        await WithUserAsync(ids[0], async user =>
        {
            if (user is null || !user.EmailConfirmed || user.NormalizedEmail != normalized || await deletion.IsPendingAsync(user)) return false;
            var previous = await ReadRequestAsync(user, ResetKey);
            if (previous is not null && clock.GetUtcNow() - previous.IssuedAt < TimeSpan.FromMinutes(1)) return false;
            var request = NewRequest(user.Email!);
            var code = await users.GeneratePasswordResetTokenAsync(user);
            if (!(await users.SetAuthenticationTokenAsync(user, Provider, ResetKey, JsonSerializer.Serialize(request))).Succeeded) return false;
            try
            {
                await sender.SendAsync(new(user.Email!, "Đặt lại mật khẩu TaxManager", Message(
                    "Đặt lại mật khẩu cho tài khoản " + user.UserName + ".",
                    Link("/tai-khoan/dat-lai-mat-khau", user.Id, request.Nonce, code))), token);
                return true;
            }
            catch (AccountEmailDeliveryException)
            {
                await users.RemoveAuthenticationTokenAsync(user, Provider, ResetKey);
                return false;
            }
        });
    }

    public Task<bool> CanResetAsync(string userId, string nonce, string code) => WithUserAsync(userId, async user =>
        await ValidResetAsync(user, nonce) && await users.VerifyUserTokenAsync(user!, RecoveryTokenProvider.ProviderName,
            UserManager<LocalUser>.ResetPasswordTokenPurpose, Decode(code) ?? ""));

    public Task<bool> ResetAsync(string userId, string nonce, string code, string password) => WithUserAsync(userId, async user =>
    {
        if (!await ValidResetAsync(user, nonce) || Decode(code) is not { } decoded) return false;
        await using var transaction = await registry.Database.BeginTransactionAsync();
        if (!(await users.ResetPasswordAsync(user!, decoded, password)).Succeeded) return false;
        if (!(await users.ResetAccessFailedCountAsync(user!)).Succeeded ||
            !(await users.SetLockoutEndDateAsync(user!, null)).Succeeded ||
            !(await users.RemoveAuthenticationTokenAsync(user!, Provider, ResetKey)).Succeeded ||
            !(await users.RemoveAuthenticationTokenAsync(user!, Provider, PendingKey)).Succeeded) return false;
        await transaction.CommitAsync();
        return true;
    });

    private async Task<bool> ValidResetAsync(LocalUser? user, string nonce)
    {
        var request = await ValidPendingAsync(user, ResetKey, nonce);
        return request is not null && user!.EmailConfirmed && user.NormalizedEmail == users.NormalizeEmail(request.Email);
    }
    private async Task<Request?> ValidPendingAsync(LocalUser? user, string key, string nonce)
    {
        if (!settings.Enabled || user is null || await deletion.IsPendingAsync(user)) return null;
        var request = await ReadRequestAsync(user, key);
        return request?.Nonce == nonce ? request : null;
    }
    private async Task<Request?> ReadRequestAsync(LocalUser user, string key)
    {
        var value = await users.GetAuthenticationTokenAsync(user, Provider, key);
        if (value is null) return null;
        try
        {
            var request = JsonSerializer.Deserialize<Request>(value);
            return request is { Email.Length: > 0, Nonce.Length: > 0 } && request.IssuedAt <= clock.GetUtcNow() &&
                clock.GetUtcNow() - request.IssuedAt < TimeSpan.FromMinutes(30) ? request : null;
        }
        catch (JsonException) { return null; }
    }
    private async Task<bool> WithUserAsync(string userId, Func<LocalUser?, Task<bool>> action)
    {
        if (!Guid.TryParse(userId, out _)) return false;
        var current = accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (current is not null && current != userId) return false;
        var gate = stores.Gate(userId);
        var ownsGate = current is null;
        if (ownsGate) await gate.WaitAsync();
        try
        {
            // Cookie authentication may have tracked an older Identity row before waiting for the gate.
            registry.ChangeTracker.Clear();
            return await action(await users.FindByIdAsync(userId));
        }
        finally { if (ownsGate) gate.Release(); }
    }
    private Request NewRequest(string email) => new(email, Guid.NewGuid().ToString("N"), clock.GetUtcNow());
    private string Link(string path, string userId, string nonce, string code) => QueryHelpers.AddQueryString(
        new Uri(settings.Origin!, path).AbsoluteUri, new Dictionary<string, string?>
        {
            ["userId"] = userId, ["requestId"] = nonce, ["code"] = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code))
        });
    private static string Message(string instruction, string url) => instruction + "\n\n" + url +
        "\n\nLiên kết có hiệu lực 30 phút. Nếu bạn không yêu cầu, hãy bỏ qua thư này. Không chia sẻ liên kết.";
    private static string? Decode(string code)
    {
        if (code.Length > 4096) return null;
        try { return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code)); }
        catch (FormatException) { return null; }
    }
}
