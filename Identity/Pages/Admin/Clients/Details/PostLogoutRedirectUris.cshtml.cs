namespace Identity.Pages.Admin.Clients.Details;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Displays post-logout redirect URIs for a client.</summary>
public class PostLogoutRedirectUrisModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="PostLogoutRedirectUrisModel"/> class.</summary>
    public PostLogoutRedirectUrisModel(IConfigurationDbContext context) => _context = context;

    /// <summary>Gets the client.</summary>
    public Client Client { get; private set; } = new();

    /// <summary>Loads the client with post-logout redirect URIs.</summary>
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var client = await _context.Clients
            .Include(c => c.PostLogoutRedirectUris)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (client is null)
        {
            return NotFound();
        }

        Client = client;
        return Page();
    }
}
