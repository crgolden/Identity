namespace Identity.Pages.Admin.ApiScopes.Edit;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Edits API scope claim types.</summary>
public class ClaimTypesModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="ClaimTypesModel"/> class.</summary>
    public ClaimTypesModel(IConfigurationDbContext context) => _context = context;

    /// <summary>Gets or sets the claim types.</summary>
    [BindProperty]
    public List<ApiScopeClaim> ClaimTypes { get; set; } = [];

    /// <summary>Loads the API scope claim types for editing.</summary>
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var scope = await _context.ApiScopes
            .Include(s => s.UserClaims)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (scope is null)
        {
            return NotFound();
        }

        ClaimTypes = scope.UserClaims;
        return Page();
    }

    /// <summary>Saves claim type changes.</summary>
    public async Task<IActionResult> OnPostAsync(int id)
    {
        var scope = await _context.ApiScopes
            .Include(s => s.UserClaims)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (scope is null)
        {
            return NotFound();
        }

        scope.UserClaims.RemoveAll(c => !ClaimTypes.Any(p => p.Id == c.Id));

        foreach (var posted in ClaimTypes.Where(p => p.Id > 0))
        {
            var existing = scope.UserClaims.FirstOrDefault(c => c.Id == posted.Id);
            if (existing is not null)
            {
                existing.Type = posted.Type;
            }
        }

        scope.UserClaims.AddRange(ClaimTypes
            .Where(p => p.Id == 0)
            .Select(p => new ApiScopeClaim { Type = p.Type, ScopeId = id }));

        await _context.SaveChangesAsync();
        return RedirectToPage("/Admin/ApiScopes/Details/ClaimTypes", new { id });
    }
}
