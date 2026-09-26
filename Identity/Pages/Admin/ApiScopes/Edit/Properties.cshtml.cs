namespace Identity.Pages.Admin.ApiScopes.Edit;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class Properties : EditableResourcesBase<int, ApiScopeProperty>
{
    internal const string DetailsPageName = "/Admin/ApiScopes/Details/Properties";

    private readonly IConfigurationDbContext _context;

    public Properties(IConfigurationDbContext context) => _context = context;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var scope = await _context.ApiScopes
            .Include(s => s.Properties)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (scope is null)
        {
            return NotFound();
        }

        Resources = scope.Properties;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var scope = await _context.ApiScopes
            .Include(s => s.Properties)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (scope is null)
        {
            return NotFound();
        }

        scope.Properties.RemoveAll(p => !Resources.Any(posted => posted.Id == p.Id));

        foreach (var posted in Resources.Where(p => p.Id > 0))
        {
            var existing = scope.Properties.FirstOrDefault(p => p.Id == posted.Id);
            if (existing is not null)
            {
                existing.Key = posted.Key;
                existing.Value = posted.Value;
            }
        }

        scope.Properties.AddRange(Resources
            .Where(p => p.Id == 0)
            .Select(p => new ApiScopeProperty { Key = p.Key, Value = p.Value, ScopeId = id }));

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

    protected override ApiScopeProperty NewResource() => new();
}
