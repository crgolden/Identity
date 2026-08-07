namespace Identity.Pages.Admin.IdentityResources.Details;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class PropertiesModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    public PropertiesModel(IConfigurationDbContext context) => _context = context;

    public IdentityResource Resource { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var resource = await _context.IdentityResources
            .Include(r => r.Properties)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (resource is null)
        {
            return NotFound();
        }

        Resource = resource;
        return Page();
    }
}