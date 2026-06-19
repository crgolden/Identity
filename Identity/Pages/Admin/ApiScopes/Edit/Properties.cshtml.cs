namespace Identity.Pages.Admin.ApiScopes.Edit;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Edits API scope properties.</summary>
public class PropertiesModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="PropertiesModel"/> class.</summary>
    public PropertiesModel(IConfigurationDbContext context) => _context = context;

    /// <summary>Gets or sets the properties.</summary>
    [BindProperty]
    public List<ApiScopeProperty> Properties { get; set; } = [];

    /// <summary>Loads the API scope properties for editing.</summary>
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var scope = await _context.ApiScopes
            .Include(s => s.Properties)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (scope is null)
        {
            return NotFound();
        }

        Properties = scope.Properties;
        return Page();
    }

    /// <summary>Saves property changes.</summary>
    public async Task<IActionResult> OnPostAsync(int id)
    {
        var scope = await _context.ApiScopes
            .Include(s => s.Properties)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (scope is null)
        {
            return NotFound();
        }

        scope.Properties.RemoveAll(p => !Properties.Any(posted => posted.Id == p.Id));

        foreach (var posted in Properties.Where(p => p.Id > 0))
        {
            var existing = scope.Properties.FirstOrDefault(p => p.Id == posted.Id);
            if (existing is not null)
            {
                existing.Key = posted.Key;
                existing.Value = posted.Value;
            }
        }

        scope.Properties.AddRange(Properties
            .Where(p => p.Id == 0)
            .Select(p => new ApiScopeProperty { Key = p.Key, Value = p.Value, ScopeId = id }));

        await _context.SaveChangesAsync();
        return RedirectToPage("/Admin/ApiScopes/Details/Properties", new { id });
    }
}
