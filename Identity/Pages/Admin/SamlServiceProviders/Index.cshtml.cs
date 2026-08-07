namespace Identity.Pages.Admin.SamlServiceProviders;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class IndexModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    public IndexModel(IConfigurationDbContext context) => _context = context;

    public IList<SamlServiceProvider> SamlServiceProviders { get; private set; } = [];

    public async Task OnGetAsync()
    {
        SamlServiceProviders = await _context.SamlServiceProviders
            .OrderBy(s => s.EntityId)
            .ToListAsync();
    }
}