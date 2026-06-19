namespace Identity.Pages.Admin.ApiResources.Details;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Shows API resource scopes.</summary>
public class ScopesModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="ScopesModel"/> class.</summary>
    public ScopesModel(IConfigurationDbContext context) => _context = context;

    /// <summary>Gets the API resource.</summary>
    public ApiResource Resource { get; private set; } = new();

    /// <summary>Gets the scopes.</summary>
    public IList<ApiResourceScope> Scopes { get; private set; } = [];

    /// <summary>Loads scopes for the API resource.</summary>
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var resource = await _context.ApiResources.Include(r => r.Scopes).FirstOrDefaultAsync(r => r.Id == id);
        if (resource is null)
        {
            return NotFound();
        }

        Resource = resource;
        Scopes = resource.Scopes;
        return Page();
    }
}
