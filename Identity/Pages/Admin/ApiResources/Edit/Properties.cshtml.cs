namespace Identity.Pages.Admin.ApiResources.Edit;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class PropertiesModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    public PropertiesModel(IConfigurationDbContext context) => _context = context;

    public int ResourceId { get; private set; }

    public string ResourceName { get; private set; } = Empty;

    [BindProperty]
    public List<ApiResourceProperty> Properties { get; set; } = [];

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

    public async Task<IActionResult> OnPostAddRowAsync(int id)
    {
        var resource = await _context.ApiResources.FirstOrDefaultAsync(r => r.Id == id);
        if (resource is null)
        {
            return NotFound();
        }

        ResourceId = resource.Id;
        ResourceName = resource.Name;
        Properties.Add(new ApiResourceProperty());
        return Page();
    }

    public async Task<IActionResult> OnPostRemoveRowAsync(int id, int index)
    {
        var resource = await _context.ApiResources.FirstOrDefaultAsync(r => r.Id == id);
        if (resource is null)
        {
            return NotFound();
        }

        ResourceId = resource.Id;
        ResourceName = resource.Name;
        if (index >= 0 && index < Properties.Count)
        {
            Properties.RemoveAt(index);
        }

        return Page();
    }
}