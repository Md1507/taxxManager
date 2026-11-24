using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaxManager.Web.MonthlySalary;

namespace TaxManager.Web.Tests;

public sealed class DemoHost : IAsyncDisposable
{
    private readonly WebApplication app;
    private DemoHost(WebApplication app, Uri address) { this.app = app; Address = address; }
    public Uri Address { get; }
    public IServiceProvider Services => app.Services;

    public static async Task<DemoHost> StartAsync(ISalaryCalculator? calculator = null)
    {
        var directory = new DirectoryInfo(Path.GetDirectoryName(SourcePath())!);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "TaxManager.sln"))) directory = directory.Parent;
        var webRoot = Path.Combine(directory?.FullName ?? throw new InvalidOperationException("Solution root not found"), "src", "TaxManager.Web");
        var app = DemoApplication.Build(["--SyntheticDemo=true"], "Development", 0, webRoot,
            calculator is null ? null : services => services.Replace(ServiceDescriptor.Singleton(calculator)));
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        return new DemoHost(app, new Uri(address));
    }

    private static string SourcePath([CallerFilePath] string path = "") => path;

    public async ValueTask DisposeAsync() { await app.StopAsync(); await app.DisposeAsync(); }
}
