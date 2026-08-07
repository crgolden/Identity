namespace Identity.Pages.Admin.ApiResources.Details;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class SecretsModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    public SecretsModel(IConfigurationDbContext context) => _context = context;

    public ApiResource Resource { get; private set; } = new();

    public IList<ApiResourceSecret> Secrets { get; private set; } = [];

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