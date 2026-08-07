namespace Identity.Pages.Admin.IdentityResources.Edit;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class IndexModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    public IndexModel(IConfigurationDbContext context) => _context = context;

    [BindProperty]
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

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var resource = await _context.IdentityResources.FirstOrDefaultAsync(r => r.Id == id);
        if (resource is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        resource.Name = Resource.Name;
        resource.DisplayName = Resource.DisplayName;
        resource.Description = Resource.Description;
        resource.Enabled = Resource.Enabled;
        resource.ShowInDiscoveryDocument = Resource.ShowInDiscoveryDocument;
        resource.Required = Resource.Required;
        resource.Emphasize = Resource.Emphasize;
        resource.Updated = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return RedirectToPage("/Admin/IdentityResources/Details/Index", new { id });
    }
}