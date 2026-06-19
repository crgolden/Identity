namespace Identity.Pages.Admin.Keys.Details;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Shows key details.</summary>
public class IndexModel : PageModel
{
    private readonly IPersistedGrantDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="IndexModel"/> class.</summary>
    public IndexModel(IPersistedGrantDbContext context) => _context = context;

    /// <summary>Gets the key.</summary>
    public Key Key { get; private set; } = new();

    /// <summary>Loads the key by id.</summary>
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
