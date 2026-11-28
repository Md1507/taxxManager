using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaxManager.Web.MonthlySalary;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace TaxManager.Web.Tests;

public sealed class DemoHost : IAsyncDisposable
{
    private readonly WebApplication app;
    private DemoHost(WebApplication app, Uri address) { this.app = app; Address = address; }
    public Uri Address { get; }
    public IServiceProvider Services => app.Services;

    private static readonly Lazy<X509Certificate2> Certificate = new(() =>
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=localhost", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var san = new SubjectAlternativeNameBuilder();
        san.AddIpAddress(System.Net.IPAddress.Loopback);
        request.CertificateExtensions.Add(san.Build());
        using var generated = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(1));
        // Schannel needs an imported key container rather than CreateSelfSigned's ephemeral RSA handle.
        return X509CertificateLoader.LoadPkcs12(generated.Export(X509ContentType.Pfx), null);
    });

    public HttpClient AccountClient(System.Net.CookieContainer? cookies = null) => new(new HttpClientHandler
    {
        AllowAutoRedirect = false,
        CookieContainer = cookies ?? new(),
        ServerCertificateCustomValidationCallback = (_, certificate, _, _) => certificate?.Thumbprint == Certificate.Value.Thumbprint
    }) { BaseAddress = Address };

    public static async Task<DemoHost> StartAsync(ISalaryCalculator? calculator = null, TimeProvider? clock = null, string? syntheticRoot = null, bool accounts = false,
        string[]? extraArgs = null, Action<IServiceCollection>? configure = null)
    {
        var directory = new DirectoryInfo(Path.GetDirectoryName(SourcePath())!);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "TaxManager.sln"))) directory = directory.Parent;
        var webRoot = Path.Combine(directory?.FullName ?? throw new InvalidOperationException("Solution root not found"), "src", "TaxManager.Web");
        var args = new List<string> { "--SyntheticDemo=true" };
        if (syntheticRoot is not null) args.Add("--SyntheticStore=" + syntheticRoot);
        if (accounts) args.Add("--SyntheticAccounts=true");
        if (extraArgs is not null) args.AddRange(extraArgs);
        var app = DemoApplication.Build(args.ToArray(), "Development", 0, webRoot,
            services =>
            {
                if (calculator is not null) services.Replace(ServiceDescriptor.Singleton(calculator));
                if (clock is not null) services.Replace(ServiceDescriptor.Singleton(clock));
                if (accounts) services.Configure<KestrelServerOptions>(options =>
                    options.ConfigureHttpsDefaults(https => https.ServerCertificate = Certificate.Value));
                configure?.Invoke(services);
            });
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        return new DemoHost(app, new Uri(address));
    }

    private static string SourcePath([CallerFilePath] string path = "") => path;

    public async ValueTask DisposeAsync() { await app.StopAsync(); await app.DisposeAsync(); }
}
