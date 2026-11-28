using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TaxManager.Data.Forecasting;
using TaxManager.Web.Demo;

namespace TaxManager.Web.Pages;

public sealed class ForecastAnnualHistoryModel(TaxManager.Web.Backup.SelectedDatabase database) : PageModel
{
    public Guid Id { get; private set; }
    public bool ProfileExists { get; private set; }
    public AnnualCalculationSnapshot? Selected { get; private set; }
    public IReadOnlyList<AnnualCalculationSnapshot> History { get; private set; } = [];
    public async Task<IActionResult> OnGetAsync(Guid id, Guid? record)
    {
        Id = id;
        await using var context = database.Open();
        var service = new AnnualForecastService(context);
        History = await service.HistoryAsync(id);
        ProfileExists = await new ForecastStore(context).ReadAsync(id) is not null;
        if (record is not null)
        {
            Selected = await service.ReadAsync(id, record.Value);
            if (Selected is null) return NotFound();
        }
        return Page();
    }
}
