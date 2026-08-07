namespace Identity.Pages.Admin.ApiResources.Details;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class ClaimTypesModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    public ClaimTypesModel(IConfigurationDbContext context) => _context = context;

    public ApiResource Resource { get; private set; } = new();

    public IList<ApiResourceClaim> ClaimTypes { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var resource = await _context.ApiResources.Include(r => r.UserClaims).FirstOrDefaultAsync(r => r.Id == id);
        if (resource is null)
        {
            return NotFound();
        }

        Resource = resource;
        ClaimTypes = resource.UserClaims;
        return Page();
    }
}