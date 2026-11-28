using System.Net;
using Microsoft.AspNetCore.DataProtection;
using TaxManager.Web.Demo;
using TaxManager.Web.MonthlySalary;
using TaxManager.Web.Backup;
using Microsoft.AspNetCore.Antiforgery;

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
        builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
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
        configureServices?.Invoke(builder.Services);
        var app = builder.Build();
        app.Use(async (context, next) =>
        {
            if (context.Connection.RemoteIpAddress is not { } remote || !IPAddress.IsLoopback(remote) ||
                !(context.Request.Host.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                  context.Request.Host.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }
            await next();
        });
        app.UseStaticFiles();
        app.Use(async (context, next) =>
        {
            var workspace = context.RequestServices.GetRequiredService<SelectedDatabase>();
            await workspace.State.Gate.WaitAsync(context.RequestAborted);
            try { await next(); }
            finally { workspace.State.Gate.Release(); }
        });
        app.MapRazorPages();
        return app;
    }
}
