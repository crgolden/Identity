namespace Identity.Pages.Admin.ApiResources;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class IndexModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    public IndexModel(IConfigurationDbContext context) => _context = context;

    public IList<ApiResource> ApiResources { get; private set; } = [];

    public async Task OnGetAsync()
    {
        ApiResources = await _context.ApiResources.OrderBy(r => r.Name).ToListAsync();
    }
}