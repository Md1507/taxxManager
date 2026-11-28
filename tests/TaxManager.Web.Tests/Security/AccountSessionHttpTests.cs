using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TaxManager.Web.Accounts;
using TaxManager.Web.Demo;
using Xunit;

namespace TaxManager.Web.Tests.Security;

public sealed class AccountSessionHttpTests
{
    private const string Password = "synthetic session password 123";
    private const string LoginPath = "/tai-khoan/dang-nhap";

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(true, false)]
    public async Task Session_revoked_or_expired_while_request_waits_rejects_reads_and_writes(bool write, bool revoke)
    {
        var clock = new SessionClock();
        await using var host = await DemoHost.StartAsync(clock: clock, accounts: true);
        var user = await Account(host);
        using var client = host.AccountClient();
        await Login(client);
        var token = Token(await client.GetStringAsync("/Forecast"));
        var options = host.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(IdentityConstants.ApplicationScheme);
        var validate = options.Events.OnValidatePrincipal;
        var authenticated = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        options.Events.OnValidatePrincipal = async context =>
        {
            await validate(context);
            if (context.Principal?.Identity?.IsAuthenticated == true) authenticated.TrySetResult();
        };
        var gate = host.Services.GetRequiredService<LocalAccountStores>().Gate(user.Id);
        await gate.WaitAsync();
        Task<HttpResponseMessage> queued;
        try
        {
            queued = write ? client.PostAsync("/Forecast?handler=Save", ProfileForm(token)) : client.GetAsync("/ho-so");
            await authenticated.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(queued.IsCompleted);
            if (revoke)
            {
                using var scope = host.Services.CreateScope();
                var users = scope.ServiceProvider.GetRequiredService<UserManager<LocalUser>>();
                Assert.True((await users.UpdateSecurityStampAsync((await users.FindByIdAsync(user.Id))!)).Succeeded);
            }
            else clock.Advance(TimeSpan.FromMinutes(31));
        }
        finally { gate.Release(); }
        using var rejected = await queued;
        Assert.Equal(HttpStatusCode.Unauthorized, rejected.StatusCode);
        using var scopeAfter = host.Services.CreateScope();
        var account = await scopeAfter.ServiceProvider.GetRequiredService<UserManager<LocalUser>>().FindByIdAsync(user.Id);
        Assert.NotNull(account);
        await using var db = host.Services.GetRequiredService<LocalAccountStores>().Get(user.Id, user.StoreRoot).Open();
        Assert.Empty(await new TaxManager.Data.Forecasting.ForecastStore(db).ListAsync());
        await Login(client);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/ho-so")).StatusCode);
    }

    [Fact]
    public async Task Active_session_expires_after_thirty_minutes_and_cannot_export_or_delete()
    {
        var clock = new SessionClock();
        await using var host = await DemoHost.StartAsync(clock: clock, accounts: true);
        var user = await Account(host);
        using var client = host.AccountClient();
        await Login(client);
        var token = Token(await client.GetStringAsync("/tai-khoan/xoa"));
        foreach (var elapsed in new[] { 10, 10, 9 })
        {
            clock.Advance(TimeSpan.FromMinutes(elapsed));
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/ho-so")).StatusCode);
        }
        clock.Advance(TimeSpan.FromMinutes(2));
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/ho-so")).StatusCode);
        foreach (var path in new[] { "/tai-khoan/xuat-du-lieu", "/tai-khoan/xoa" })
        {
            using var response = await client.PostAsync(path, Form(token, ("Password", Password), ("ConfirmDeletion", "true")));
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains(LoginPath, response.Headers.Location!.OriginalString);
            Assert.Null(response.Content.Headers.ContentDisposition);
        }
        Assert.True(Directory.Exists(user.StoreRoot));
        await Login(client);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/ho-so")).StatusCode);
    }

    private sealed class SessionClock : TimeProvider
    {
        private DateTimeOffset now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => now;
        public void Advance(TimeSpan elapsed) => now += elapsed;
    }

    private static async Task<LocalUser> Account(DemoHost host)
    {
        using var scope = host.Services.CreateScope();
        using var store = new SyntheticDatabase(Directory.GetCurrentDirectory());
        var user = new LocalUser { UserName = "alice", StoreRoot = store.RootPath };
        Assert.True((await scope.ServiceProvider.GetRequiredService<UserManager<LocalUser>>().CreateAsync(user, Password)).Succeeded);
        Assert.True((await scope.ServiceProvider.GetRequiredService<LocalDataUse>().AcceptAsync(user)).Succeeded);
        return user;
    }

    private static async Task Login(HttpClient client)
    {
        using var response = await client.PostAsync(LoginPath, Form(Token(await client.GetStringAsync(LoginPath)),
            ("Username", "alice"), ("Password", Password)));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    private static string Token(string html) => WebUtility.HtmlDecode(Regex.Match(html,
        "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value);

    private static FormUrlEncodedContent Form(string token, params (string Key, string Value)[] fields) =>
        new(fields.Select(field => new KeyValuePair<string, string>(field.Key, field.Value)).Append(new("__RequestVerificationToken", token)));

    private static FormUrlEncodedContent ProfileForm(string token)
    {
        var fields = new List<(string, string)> { ("FromMonth", "1"), ("ToMonth", "12"), ("Gross", "30000000"), ("Status", "Actual"), ("Source", "Synthetic session test") };
        for (var i = 0; i < 12; i++)
        {
            fields.Add(($"Exceptions[{i}].Month", (i + 1).ToString()));
            foreach (var field in new[] { "Insurance", "Dependents", "Withheld" }) fields.Add(($"Exceptions[{i}].{field}", "0"));
        }
        return Form(token, fields.ToArray());
    }
}
