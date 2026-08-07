namespace Identity.Pages.Admin.ServerSideSessions.Details;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class IndexModel : PageModel
{
    private readonly IPersistedGrantDbContext _context;

    public IndexModel(IPersistedGrantDbContext context) => _context = context;

    public ServerSideSession ServerSideSession { get; private set; } = new();

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