using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using TaxManager.Core.Forecasting;
using TaxManager.Data.Forecasting;
using TaxManager.Web.Demo;
using Xunit;

namespace TaxManager.Web.Tests.Forecasting;

public sealed class ForecastDeleteHttpTests
{
    [Theory]
    [InlineData("")]
    [InlineData("&revision=0")]
    public async Task Confirmation_loads_saved_identity_and_revision_without_exposing_uuid(string query)
    {
        await using var host = await DemoHost.StartAsync();
        var store = host.Services.GetRequiredService<SyntheticDatabase>();
        var id = await Seed(store);
        await using (var update = store.Open()) await new ForecastStore(update).UpdateAsync(id, 1, Payload(0));
        using var client = new HttpClient { BaseAddress = host.Address };
        var html = await client.GetStringAsync($"/ForecastDelete?id={id}{query}");
        var visible = WebUtility.HtmlDecode(Regex.Replace(html, "<[^>]+>", " "));
        Assert.Contains("Hồ sơ năm 2026", visible);
        Assert.Contains("Phiên bản 2", visible);
        Assert.Contains("0 đ", visible);
        Assert.DoesNotContain(id.ToString(), visible);
        Assert.Equal("2", Field(html, "Revision"));
        Assert.Equal(id.ToString(), Field(html, "Id"));
        Assert.NotEmpty(Field(html, "__RequestVerificationToken"));
        // Viewing confirmation does not delete, revise, or snapshot the profile.
        await using var read = store.Open();
        Assert.Equal(2, (await new ForecastStore(read).ReadAsync(id))!.Revision);
        Assert.Equal(2, (await new ForecastStore(read).HistoryAsync(id)).Count);
    }

    [Fact]
    public async Task Stale_confirmation_and_zero_revision_post_cannot_delete_a_newer_profile()
    {
        await using var host = await DemoHost.StartAsync();
        var store = host.Services.GetRequiredService<SyntheticDatabase>();
        var id = await Seed(store);
        using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { BaseAddress = host.Address };
        var initial = await client.GetStringAsync($"/ForecastDelete?id={id}&revision=1");
        await using (var update = store.Open()) await new ForecastStore(update).UpdateAsync(id, 1, Payload(40_000_000));
        var staleGet = await client.GetStringAsync($"/ForecastDelete?id={id}&revision=1");
        Assert.DoesNotContain("<form", staleGet);
        Assert.Contains("Mở lại hồ sơ", WebUtility.HtmlDecode(staleGet));
        foreach (var revision in new[] { "1", "0" })
        {
            using var stale = await client.PostAsync("/ForecastDelete", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Id"] = id.ToString(), ["Revision"] = revision,
                ["__RequestVerificationToken"] = Field(initial, "__RequestVerificationToken")
            }));
            Assert.Equal(HttpStatusCode.OK, stale.StatusCode);
            Assert.DoesNotContain("<form", await stale.Content.ReadAsStringAsync());
            await using var read = store.Open();
            Assert.Equal(2, (await new ForecastStore(read).ReadAsync(id))!.Revision);
        }
        var current = await client.GetStringAsync($"/ForecastDelete?id={id}");
        // Opening the cancellation destination preserves the current profile.
        await client.GetAsync($"/Forecast?id={id}");
        await using (var read = store.Open()) Assert.NotNull(await new ForecastStore(read).ReadAsync(id));
        using var noCsrf = await client.PostAsync("/ForecastDelete", new FormUrlEncodedContent(new Dictionary<string, string> { ["Id"] = id.ToString(), ["Revision"] = "2" }));
        Assert.Equal(HttpStatusCode.BadRequest, noCsrf.StatusCode);
        using var deleted = await client.PostAsync("/ForecastDelete", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Id"] = id.ToString(), ["Revision"] = Field(current, "Revision"),
            ["__RequestVerificationToken"] = Field(current, "__RequestVerificationToken")
        }));
        Assert.Equal(HttpStatusCode.Redirect, deleted.StatusCode);
        Assert.Equal("/ho-so", deleted.Headers.Location!.OriginalString);
        await using var final = store.Open();
        Assert.Null(await new ForecastStore(final).ReadAsync(id));
        Assert.Equal(2, (await new ForecastStore(final).HistoryAsync(id)).Count);
    }

    [Fact]
    public async Task Missing_profile_has_no_destructive_form_or_fabricated_revision()
    {
        await using var host = await DemoHost.StartAsync();
        await Seed(host.Services.GetRequiredService<SyntheticDatabase>());
        using var client = new HttpClient { BaseAddress = host.Address };
        using var result = await client.GetAsync($"/ForecastDelete?id={Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        var html = await result.Content.ReadAsStringAsync();
        Assert.DoesNotContain("<form", html);
        Assert.DoesNotContain("Phiên bản 0", WebUtility.HtmlDecode(html));
    }

    private static async Task<Guid> Seed(SyntheticDatabase store)
    {
        await using var context = store.Open();
        return await new ForecastStore(context).CreateAsync(Payload(null));
    }
    private static string Payload(decimal? gross)
    {
        var entered = DateTimeOffset.UtcNow;
        return ForecastDraftReader.Encode(new(2026, Enumerable.Range(1, 12).Select(m => new ForecastMonth(m,
            gross is { } amount ? new(amount, ForecastStatus.Planned, "Synthetic delete", null, entered) : null,
            null, null, null)).ToArray(), []));
    }
    private static string Field(string html, string name) => WebUtility.HtmlDecode(Regex.Match(
        Regex.Match(html, $"<input\\b[^>]*name=\"{name}\"[^>]*>").Value, "value=\"([^\"]*)\"").Groups[1].Value);
}
