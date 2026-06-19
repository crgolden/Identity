namespace Identity.Pages.Admin.ApiResources.Edit;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Edits scopes for an API resource.</summary>
public class ScopesModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="ScopesModel"/> class.</summary>
    public ScopesModel(IConfigurationDbContext context) => _context = context;

    /// <summary>Gets the API resource id.</summary>
    public int ResourceId { get; private set; }

    /// <summary>Gets the API resource name.</summary>
    public string ResourceName { get; private set; } = string.Empty;

    /// <summary>Gets or sets the scopes.</summary>
    [BindProperty]
    public List<ApiResourceScope> Scopes { get; set; } = [];

    /// <summary>Loads scopes for editing.</summary>
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var resource = await _context.ApiResources.Include(r => r.Scopes).FirstOrDefaultAsync(r => r.Id == id);
        if (resource is null)
        {
            return NotFound();
        }

        ResourceId = resource.Id;
        ResourceName = resource.Name;
        Scopes = resource.Scopes;
        return Page();
    }

    /// <summary>Saves scope changes.</summary>
    public async Task<IActionResult> OnPostAsync(int id)
    {
        var resource = await _context.ApiResources.Include(r => r.Scopes).FirstOrDefaultAsync(r => r.Id == id);
        if (resource is null)
        {
            return NotFound();
        }

        resource.Scopes.RemoveAll(s => !Scopes.Any(p => p.Id == s.Id));

        foreach (var posted in Scopes.Where(p => p.Id > 0))
        {
            var existing = resource.Scopes.FirstOrDefault(s => s.Id == posted.Id);
            if (existing is not null)
            {
                existing.Scope = posted.Scope;
            }
        }

        resource.Scopes.AddRange(
            Scopes.Where(p => p.Id == 0).Select(p => new ApiResourceScope { Scope = p.Scope, ApiResourceId = id }));

        await _context.SaveChangesAsync();
        return RedirectToPage("/Admin/ApiResources/Details/Scopes", new { id });
    }
}
