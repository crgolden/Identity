namespace Identity.Pages.Admin.ApiScopes;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class DeleteModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    public DeleteModel(IConfigurationDbContext context) => _context = context;

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

        _context.ApiScopes.Remove(scope);
        await _context.SaveChangesAsync();
        return RedirectToPage("./Index");
    }
}