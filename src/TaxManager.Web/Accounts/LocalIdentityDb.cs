using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace TaxManager.Web.Accounts;

public sealed class LocalUser : IdentityUser
{
    public required string StoreRoot { get; set; }
}

// Separate synthetic registry; never add Identity tables to a tax database or backup.
public sealed class LocalIdentityDb(DbContextOptions<LocalIdentityDb> options) : IdentityDbContext<LocalUser>(options);

public sealed record AccountMode(bool Enabled);
