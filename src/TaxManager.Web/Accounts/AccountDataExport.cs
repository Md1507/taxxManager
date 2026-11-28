using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using TaxManager.Data.Exports;

namespace TaxManager.Web.Accounts;

public sealed record ExportAccount(string Id, string? Username, string[] DataUseAcknowledgements);
public sealed record AccountExportDocument(int SchemaVersion, DateTimeOffset CreatedAtUtc, string Scope,
    ExportAccount Account, PersonalTaxData TaxData);

public sealed class AccountDataExport(UserManager<LocalUser> users, LocalAccountStores stores, TimeProvider clock)
{
    public const int MaxBytes = 32 * 1024 * 1024;
    public async Task<byte[]> CreateAsync(ClaimsPrincipal principal, CancellationToken token)
    {
        var user = await users.GetUserAsync(principal) ?? throw new InvalidOperationException("Account is missing.");
        var acknowledgements = (await users.GetClaimsAsync(user)).Where(c => c.Type == LocalDataUse.ClaimType)
            .Select(c => c.Value).ToArray();
        // Original account store, deliberately independent of browser restore selection.
        var store = stores.Get(user.Id, user.StoreRoot);
        await using var context = store.Open();
        var taxData = await new PersonalTaxDataReader(context).ReadAsync(token);
        var document = new AccountExportDocument(1, clock.GetUtcNow(), "original-account-store",
            new ExportAccount(user.Id, user.UserName, acknowledgements), taxData);
        using var output = new LimitedExportStream();
        await JsonSerializer.SerializeAsync(output, document, new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        }, token);
        return output.ToArray();
    }

    private sealed class LimitedExportStream : MemoryStream
    {
        private void Check(int count)
        {
            if (Position + count > MaxBytes) throw new InvalidDataException("Personal export exceeds the file limit.");
        }
        public override void Write(byte[] buffer, int offset, int count) { Check(count); base.Write(buffer, offset, count); }
        public override void Write(ReadOnlySpan<byte> buffer) { Check(buffer.Length); base.Write(buffer); }
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            Write(buffer.Span);
            return ValueTask.CompletedTask;
        }
    }
}
