namespace Identity.Pages.Admin.SamlLogoutSessions.Details;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Shows SAML logout session details.</summary>
public class IndexModel : PageModel
{
    private readonly IPersistedGrantDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="IndexModel"/> class.</summary>
    public IndexModel(IPersistedGrantDbContext context) => _context = context;

    /// <summary>Gets the SAML logout session.</summary>
    public SamlLogoutSession SamlLogoutSession { get; private set; } = new();

    /// <summary>Loads the SAML logout session by id.</summary>
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
