namespace Identity.Pages.Admin.IdentityProviders;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>Creates a new identity provider.</summary>
public class CreateModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="CreateModel"/> class.</summary>
    public CreateModel(IConfigurationDbContext context) => _context = context;

    /// <summary>Gets or sets the identity provider to create.</summary>
    [BindProperty]
    public IdentityProvider IdentityProvider { get; set; } = new();

    /// <summary>Returns the create page.</summary>
    public IActionResult OnGet() => Page();

    /// <summary>Creates the identity provider.</summary>
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        _context.IdentityProviders.Add(IdentityProvider);
        await _context.SaveChangesAsync();
        return RedirectToPage("./Details", new { id = IdentityProvider.Id });
    }
}
