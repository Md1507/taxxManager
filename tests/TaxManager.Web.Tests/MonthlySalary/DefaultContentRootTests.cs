using System.Net;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace TaxManager.Web.Tests.MonthlySalary;

public sealed class DefaultContentRootTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Default_host_serves_assets_from_repository_and_project_working_directories(bool projectDirectory)
    {
        var root = new DirectoryInfo(Path.GetDirectoryName(SourcePath())!);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "TaxManager.sln"))) root = root.Parent;
        Assert.NotNull(root);
        var previous = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(projectDirectory ? Path.Combine(root.FullName, "src", "TaxManager.Web") : root.FullName);
            await using var app = DemoApplication.Build(["--SyntheticDemo=true"], "Development", 0);
            await app.StartAsync();
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            using var client = new HttpClient { BaseAddress = new Uri(address) };
            foreach (var (path, expectedType, content) in new[] { ("/site.css", "text/css", "var(--brand)"), ("/citation.js", "text/javascript", "Escape") })
            {
                using var response = await client.GetAsync(path);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(expectedType, response.Content.Headers.ContentType?.MediaType);
                Assert.Contains(content, await response.Content.ReadAsStringAsync());
            }
        }
        finally { Directory.SetCurrentDirectory(previous); }
    }

    private static string SourcePath([CallerFilePath] string path = "") => path;
}
