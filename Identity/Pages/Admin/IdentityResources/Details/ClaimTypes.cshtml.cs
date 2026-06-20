namespace Identity.Pages.Admin.IdentityResources.Details;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Shows identity resource claim types.</summary>
public class ClaimTypesModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="ClaimTypesModel"/> class.</summary>
    public ClaimTypesModel(IConfigurationDbContext context) => _context = context;

    /// <summary>Gets or sets the identity resource.</summary>
    public IdentityResource Resource { get; set; } = new();

    /// <summary>Loads the identity resource with claim types.</summary>
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var resource = await _context.IdentityResources
            .Include(r => r.UserClaims)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (resource is null)
        {
            return NotFound();
        }

        Resource = resource;
        return Page();
    }
}
