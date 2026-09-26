namespace Identity.Pages.Admin.IdentityResources.Edit;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class Properties : EditableResourcesBase<int, IdentityResourceProperty>
{
    internal const string DetailsPageName = "/Admin/IdentityResources/Details/Properties";

    private readonly IConfigurationDbContext _context;

    public Properties(IConfigurationDbContext context) => _context = context;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var resource = await _context.IdentityResources
            .Include(r => r.Properties)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (resource is null)
        {
            return NotFound();
        }

        Resources = resource.Properties;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var resource = await _context.IdentityResources
            .Include(r => r.Properties)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (resource is null)
        {
            return NotFound();
        }

        resource.Properties.RemoveAll(p => !Resources.Any(posted => posted.Id == p.Id));

        foreach (var posted in Resources.Where(p => p.Id > 0))
        {
            var existing = resource.Properties.FirstOrDefault(p => p.Id == posted.Id);
            if (existing is not null)
            {
                existing.Key = posted.Key;
                existing.Value = posted.Value;
            }
        }

        resource.Properties.AddRange(Resources
            .Where(p => p.Id == 0)
            .Select(p => new IdentityResourceProperty { Key = p.Key, Value = p.Value, IdentityResourceId = id }));

        await _context.SaveChangesAsync();
        return RedirectToPage(DetailsPageName, new { id });
    }

    protected override async Task<bool> LoadContextAsync(int id)
    {
        var exists = await _context.IdentityResources.AnyAsync(r => r.Id == id);
        if (!exists)
        {
            return false;
        }

        return true;
    }

    protected override IdentityResourceProperty NewResource() => new();
}
