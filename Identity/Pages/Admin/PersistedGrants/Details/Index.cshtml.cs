namespace Identity.Pages.Admin.PersistedGrants.Details;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Shows persisted grant details.</summary>
public class IndexModel : PageModel
{
    private readonly IPersistedGrantDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="IndexModel"/> class.</summary>
    public IndexModel(IPersistedGrantDbContext context) => _context = context;

    /// <summary>Gets the persisted grant.</summary>
    public PersistedGrant PersistedGrant { get; private set; } = new();

    /// <summary>Loads the persisted grant by key.</summary>
    public async Task<IActionResult> OnGetAsync(string key)
    {
        var grant = await _context.PersistedGrants.FirstOrDefaultAsync(g => g.Key == key);
        if (grant is null)
        {
            return NotFound();
        }

        PersistedGrant = grant;
        return Page();
    }
}
