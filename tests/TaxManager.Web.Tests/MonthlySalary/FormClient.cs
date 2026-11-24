using System.Net;
using System.Text.RegularExpressions;

namespace TaxManager.Web.Tests.MonthlySalary;

public sealed class FormClient : IDisposable
{
    private readonly HttpClient client;
    public FormClient(Uri address) => client = new HttpClient(new HttpClientHandler { UseCookies = true }) { BaseAddress = address };

    public async Task<string> PostAsync(string gross = "30000000", string insurance = "0", int count = 12,
        string year = "2025", string? extraKey = null, int? badMonth = null, string? badInsurance = null)
        => WebUtility.HtmlDecode(await PostRawAsync(gross, insurance, count, year, extraKey, badMonth, badInsurance));

    public async Task<string> PostRawAsync(string gross = "30000000", string insurance = "0", int count = 12,
        string year = "2025", string? extraKey = null, int? badMonth = null, string? badInsurance = null)
    {
        var page = await client.GetStringAsync("/");
        var token = Regex.Match(page, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
        if (token.Length == 0) throw new InvalidOperationException("Missing antiforgery token");
        var form = new List<KeyValuePair<string, string>> { new("__RequestVerificationToken", token), new("year", year) };
        for (var i = 1; i <= count; i++)
        {
            form.Add(new("gross", gross));
            form.Add(new("insurance", i == badMonth ? badInsurance! : insurance));
            form.Add(new("withheld", "0"));
        }
        if (extraKey is not null) form.Add(new(extraKey, "1"));
        using var response = await client.PostAsync("/", new FormUrlEncodedContent(form));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
    public void Dispose() => client.Dispose();
}
