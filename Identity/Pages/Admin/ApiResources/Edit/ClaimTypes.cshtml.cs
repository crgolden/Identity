namespace Identity.Pages.Admin.ApiResources.Edit;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class ClaimTypes : EditableResourcesBase<int, ApiResourceClaim>
{
    internal const string DetailsPageName = "/Admin/ApiResources/Details/ClaimTypes";

    private readonly IConfigurationDbContext _context;

    public ClaimTypes(IConfigurationDbContext context) => _context = context;

    public int ResourceId { get; private set; }

    public string? ResourceName { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var resource = await _context.ApiResources.Include(r => r.UserClaims).FirstOrDefaultAsync(r => r.Id == id);
        if (resource is null)
        {
            return NotFound();
        }

        ResourceId = resource.Id;
        ResourceName = resource.Name;
        Resources = resource.UserClaims;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var resource = await _context.ApiResources.Include(r => r.UserClaims).FirstOrDefaultAsync(r => r.Id == id);
        if (resource is null)
        {
            return NotFound();
        }

        resource.UserClaims.RemoveAll(c => !Resources.Any(p => p.Id == c.Id));

        foreach (var posted in Resources.Where(p => p.Id > 0))
        {
            var existing = resource.UserClaims.FirstOrDefault(c => c.Id == posted.Id);
            if (existing is not null)
            {
                existing.Type = posted.Type;
            }
        }

        resource.UserClaims.AddRange(
            Resources.Where(p => p.Id == 0).Select(p => new ApiResourceClaim { Type = p.Type, ApiResourceId = id }));

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

    protected override ApiResourceClaim NewResource() => new();
}
