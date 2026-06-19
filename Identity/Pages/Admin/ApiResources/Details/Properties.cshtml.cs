namespace Identity.Pages.Admin.ApiResources.Details;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Shows API resource properties.</summary>
public class PropertiesModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="PropertiesModel"/> class.</summary>
    public PropertiesModel(IConfigurationDbContext context) => _context = context;

    /// <summary>Gets the API resource.</summary>
    public ApiResource Resource { get; private set; } = new();

    /// <summary>Gets the properties.</summary>
    public IList<ApiResourceProperty> Properties { get; private set; } = [];

    /// <summary>Loads properties for the API resource.</summary>
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var resource = await _context.ApiResources.Include(r => r.Properties).FirstOrDefaultAsync(r => r.Id == id);
        if (resource is null)
        {
            return NotFound();
        }

        Resource = resource;
        Properties = resource.Properties;
        return Page();
    }
}
