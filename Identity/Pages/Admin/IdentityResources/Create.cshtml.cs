namespace Identity.Pages.Admin.IdentityResources;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>Creates a new identity resource.</summary>
public class CreateModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="CreateModel"/> class.</summary>
    public CreateModel(IConfigurationDbContext context) => _context = context;

    /// <summary>Gets or sets the identity resource to create.</summary>
    [BindProperty]
    public IdentityResource Resource { get; set; } = new();

    /// <summary>Returns the create page.</summary>
    public IActionResult OnGet() => Page();

    /// <summary>Creates the identity resource.</summary>
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        _context.IdentityResources.Add(Resource);
        await _context.SaveChangesAsync();
        return RedirectToPage("./Details/Index", new { id = Resource.Id });
    }
}
