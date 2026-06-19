namespace Identity.Pages.Admin.ServerSideSessions.Details;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Shows server-side session details.</summary>
public class IndexModel : PageModel
{
    private readonly IPersistedGrantDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="IndexModel"/> class.</summary>
    public IndexModel(IPersistedGrantDbContext context) => _context = context;

    /// <summary>Gets the server-side session.</summary>
    public ServerSideSession ServerSideSession { get; private set; } = new();

    /// <summary>Loads the server-side session by key.</summary>
    public async Task<IActionResult> OnGetAsync(string key)
    {
        var session = await _context.ServerSideSessions.FirstOrDefaultAsync(s => s.Key == key);
        if (session is null)
        {
            return NotFound();
        }

        ServerSideSession = session;
        return Page();
    }
}
