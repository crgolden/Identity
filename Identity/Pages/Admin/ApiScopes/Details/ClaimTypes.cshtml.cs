namespace Identity.Pages.Admin.ApiScopes.Details;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class ClaimTypes : ResourcesBase<ApiScopeClaim>
{
    private readonly IConfigurationDbContext _context;

    public ClaimTypes(IConfigurationDbContext context) => _context = context;

    public ApiScope Scope { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var scope = await _context.ApiScopes
            .Include(s => s.UserClaims)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (scope is null)
        {
            return NotFound();
        }

        Scope = scope;
        Resources = scope.UserClaims;
        return Page();
    }
}
