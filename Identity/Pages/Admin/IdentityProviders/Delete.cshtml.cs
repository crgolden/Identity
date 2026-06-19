namespace Identity.Pages.Admin.IdentityProviders;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Deletes an identity provider.</summary>
public class DeleteModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="DeleteModel"/> class.</summary>
    public DeleteModel(IConfigurationDbContext context) => _context = context;

    /// <summary>Gets the identity provider to delete.</summary>
    public IdentityProvider IdentityProvider { get; private set; } = new();

    /// <summary>Loads the identity provider for deletion confirmation.</summary>
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

    /// <summary>Deletes the identity provider.</summary>
    public async Task<IActionResult> OnPostAsync(int id)
    {
        var provider = await _context.IdentityProviders.FirstOrDefaultAsync(p => p.Id == id);
        if (provider is null)
        {
            return NotFound();
        }

        _context.IdentityProviders.Remove(provider);
        await _context.SaveChangesAsync();
        return RedirectToPage("./Index");
    }
}
