namespace Identity.Pages.Admin.ApiScopes.Details;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class Properties : ResourcesBase<ApiScopeProperty>
{
    private readonly IConfigurationDbContext _context;

    public Properties(IConfigurationDbContext context) => _context = context;

    public ApiScope Scope { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var scope = await _context.ApiScopes
            .Include(s => s.Properties)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (scope is null)
        {
            return NotFound();
        }

        Scope = scope;
        Resources = scope.Properties;
        return Page();
    }
}
