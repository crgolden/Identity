namespace Identity.Pages.Admin.IdentityProviders;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Edits an identity provider.</summary>
public class EditModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="EditModel"/> class.</summary>
    public EditModel(IConfigurationDbContext context) => _context = context;

    /// <summary>Gets or sets the identity provider being edited.</summary>
    [BindProperty]
    public IdentityProvider IdentityProvider { get; set; } = new();

    /// <summary>Loads the identity provider for editing.</summary>
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

    /// <summary>Saves the edited identity provider.</summary>
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
