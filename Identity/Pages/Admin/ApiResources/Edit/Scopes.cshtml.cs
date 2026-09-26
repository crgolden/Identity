namespace Identity.Pages.Admin.ApiResources.Edit;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class Scopes : EditableResourcesBase<int, ApiResourceScope>
{
    internal const string DetailsPageName = "/Admin/ApiResources/Details/Scopes";

    private readonly IConfigurationDbContext _context;

    public Scopes(IConfigurationDbContext context) => _context = context;

    public int ResourceId { get; private set; }

    public string? ResourceName { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var resource = await _context.ApiResources.Include(r => r.Scopes).FirstOrDefaultAsync(r => r.Id == id);
        if (resource is null)
        {
            return NotFound();
        }

        ResourceId = resource.Id;
        ResourceName = resource.Name;
        Resources = resource.Scopes;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var resource = await _context.ApiResources.Include(r => r.Scopes).FirstOrDefaultAsync(r => r.Id == id);
        if (resource is null)
        {
            return NotFound();
        }

        resource.Scopes.RemoveAll(s => !Resources.Any(p => p.Id == s.Id));

        foreach (var posted in Resources.Where(p => p.Id > 0))
        {
            var existing = resource.Scopes.FirstOrDefault(s => s.Id == posted.Id);
            if (existing is not null)
            {
                existing.Scope = posted.Scope;
            }
        }

        resource.Scopes.AddRange(
            Resources.Where(p => p.Id == 0).Select(p => new ApiResourceScope { Scope = p.Scope, ApiResourceId = id }));

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

    protected override ApiResourceScope NewResource() => new();
}
