namespace Identity.Pages.Admin.Clients;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class IndexModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    public IndexModel(IConfigurationDbContext context) => _context = context;

    public IList<Client> Clients { get; private set; } = [];

    public async Task OnGetAsync() =>
        Clients = await _context.Clients.OrderBy(c => c.ClientId).ToListAsync();
}