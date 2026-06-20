namespace Identity.Pages.Admin.IdentityProviders;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Lists all identity providers.</summary>
public class IndexModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="IndexModel"/> class.</summary>
    public IndexModel(IConfigurationDbContext context) => _context = context;

    /// <summary>Gets the identity providers.</summary>
    public IList<IdentityProvider> IdentityProviders { get; private set; } = [];

    /// <summary>Loads all identity providers.</summary>
    public async Task OnGetAsync()
    {
        IdentityProviders = await _context.IdentityProviders
            .OrderBy(p => p.Scheme)
            .ToListAsync();
    }
}
