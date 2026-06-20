namespace Identity.Pages.Admin.ApiResources.Edit;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Edits properties for an API resource.</summary>
public class PropertiesModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="PropertiesModel"/> class.</summary>
    public PropertiesModel(IConfigurationDbContext context) => _context = context;

    /// <summary>Gets the API resource id.</summary>
    public int ResourceId { get; private set; }

    /// <summary>Gets the API resource name.</summary>
    public string ResourceName { get; private set; } = string.Empty;

    /// <summary>Gets or sets the properties.</summary>
    [BindProperty]
    public List<ApiResourceProperty> Properties { get; set; } = [];

    /// <summary>Loads properties for editing.</summary>
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var resource = await _context.ApiResources.Include(r => r.Properties).FirstOrDefaultAsync(r => r.Id == id);
        if (resource is null)
        {
            return NotFound();
        }

        ResourceId = resource.Id;
        ResourceName = resource.Name;
        Properties = resource.Properties;
        return Page();
    }

    /// <summary>Saves property changes.</summary>
    public async Task<IActionResult> OnPostAsync(int id)
    {
        var resource = await _context.ApiResources.Include(r => r.Properties).FirstOrDefaultAsync(r => r.Id == id);
        if (resource is null)
        {
            return NotFound();
        }

        resource.Properties.RemoveAll(p => !Properties.Any(posted => posted.Id == p.Id));

        foreach (var posted in Properties.Where(p => p.Id > 0))
        {
            var existing = resource.Properties.FirstOrDefault(p => p.Id == posted.Id);
            if (existing is not null)
            {
                existing.Key = posted.Key;
                existing.Value = posted.Value;
            }
        }

        resource.Properties.AddRange(
            Properties.Where(p => p.Id == 0).Select(p => new ApiResourceProperty { Key = p.Key, Value = p.Value, ApiResourceId = id }));

        await _context.SaveChangesAsync();
        return RedirectToPage("/Admin/ApiResources/Details/Properties", new { id });
    }
}
