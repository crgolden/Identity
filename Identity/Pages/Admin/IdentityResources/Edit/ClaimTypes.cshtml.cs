namespace Identity.Pages.Admin.IdentityResources.Edit;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class ClaimTypes : EditableResourcesBase<int, IdentityResourceClaim>
{
    internal const string DetailsPageName = "/Admin/IdentityResources/Details/ClaimTypes";

    private readonly IConfigurationDbContext _context;

    public ClaimTypes(IConfigurationDbContext context) => _context = context;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var resource = await _context.IdentityResources
            .Include(r => r.UserClaims)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (resource is null)
        {
            return NotFound();
        }

        Resources = resource.UserClaims;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var resource = await _context.IdentityResources
            .Include(r => r.UserClaims)
            .FirstOrDefaultAsync(r => r.Id == id);
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

        resource.UserClaims.AddRange(Resources
            .Where(p => p.Id == 0)
            .Select(p => new IdentityResourceClaim { Type = p.Type, IdentityResourceId = id }));

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

    protected override IdentityResourceClaim NewResource() => new();
}
