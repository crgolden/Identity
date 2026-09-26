namespace Identity.Pages.Admin.ApiScopes.Edit;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class ClaimTypes : EditableResourcesBase<int, ApiScopeClaim>
{
    internal const string DetailsPageName = "/Admin/ApiScopes/Details/ClaimTypes";

    private readonly IConfigurationDbContext _context;

    public ClaimTypes(IConfigurationDbContext context) => _context = context;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var scope = await _context.ApiScopes
            .Include(s => s.UserClaims)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (scope is null)
        {
            return NotFound();
        }

        Resources = scope.UserClaims;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var scope = await _context.ApiScopes
            .Include(s => s.UserClaims)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (scope is null)
        {
            return NotFound();
        }

        scope.UserClaims.RemoveAll(c => !Resources.Any(p => p.Id == c.Id));

        foreach (var posted in Resources.Where(p => p.Id > 0))
        {
            var existing = scope.UserClaims.FirstOrDefault(c => c.Id == posted.Id);
            if (existing is not null)
            {
                existing.Type = posted.Type;
            }
        }

        scope.UserClaims.AddRange(Resources
            .Where(p => p.Id == 0)
            .Select(p => new ApiScopeClaim { Type = p.Type, ScopeId = id }));

        await _context.SaveChangesAsync();
        return RedirectToPage(DetailsPageName, new { id });
    }

    protected override async Task<bool> LoadContextAsync(int id)
    {
        var exists = await _context.ApiScopes.AnyAsync(s => s.Id == id);
        if (!exists)
        {
            return false;
        }

        return true;
    }

    protected override ApiScopeClaim NewResource() => new();
}
