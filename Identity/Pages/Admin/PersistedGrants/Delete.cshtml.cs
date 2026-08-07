namespace Identity.Pages.Admin.PersistedGrants;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class DeleteModel : PageModel
{
    private readonly IPersistedGrantDbContext _context;

    public DeleteModel(IPersistedGrantDbContext context) => _context = context;

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