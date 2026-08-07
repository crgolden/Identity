namespace Identity.Pages.Admin.PersistedGrants.Details;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class IndexModel : PageModel
{
    private readonly IPersistedGrantDbContext _context;

    public IndexModel(IPersistedGrantDbContext context) => _context = context;

    public PersistedGrant PersistedGrant { get; private set; } = new();

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