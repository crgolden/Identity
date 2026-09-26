namespace Identity.Pages.Admin.IdentityResources.Details;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class Index : PageModel
{
    private readonly IConfigurationDbContext _context;

    public Index(IConfigurationDbContext context) => _context = context;

    public IdentityResource Resource { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var resource = await _context.IdentityResources.FirstOrDefaultAsync(r => r.Id == id);
        if (resource is null)
        {
            return NotFound();
        }

        Resource = resource;
        return Page();
    }
}
