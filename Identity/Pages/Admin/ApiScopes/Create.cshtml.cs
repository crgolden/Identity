namespace Identity.Pages.Admin.ApiScopes;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>Creates a new API scope.</summary>
public class CreateModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="CreateModel"/> class.</summary>
    public CreateModel(IConfigurationDbContext context) => _context = context;

    /// <summary>Gets or sets the API scope to create.</summary>
    [BindProperty]
    public ApiScope Scope { get; set; } = new();

    /// <summary>Returns the create page.</summary>
    public IActionResult OnGet() => Page();

    /// <summary>Creates the API scope.</summary>
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        _context.ApiScopes.Add(Scope);
        await _context.SaveChangesAsync();
        return RedirectToPage("./Details/Index", new { id = Scope.Id });
    }
}
