namespace Identity.Pages.Admin.IdentityResources.Edit;

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
    public List<IdentityResourceClaim> ClaimTypes { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var resource = await _context.IdentityResources
            .Include(r => r.UserClaims)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (resource is null)
        {
            return NotFound();
        }

        ClaimTypes = resource.UserClaims;
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

        resource.UserClaims.RemoveAll(c => !ClaimTypes.Any(p => p.Id == c.Id));

        foreach (var posted in ClaimTypes.Where(p => p.Id > 0))
        {
            var existing = resource.UserClaims.FirstOrDefault(c => c.Id == posted.Id);
            if (existing is not null)
            {
                existing.Type = posted.Type;
            }
        }

        resource.UserClaims.AddRange(ClaimTypes
            .Where(p => p.Id == 0)
            .Select(p => new IdentityResourceClaim { Type = p.Type, IdentityResourceId = id }));

        await _context.SaveChangesAsync();
        return RedirectToPage("/Admin/IdentityResources/Details/ClaimTypes", new { id });
    }

    public async Task<IActionResult> OnPostAddRowAsync(int id)
    {
        var exists = await _context.IdentityResources.AnyAsync(r => r.Id == id);
        if (!exists)
        {
            return NotFound();
        }

        ClaimTypes.Add(new IdentityResourceClaim());
        return Page();
    }

    public async Task<IActionResult> OnPostRemoveRowAsync(int id, int index)
    {
        var exists = await _context.IdentityResources.AnyAsync(r => r.Id == id);
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