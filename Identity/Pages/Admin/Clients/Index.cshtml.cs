namespace Identity.Pages.Admin.Clients;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class Index : PageModel
{
    private readonly IConfigurationDbContext _context;

    public Index(IConfigurationDbContext context) => _context = context;

    public IList<Client> Clients { get; private set; } = [];

    public async Task OnGetAsync() =>
        Clients = await _context.Clients.OrderBy(c => c.ClientId).ToListAsync();
}
