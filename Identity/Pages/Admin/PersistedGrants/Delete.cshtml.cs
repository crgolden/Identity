namespace Identity.Pages.Admin.PersistedGrants;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Deletes a persisted grant.</summary>
public class DeleteModel : PageModel
{
    private readonly IPersistedGrantDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="DeleteModel"/> class.</summary>
    public DeleteModel(IPersistedGrantDbContext context) => _context = context;

    /// <summary>Gets the persisted grant to delete.</summary>
    public PersistedGrant PersistedGrant { get; private set; } = new();

    /// <summary>Loads the persisted grant for confirmation.</summary>
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

    /// <summary>Deletes the persisted grant.</summary>
    public async Task<IActionResult> OnPostAsync(string key)
    {
        var grant = await _context.PersistedGrants.FirstOrDefaultAsync(g => g.Key == key);
        if (grant is null)
        {
            return NotFound();
        }

        _context.PersistedGrants.Remove(grant);
        await _context.SaveChangesAsync();
        return RedirectToPage("./Index");
    }
}
