using System.Net;
using Microsoft.AspNetCore.DataProtection;
using TaxManager.Web.Demo;
using TaxManager.Web.MonthlySalary;
using TaxManager.Web.Backup;
using Microsoft.AspNetCore.Antiforgery;
using TaxManager.Web.Accounts;
using Microsoft.EntityFrameworkCore;

namespace TaxManager.Web;

public static class DemoApplication
{
    public static WebApplication Build(string[] args, string? environment = null, int port = 5088, string? contentRoot = null,
        Action<IServiceCollection>? configureServices = null)
    {
        var current = Directory.GetCurrentDirectory();
        var candidate = Path.Combine(current, "src", "TaxManager.Web");
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            ApplicationName = typeof(DemoApplication).Assembly.FullName,
            EnvironmentName = environment,
            ContentRootPath = contentRoot ?? (Directory.Exists(Path.Combine(current, "wwwroot")) ? current :
                Directory.Exists(Path.Combine(candidate, "wwwroot")) ? candidate : AppContext.BaseDirectory)
        });
        if (!builder.Environment.IsDevelopment() || builder.Configuration["SyntheticDemo"] != "true")
            throw new InvalidOperationException("Chỉ chạy demo Development với SyntheticDemo=true.");
        var accounts = new AccountMode(builder.Configuration["SyntheticAccounts"] == "true");
        builder.Services.AddSingleton(accounts);
        builder.WebHost.UseUrls($"{(accounts.Enabled ? "https" : "http")}://127.0.0.1:{port}");
        builder.Logging.ClearProviders();
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton(_ => new SyntheticDatabase(Directory.GetCurrentDirectory(), builder.Configuration["SyntheticStore"]));
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton<BackupStore>();
        builder.Services.AddSingleton<BackupWorkspaces>();
        builder.Services.AddScoped<SelectedDatabase>();
        builder.Services.AddSingleton<IAntiforgeryAdditionalDataProvider, WorkspaceAntiforgery>();
        builder.Services.AddScoped<ISalaryCalculator>(services => new SalaryCalculator(services.GetRequiredService<SelectedDatabase>().Database));
        builder.Services.AddRazorPages();
        if (accounts.Enabled) builder.Services.AddLocalAccounts();
        configureServices?.Invoke(builder.Services);
        var app = builder.Build();
        if (accounts.Enabled)
        {
            using var scope = app.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<LocalIdentityDb>().Database.EnsureCreated();
        }
        app.Use(async (context, next) =>
        {
            if (context.Connection.RemoteIpAddress is not { } remote || !IPAddress.IsLoopback(remote) ||
                !(context.Request.Host.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                  context.Request.Host.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }
            if (accounts.Enabled && !context.Request.IsHttps)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }
            if (!accounts.Enabled && context.Request.Path.StartsWithSegments("/tai-khoan"))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }
            await next();
        });
        app.UseStaticFiles();
        app.UseRouting();
        if (accounts.Enabled)
        {
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseRateLimiter();
        }
        app.Use(async (context, next) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            if (accounts.Enabled && context.Request.Path.StartsWithSegments("/tai-khoan"))
            {
                await next();
                return;
            }
            if (accounts.Enabled && await context.RequestServices.GetRequiredService<LocalDataUse>().ReadAsync(context.User) is null)
            {
                if (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method))
                    context.Response.Redirect("/tai-khoan/du-lieu-thu?returnUrl=" +
                        Uri.EscapeDataString(context.Request.PathBase + context.Request.Path + context.Request.QueryString));
                else
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync("Cần xác nhận phạm vi dữ liệu thử tại /tai-khoan/du-lieu-thu trước khi dùng hồ sơ.");
                }
                return;
            }
            SelectedDatabase workspace;
            try { workspace = context.RequestServices.GetRequiredService<SelectedDatabase>(); }
            catch (Exception ex) when (accounts.Enabled && BackupFailure.IsExpected(ex))
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await context.Response.WriteAsync("Chưa mở được kho tài khoản. Giữ nguyên kho và khóa, không tạo kho thay thế.");
                return;
            }
            await workspace.State.Gate.WaitAsync(context.RequestAborted);
            try { await next(); }
            finally { workspace.State.Gate.Release(); }
        });
        app.MapRazorPages();
        return app;
    }
}
