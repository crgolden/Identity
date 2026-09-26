namespace Identity.Pages.Admin.IdentityResources;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class Index : PageModel
{
    private readonly IConfigurationDbContext _context;

    public Index(IConfigurationDbContext context) => _context = context;

    public IList<IdentityResource> IdentityResources { get; private set; } = [];

    public async Task OnGetAsync()
    {
        IdentityResources = await _context.IdentityResources.OrderBy(r => r.Name).ToListAsync();
    }
}
