namespace Identity.Pages.Admin.Keys.Details;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class IndexModel : PageModel
{
    private readonly IPersistedGrantDbContext _context;

    public IndexModel(IPersistedGrantDbContext context) => _context = context;

    public Key Key { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var key = await _context.Keys.FirstOrDefaultAsync(k => k.Id == id);
        if (key is null)
        {
            return NotFound();
        }

        Key = key;
        return Page();
    }
}