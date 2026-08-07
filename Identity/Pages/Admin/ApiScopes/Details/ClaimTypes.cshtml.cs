namespace Identity.Pages.Admin.ApiScopes.Details;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class ClaimTypesModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    public ClaimTypesModel(IConfigurationDbContext context) => _context = context;

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
        return Page();
    }
}