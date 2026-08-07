namespace Identity.Pages.Admin.SamlLogoutSessions.Details;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class IndexModel : PageModel
{
    private readonly IPersistedGrantDbContext _context;

    public IndexModel(IPersistedGrantDbContext context) => _context = context;

    public SamlLogoutSession SamlLogoutSession { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(long id)
    {
        var session = await _context.SamlLogoutSessions.FirstOrDefaultAsync(s => s.Id == id);
        if (session is null)
        {
            return NotFound();
        }

        SamlLogoutSession = session;
        return Page();
    }
}