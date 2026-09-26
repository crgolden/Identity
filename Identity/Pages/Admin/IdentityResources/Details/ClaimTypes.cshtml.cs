namespace Identity.Pages.Admin.IdentityResources.Details;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class ClaimTypes : ResourcesBase<IdentityResourceClaim>
{
    private readonly IConfigurationDbContext _context;

    public ClaimTypes(IConfigurationDbContext context) => _context = context;

    public IdentityResource Resource { get; set; } = new();

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
        Resources = resource.UserClaims;
        return Page();
    }
}
