using System.Text.Json;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TaxManager.Data.Forecasting;
using TaxManager.Data.Protection;
using TaxManager.Web.Demo;

namespace TaxManager.Web.Pages;

public sealed class ForecastProfilesModel(SyntheticDatabase database) : PageModel
{
    public sealed record ProfileRow(Guid Id, int Revision, bool IsComplete);
    public IReadOnlyList<ProfileRow> Profiles { get; private set; } = [];

    public async Task OnGetAsync()
    {
        try
        {
            await using var context = database.Open();
            var documents = await new ForecastStore(context).ListAsync();
            Profiles = documents.Select(d => new ProfileRow(d.Id, d.Revision, ForecastModel.HasCompleteInputs(d))).ToArray();
        }
        catch (Exception ex) when (ex is DataProtectionException or JsonException or DbUpdateException or Microsoft.Data.Sqlite.SqliteException)
        {
            ModelState.AddModelError(string.Empty, "Không thể mở hồ sơ: kiểm tra khóa bảo vệ và dữ liệu.");
        }
    }
}
