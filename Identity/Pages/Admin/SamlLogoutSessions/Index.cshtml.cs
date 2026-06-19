namespace Identity.Pages.Admin.SamlLogoutSessions;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Lists all SAML logout sessions.</summary>
public class IndexModel : PageModel
{
    private readonly IPersistedGrantDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="IndexModel"/> class.</summary>
    public IndexModel(IPersistedGrantDbContext context) => _context = context;

    /// <summary>Gets the SAML logout sessions.</summary>
    public IList<SamlLogoutSession> SamlLogoutSessions { get; private set; } = [];

    /// <summary>Loads all SAML logout sessions.</summary>
    public async Task OnGetAsync()
    {
        SamlLogoutSessions = await _context.SamlLogoutSessions
            .OrderByDescending(s => s.ExpiresAtUtc)
            .ToListAsync();
    }
}
