namespace Identity.Pages.Admin.ApiScopes.Edit;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class ClaimTypesModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    public ClaimTypesModel(IConfigurationDbContext context) => _context = context;

    [BindProperty]
    public List<ApiScopeClaim> ClaimTypes { get; set; } = [];

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

    public async Task<IActionResult> OnPostAddRowAsync(int id)
    {
        var exists = await _context.ApiScopes.AnyAsync(s => s.Id == id);
        if (!exists)
        {
            return NotFound();
        }

        ClaimTypes.Add(new ApiScopeClaim());
        return Page();
    }

    public async Task<IActionResult> OnPostRemoveRowAsync(int id, int index)
    {
        var exists = await _context.ApiScopes.AnyAsync(s => s.Id == id);
        if (!exists)
        {
            return NotFound();
        }

        if (index >= 0 && index < ClaimTypes.Count)
        {
            ClaimTypes.RemoveAt(index);
        }

        return Page();
    }
}