using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mail;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace TaxManager.Web.Accounts;

public sealed record EmailRecoverySettings(bool Enabled, Uri? Origin = null);
public sealed record AccountEmail(string To, string Subject, string Text);
public interface IAccountEmailSender { Task SendAsync(AccountEmail email, CancellationToken token); }
public sealed class AccountEmailDeliveryException : Exception;

internal static class AccountEmailServices
{
    public const string ClientName = "account-email";
    internal static void AddAccountEmail(this IServiceCollection services, IConfiguration configuration)
    {
        var enabled = configuration.GetValue<bool>("EmailRecovery:Enabled");
        services.AddScoped<EmailRecovery>();
        services.Configure<RecoveryTokenOptions>(options =>
        {
            options.Name = "TaxManager.AccountRecovery.v1";
            options.TokenLifespan = TimeSpan.FromMinutes(30);
        });
        if (!enabled)
        {
            services.AddSingleton(new EmailRecoverySettings(false));
            services.AddSingleton<IAccountEmailSender, DisabledAccountEmailSender>();
            return;
        }
        var key = configuration["EmailRecovery:Resend:ApiKey"];
        var from = configuration["EmailRecovery:Resend:From"];
        if (string.IsNullOrWhiteSpace(key) || !MailAddress.TryCreate(from, out _) ||
            !Uri.TryCreate(configuration["EmailRecovery:PublicOrigin"], UriKind.Absolute, out var origin) ||
            origin.Scheme != "https" || origin.Host is not ("localhost" or "127.0.0.1") ||
            origin.AbsolutePath != "/" || origin.Query.Length != 0 || origin.Fragment.Length != 0 || origin.UserInfo.Length != 0)
            throw new InvalidOperationException("Bật email khôi phục cần PublicOrigin HTTPS loopback, địa chỉ gửi và API key bên ngoài mã nguồn.");
        services.AddSingleton(new EmailRecoverySettings(true, origin));
        services.AddHttpClient(ClientName, client => client.Timeout = TimeSpan.FromSeconds(15))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        services.AddScoped<IAccountEmailSender>(provider => new ResendAccountEmailSender(
            provider.GetRequiredService<IHttpClientFactory>(), from!, key!));
    }

    private sealed class DisabledAccountEmailSender : IAccountEmailSender
    {
        public Task SendAsync(AccountEmail email, CancellationToken token) => throw new AccountEmailDeliveryException();
    }
}

public sealed class RecoveryTokenOptions : DataProtectionTokenProviderOptions;
public sealed class RecoveryTokenProvider(IDataProtectionProvider protection, IOptions<RecoveryTokenOptions> options,
    ILogger<DataProtectorTokenProvider<LocalUser>> logger) : DataProtectorTokenProvider<LocalUser>(protection, options, logger)
{
    public const string ProviderName = "TaxManager.AccountRecovery";
}

public sealed class ResendAccountEmailSender(IHttpClientFactory clients, string from, string apiKey) : IAccountEmailSender
{
    public async Task SendAsync(AccountEmail email, CancellationToken token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = JsonContent.Create(new { from, to = new[] { email.To }, subject = email.Subject, text = email.Text });
        try
        {
            using var response = await clients.CreateClient(AccountEmailServices.ClientName).SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            if (!response.IsSuccessStatusCode) throw new AccountEmailDeliveryException();
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
            // No upstream body, recipient, token, URL or API key in the exception/log.
            throw new AccountEmailDeliveryException();
        }
    }
}
