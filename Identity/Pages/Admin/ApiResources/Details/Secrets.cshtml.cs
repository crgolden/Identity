namespace Identity.Pages.Admin.ApiResources.Details;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Shows API resource secrets.</summary>
public class SecretsModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="SecretsModel"/> class.</summary>
    public SecretsModel(IConfigurationDbContext context) => _context = context;

    /// <summary>Gets the API resource.</summary>
    public ApiResource Resource { get; private set; } = new();

    /// <summary>Gets the secrets.</summary>
    public IList<ApiResourceSecret> Secrets { get; private set; } = [];

    /// <summary>Loads secrets for the API resource.</summary>
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var resource = await _context.ApiResources.Include(r => r.Secrets).FirstOrDefaultAsync(r => r.Id == id);
        if (resource is null)
        {
            return NotFound();
        }

        Resource = resource;
        Secrets = resource.Secrets;
        return Page();
    }
}
