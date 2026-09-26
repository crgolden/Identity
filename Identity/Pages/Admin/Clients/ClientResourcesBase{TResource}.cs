namespace Identity.Pages.Admin.Clients;

using System.Linq.Expressions;
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public abstract class ClientResourcesBase<TResource> : ResourcesBase<TResource>
    where TResource : class
{
    private readonly IConfigurationDbContext _context;

    protected ClientResourcesBase(IConfigurationDbContext context) => _context = context;

    public Client Client { get; private set; } = new();

    protected abstract Expression<Func<Client, List<TResource>>> Collection { get; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var client = await _context.Clients
            .Include(Collection)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (client is null)
        {
            return NotFound();
        }

        Client = client;
        Resources = Collection.Compile().Invoke(client);
        return Page();
    }
}
