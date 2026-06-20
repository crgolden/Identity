namespace Identity.Pages.Admin.SamlServiceProviders;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Shows details for a SAML service provider.</summary>
public class DetailsModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="DetailsModel"/> class.</summary>
    public DetailsModel(IConfigurationDbContext context) => _context = context;

    /// <summary>Gets the SAML service provider.</summary>
    public SamlServiceProvider SamlServiceProvider { get; private set; } = new();

    /// <summary>Loads the SAML service provider.</summary>
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var sp = await _context.SamlServiceProviders.FirstOrDefaultAsync(s => s.Id == id);
        if (sp is null)
        {
            return NotFound();
        }

        SamlServiceProvider = sp;
        return Page();
    }
}
