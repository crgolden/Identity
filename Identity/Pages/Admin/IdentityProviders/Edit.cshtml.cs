namespace Identity.Pages.Admin.IdentityProviders;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class EditModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    public EditModel(IConfigurationDbContext context) => _context = context;

    [BindProperty]
    public IdentityProvider IdentityProvider { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var provider = await _context.IdentityProviders.FirstOrDefaultAsync(p => p.Id == id);
        if (provider is null)
        {
            return NotFound();
        }

        IdentityProvider = provider;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var provider = await _context.IdentityProviders.FirstOrDefaultAsync(p => p.Id == id);
        if (provider is null)
        {
            return NotFound();
        }

        provider.Scheme = IdentityProvider.Scheme;
        provider.DisplayName = IdentityProvider.DisplayName;
        provider.Enabled = IdentityProvider.Enabled;
        provider.Type = IdentityProvider.Type;
        provider.Properties = IdentityProvider.Properties;
        provider.Updated = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return RedirectToPage("./Details", new { id });
    }
}