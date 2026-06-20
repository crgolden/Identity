namespace Identity.Pages.Admin.IdentityResources;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Lists all identity resources.</summary>
public class IndexModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="IndexModel"/> class.</summary>
    public IndexModel(IConfigurationDbContext context) => _context = context;

    /// <summary>Gets the identity resources.</summary>
    public IList<IdentityResource> IdentityResources { get; private set; } = [];

    /// <summary>Loads all identity resources ordered by name.</summary>
    public async Task OnGetAsync()
    {
        IdentityResources = await _context.IdentityResources.OrderBy(r => r.Name).ToListAsync();
    }
}
