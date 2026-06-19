namespace Identity.Pages.Admin.IdentityResources;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Deletes an identity resource.</summary>
public class DeleteModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="DeleteModel"/> class.</summary>
    public DeleteModel(IConfigurationDbContext context) => _context = context;

    /// <summary>Gets or sets the identity resource to delete.</summary>
    public IdentityResource Resource { get; set; } = new();

    /// <summary>Loads the identity resource for confirmation.</summary>
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

    /// <summary>Deletes the identity resource.</summary>
    public async Task<IActionResult> OnPostAsync(int id)
    {
        var resource = await _context.IdentityResources.FirstOrDefaultAsync(r => r.Id == id);
        if (resource is null)
        {
            return NotFound();
        }

        _context.IdentityResources.Remove(resource);
        await _context.SaveChangesAsync();
        return RedirectToPage("./Index");
    }
}
