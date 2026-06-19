namespace Identity.Pages.Admin.ApiScopes;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Lists all API scopes.</summary>
public class IndexModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="IndexModel"/> class.</summary>
    public IndexModel(IConfigurationDbContext context) => _context = context;

    /// <summary>Gets the API scopes.</summary>
    public IList<ApiScope> ApiScopes { get; private set; } = [];

    /// <summary>Loads all API scopes ordered by name.</summary>
    public async Task OnGetAsync()
    {
        ApiScopes = await _context.ApiScopes.OrderBy(s => s.Name).ToListAsync();
    }
}
