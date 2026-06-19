namespace Identity.Pages.Admin.ApiResources;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Lists all API resources.</summary>
public class IndexModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="IndexModel"/> class.</summary>
    public IndexModel(IConfigurationDbContext context) => _context = context;

    /// <summary>Gets the API resources.</summary>
    public IList<ApiResource> ApiResources { get; private set; } = [];

    /// <summary>Loads all API resources ordered by name.</summary>
    public async Task OnGetAsync()
    {
        ApiResources = await _context.ApiResources.OrderBy(r => r.Name).ToListAsync();
    }
}
