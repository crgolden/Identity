namespace Identity.Pages.Admin.ApiResources;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Deletes an API resource.</summary>
public class DeleteModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="DeleteModel"/> class.</summary>
    public DeleteModel(IConfigurationDbContext context) => _context = context;

    /// <summary>Gets the API resource to delete.</summary>
    public ApiResource Resource { get; private set; } = new();

    /// <summary>Loads the API resource for deletion confirmation.</summary>
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var resource = await _context.ApiResources.FirstOrDefaultAsync(r => r.Id == id);
        if (resource is null)
        {
            return NotFound();
        }

        Resource = resource;
        return Page();
    }

    /// <summary>Deletes the API resource.</summary>
    public async Task<IActionResult> OnPostAsync(int id)
    {
        var resource = await _context.ApiResources.FirstOrDefaultAsync(r => r.Id == id);
        if (resource is null)
        {
            return NotFound();
        }

        _context.ApiResources.Remove(resource);
        await _context.SaveChangesAsync();
        return RedirectToPage("./Index");
    }
}
