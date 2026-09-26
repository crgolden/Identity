namespace Identity.Pages.Admin.ApiResources.Details;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class Secrets : ResourcesBase<ApiResourceSecret>
{
    private readonly IConfigurationDbContext _context;

    public Secrets(IConfigurationDbContext context) => _context = context;

    public ApiResource Resource { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var resource = await _context.ApiResources.Include(r => r.Secrets).FirstOrDefaultAsync(r => r.Id == id);
        if (resource is null)
        {
            return NotFound();
        }

        Resource = resource;
        Resources = resource.Secrets;
        return Page();
    }
}
