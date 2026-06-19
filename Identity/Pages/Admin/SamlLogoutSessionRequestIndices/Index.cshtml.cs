namespace Identity.Pages.Admin.SamlLogoutSessionRequestIndices;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Lists all SAML logout session request indices.</summary>
public class IndexModel : PageModel
{
    private readonly IPersistedGrantDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="IndexModel"/> class.</summary>
    public IndexModel(IPersistedGrantDbContext context) => _context = context;

    /// <summary>Gets the SAML logout session request indices.</summary>
    public IList<SamlLogoutSessionRequestIndex> SamlLogoutSessionRequestIndices { get; private set; } = [];

    /// <summary>Loads all SAML logout session request indices.</summary>
    public async Task OnGetAsync()
    {
        SamlLogoutSessionRequestIndices = await _context.SamlLogoutSessionRequestIndices
            .OrderBy(i => i.Id)
            .ToListAsync();
    }
}
