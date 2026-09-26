namespace Identity.Pages.Admin.ApiResources.Edit;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class Properties : EditableResourcesBase<int, ApiResourceProperty>
{
    internal const string DetailsPageName = "/Admin/ApiResources/Details/Properties";

    private readonly IConfigurationDbContext _context;

    public Properties(IConfigurationDbContext context) => _context = context;

    public int ResourceId { get; private set; }

    public string? ResourceName { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var resource = await _context.ApiResources.Include(r => r.Properties).FirstOrDefaultAsync(r => r.Id == id);
        if (resource is null)
        {
            return NotFound();
        }

        ResourceId = resource.Id;
        ResourceName = resource.Name;
        Resources = resource.Properties;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var resource = await _context.ApiResources.Include(r => r.Properties).FirstOrDefaultAsync(r => r.Id == id);
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

        resource.Properties.AddRange(
            Resources.Where(p => p.Id == 0).Select(p => new ApiResourceProperty { Key = p.Key, Value = p.Value, ApiResourceId = id }));

        await _context.SaveChangesAsync();
        return RedirectToPage(DetailsPageName, new { id });
    }

    protected override async Task<bool> LoadContextAsync(int id)
    {
        var resource = await _context.ApiResources.FirstOrDefaultAsync(r => r.Id == id);
        if (resource is null)
        {
            return false;
        }

        ResourceId = resource.Id;
        ResourceName = resource.Name;
        return true;
    }

    protected override ApiResourceProperty NewResource() => new();
}
