namespace Identity.Pages.Admin.ApiScopes.Details;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Shows API scope properties.</summary>
public class PropertiesModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="PropertiesModel"/> class.</summary>
    public PropertiesModel(IConfigurationDbContext context) => _context = context;

    /// <summary>Gets or sets the API scope.</summary>
    public ApiScope Scope { get; set; } = new();

    /// <summary>Loads the API scope with properties.</summary>
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var scope = await _context.ApiScopes
            .Include(s => s.Properties)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (scope is null)
        {
            return NotFound();
        }

        Scope = scope;
        return Page();
    }
}
