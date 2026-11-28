using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace TaxManager.Web.Accounts;

public sealed record GoogleSignIn(bool Enabled)
{
    public const string Scheme = GoogleDefaults.AuthenticationScheme;
    public const string FlowKey = "taxmanager.google.flow";
}

internal static class GoogleSignInServices
{
    internal static void AddGoogleSignIn(this IServiceCollection services, IConfiguration configuration)
    {
        var enabled = configuration.GetValue<bool>("Authentication:Google:Enabled");
        services.AddSingleton(new GoogleSignIn(enabled));
        if (!enabled) return;
        var clientId = configuration["Authentication:Google:ClientId"];
        var secret = configuration["Authentication:Google:ClientSecret"];
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException("Bật Google cần cấu hình ClientId và ClientSecret bên ngoài mã nguồn.");

        services.ConfigureExternalCookie(options =>
        {
            options.Cookie.Name = "__Host-taxmanager.external";
            options.Cookie.Path = "/";
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
            options.SlidingExpiration = false;
        });
        services.AddAuthentication().AddGoogle(options =>
        {
            options.ClientId = clientId;
            options.ClientSecret = secret;
            options.SignInScheme = IdentityConstants.ExternalScheme;
            options.Scope.Clear();
            options.Scope.Add("openid");
            options.ClaimActions.Clear();
            options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "sub");
            options.UsePkce = true;
            options.SaveTokens = false;
            options.RemoteAuthenticationTimeout = TimeSpan.FromMinutes(5);
            options.Events.OnTicketReceived = context =>
            {
                context.Response.Headers.CacheControl = "no-store";
                return Task.CompletedTask;
            };
            options.Events.OnRemoteFailure = context =>
            {
                context.HandleResponse();
                context.Response.Headers.CacheControl = "no-store";
                context.Response.Redirect("/tai-khoan/google/ket-qua?failed=true");
                return Task.CompletedTask;
            };
        });
    }
}
