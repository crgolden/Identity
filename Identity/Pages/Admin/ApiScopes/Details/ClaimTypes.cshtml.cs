namespace Identity.Pages.Admin.ApiScopes.Details;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Shows API scope claim types.</summary>
public class ClaimTypesModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="ClaimTypesModel"/> class.</summary>
    public ClaimTypesModel(IConfigurationDbContext context) => _context = context;

    /// <summary>Gets or sets the API scope.</summary>
    public ApiScope Scope { get; set; } = new();

    /// <summary>Loads the API scope with claim types.</summary>
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
        return Page();
    }
}
