namespace Identity.Pages.Admin.Clients;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Lists all clients.</summary>
public class IndexModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="IndexModel"/> class.</summary>
    public IndexModel(IConfigurationDbContext context) => _context = context;

    /// <summary>Gets the list of clients.</summary>
    public IList<Client> Clients { get; private set; } = [];

    /// <summary>Loads all clients ordered by ClientId.</summary>
    public async Task OnGetAsync() =>
        Clients = await _context.Clients.OrderBy(c => c.ClientId).ToListAsync();
}
