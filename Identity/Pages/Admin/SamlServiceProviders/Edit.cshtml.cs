namespace Identity.Pages.Admin.SamlServiceProviders;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Edits a SAML service provider.</summary>
public class EditModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="EditModel"/> class.</summary>
    public EditModel(IConfigurationDbContext context) => _context = context;

    /// <summary>Gets or sets the SAML service provider being edited.</summary>
    [BindProperty]
    public SamlServiceProvider SamlServiceProvider { get; set; } = new();

    /// <summary>Loads the SAML service provider for editing.</summary>
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

    /// <summary>Saves the edited SAML service provider.</summary>
    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var sp = await _context.SamlServiceProviders.FirstOrDefaultAsync(s => s.Id == id);
        if (sp is null)
        {
            return NotFound();
        }

        sp.EntityId = SamlServiceProvider.EntityId;
        sp.DisplayName = SamlServiceProvider.DisplayName;
        sp.Description = SamlServiceProvider.Description;
        sp.Enabled = SamlServiceProvider.Enabled;
        sp.AllowIdpInitiated = SamlServiceProvider.AllowIdpInitiated;
        sp.DefaultNameIdFormat = SamlServiceProvider.DefaultNameIdFormat;
        sp.Updated = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return RedirectToPage("./Details", new { id });
    }
}
