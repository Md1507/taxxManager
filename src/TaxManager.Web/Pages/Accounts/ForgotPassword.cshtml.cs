using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TaxManager.Web.Accounts;

namespace TaxManager.Web.Pages.Accounts;

[AllowAnonymous, EnableRateLimiting("account-entry")]
public sealed class ForgotPasswordModel(EmailRecoverySettings settings, EmailRecovery recovery) : PageModel
{
    [BindProperty, Required(ErrorMessage = "Nhập email đã xác minh."), EmailAddress(ErrorMessage = "Nhập một địa chỉ email hợp lệ."), StringLength(254)]
    public string? Email { get; set; }
    public bool Sent { get; private set; }
    public IActionResult OnGet() => !settings.Enabled ? NotFound() : User.Identity?.IsAuthenticated == true ? RedirectToPage("/Accounts/Index") : Page();
    public async Task<IActionResult> OnPostAsync(CancellationToken token)
    {
        if (!settings.Enabled) return NotFound();
        if (User.Identity?.IsAuthenticated == true) return RedirectToPage("/Accounts/Index");
        if (!ModelState.IsValid) return Page();
        try { await recovery.RequestResetAsync(Email!, token); }
        catch (Exception ex) when (ex is DbException or DbUpdateException) { /* Keep the same public response. */ }
        Email = null;
        ModelState.Clear();
        Sent = true;
        return Page();
    }
}
