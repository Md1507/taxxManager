using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaxManager.Core;
using TaxManager.Web.Demo;
using Xunit;

namespace TaxManager.Web.Tests.MonthlySalary;

public sealed class CalculationBehaviorTests
{
    [Theory]
    [InlineData(2025)]
    [InlineData(2026)]
    public async Task Every_month_renders_all_core_steps_and_bracket_portions(int year)
    {
        await using var host = await DemoHost.StartAsync();
        using var form = new FormClient(host.Address);
        var html = await form.PostAsync(gross: "30000000", insurance: "3150000", year: year.ToString(CultureInfo.InvariantCulture));
        var result = MonthlySalaryCalculator.Calculate(30000000m, 3150000m, 0,
            year == 2025 ? TaxRuleSet.Vietnam2025 : TaxRuleSet.Vietnam2026);
        var expectedSteps = new[] { result.GrossIncome.Value, result.Insurance.Value, result.PersonalDeduction.Value,
            result.DependentDeduction.Value, result.TaxableIncomeBeforeClamp.Value, result.Pit.TaxableIncome.Value, result.Pit.TotalTax.Value };
        var months = Regex.Split(html, @"<details><summary>Tháng \d+, Thuế ").Skip(1).ToArray();
        Assert.Equal(12, months.Length);
        foreach (var month in months)
        {
            var stepsTable = month[..month.IndexOf("</table>", StringComparison.Ordinal)];
            var steps = Regex.Matches(stepsTable, "<tr><th scope=\"row\">.*?</th><td class=\"money\">(.*?)</td>")
                .Select(m => WebUtility.HtmlDecode(m.Groups[1].Value)).ToArray();
            Assert.Equal(expectedSteps.Select(Money), steps);
            var bracketTable = month[(month.IndexOf("<table class=\"brackets\">", StringComparison.Ordinal))..];
            var rows = Regex.Matches(bracketTable, "<tr><td data-label=\"Bậc\">(.*?)</td><td data-label=\"Thu nhập chịu thuế\">(.*?)</td><td data-label=\"Thuế\">(.*?)</td>");
            Assert.Equal(result.Pit.Portions.Count, rows.Count);
            for (var i = 0; i < rows.Count; i++)
            {
                var portion = result.Pit.Portions[i];
                Assert.Equal(Money(portion.TaxableAmount), rows[i].Groups[2].Value);
                Assert.Equal(Money(portion.Tax), rows[i].Groups[3].Value);
                Assert.Contains(Money(portion.Rate * 100), rows[i].Groups[1].Value);
            }
        }
    }

    [Fact]
    public async Task Overflow_in_month_seven_rolls_back_entire_batch()
    {
        await using var host = await DemoHost.StartAsync();
        using var form = new FormClient(host.Address);
        var html = await form.PostAsync(gross: "0", badMonth: 7, badInsurance: decimal.MaxValue.ToString(CultureInfo.InvariantCulture));
        Assert.Contains("Không hoàn tất tính đủ 12 tháng", html);
        Assert.DoesNotContain("<h2>Kết quả 12 tháng</h2>", html);
        var database = host.Services.GetRequiredService<SyntheticDatabase>();
        await using var context = database.Open();
        Assert.Equal(0, await context.UserProfiles.CountAsync());
        Assert.Equal(0, await context.MonthlyIncomes.CountAsync());
        Assert.Equal(0, await context.CalculationHistory.CountAsync());
    }

    private static string Money(decimal value) => value.ToString("#,0.############################", CultureInfo.GetCultureInfo("vi-VN"));
}
