namespace Identity.Pages.Admin.IdentityProviders;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class DetailsModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    public DetailsModel(IConfigurationDbContext context) => _context = context;

    public IdentityProvider IdentityProvider { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var provider = await _context.IdentityProviders.FirstOrDefaultAsync(p => p.Id == id);
        if (provider is null)
        {
            return NotFound();
        }

        IdentityProvider = provider;
        return Page();
    }
}