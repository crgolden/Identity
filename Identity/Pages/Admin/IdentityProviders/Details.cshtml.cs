namespace Identity.Pages.Admin.IdentityProviders;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Shows details for an identity provider.</summary>
public class DetailsModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="DetailsModel"/> class.</summary>
    public DetailsModel(IConfigurationDbContext context) => _context = context;

    /// <summary>Gets the identity provider.</summary>
    public IdentityProvider IdentityProvider { get; private set; } = new();

    /// <summary>Loads the identity provider.</summary>
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
}
