namespace Identity.Pages.Admin.SamlServiceProviders;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>Creates a new SAML service provider.</summary>
public class CreateModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="CreateModel"/> class.</summary>
    public CreateModel(IConfigurationDbContext context) => _context = context;

    /// <summary>Gets or sets the SAML service provider to create.</summary>
    [BindProperty]
    public SamlServiceProvider SamlServiceProvider { get; set; } = new();

    /// <summary>Returns the create page.</summary>
    public IActionResult OnGet() => Page();

    /// <summary>Creates the SAML service provider.</summary>
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        _context.SamlServiceProviders.Add(SamlServiceProvider);
        await _context.SaveChangesAsync();
        return RedirectToPage("./Details", new { id = SamlServiceProvider.Id });
    }
}
