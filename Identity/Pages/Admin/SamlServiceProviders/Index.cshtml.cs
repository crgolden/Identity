namespace Identity.Pages.Admin.SamlServiceProviders;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Lists all SAML service providers.</summary>
public class IndexModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="IndexModel"/> class.</summary>
    public IndexModel(IConfigurationDbContext context) => _context = context;

    /// <summary>Gets the SAML service providers.</summary>
    public IList<SamlServiceProvider> SamlServiceProviders { get; private set; } = [];

    /// <summary>Loads all SAML service providers.</summary>
    public async Task OnGetAsync()
    {
        SamlServiceProviders = await _context.SamlServiceProviders
            .OrderBy(s => s.EntityId)
            .ToListAsync();
    }
}
