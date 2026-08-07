namespace Identity.Pages.Admin.ApiScopes;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class IndexModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    public IndexModel(IConfigurationDbContext context) => _context = context;

    public IList<ApiScope> ApiScopes { get; private set; } = [];

    public async Task OnGetAsync()
    {
        ApiScopes = await _context.ApiScopes.OrderBy(s => s.Name).ToListAsync();
    }
}