namespace Identity.Pages.Admin.IdentityProviders;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class Index : PageModel
{
    private readonly IConfigurationDbContext _context;

    public Index(IConfigurationDbContext context) => _context = context;

    public IList<IdentityProvider> IdentityProviders { get; private set; } = [];

    public async Task OnGetAsync()
    {
        IdentityProviders = await _context.IdentityProviders
            .OrderBy(p => p.Scheme)
            .ToListAsync();
    }
}
