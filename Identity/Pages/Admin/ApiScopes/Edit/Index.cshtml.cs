namespace Identity.Pages.Admin.ApiScopes.Edit;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class IndexModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    public IndexModel(IConfigurationDbContext context) => _context = context;

    [BindProperty]
    public ApiScope Scope { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var scope = await _context.ApiScopes.FirstOrDefaultAsync(s => s.Id == id);
        if (scope is null)
        {
            return NotFound();
        }

        Scope = scope;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var scope = await _context.ApiScopes.FirstOrDefaultAsync(s => s.Id == id);
        if (scope is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        scope.Name = Scope.Name;
        scope.DisplayName = Scope.DisplayName;
        scope.Description = Scope.Description;
        scope.Enabled = Scope.Enabled;
        scope.ShowInDiscoveryDocument = Scope.ShowInDiscoveryDocument;
        scope.Required = Scope.Required;
        scope.Emphasize = Scope.Emphasize;
        scope.Updated = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return RedirectToPage("/Admin/ApiScopes/Details/Index", new { id });
    }
}