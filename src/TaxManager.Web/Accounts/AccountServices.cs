using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;

namespace TaxManager.Web.Accounts;

internal static class AccountServices
{
    internal static void AddLocalAccounts(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<LocalAccountStores>();
        services.AddScoped<LocalDataUse>();
        services.AddScoped<AccountDataExport>();
        services.AddScoped<AccountDeletion>();
        services.AddScoped<GoogleAccountConfirmation>();
        services.AddSingleton<GoogleConfirmationTickets>();
        services.AddDbContext<LocalIdentityDb>((provider, options) =>
            options.UseSqlite(provider.GetRequiredService<LocalAccountStores>().IdentityConnection));
        services.AddIdentity<LocalUser, IdentityRole>(options =>
        {
            options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789._-";
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Tokens.ChangeEmailTokenProvider = RecoveryTokenProvider.ProviderName;
            options.Tokens.PasswordResetTokenProvider = RecoveryTokenProvider.ProviderName;
        }).AddEntityFrameworkStores<LocalIdentityDb>().AddTokenProvider<RecoveryTokenProvider>(RecoveryTokenProvider.ProviderName);
        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "__Host-taxmanager.identity";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.Path = "/";
            options.LoginPath = "/tai-khoan/dang-nhap";
            options.AccessDeniedPath = "/tai-khoan";
            options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
            options.SlidingExpiration = false;
        });
        services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.Zero);
        services.AddAuthorization(options => options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("account-entry", _ => RateLimitPartition.GetFixedWindowLimiter("local", _ => new()
            {
                PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true
            }));
        });
        services.AddAntiforgery(options => options.Cookie.SecurePolicy = CookieSecurePolicy.Always);
        services.AddGoogleSignIn(configuration);
        services.AddAccountEmail(configuration);
    }
}
