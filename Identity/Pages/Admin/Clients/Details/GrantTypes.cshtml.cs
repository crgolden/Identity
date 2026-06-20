namespace Identity.Pages.Admin.Clients.Details;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Displays allowed grant types for a client.</summary>
public class GrantTypesModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="GrantTypesModel"/> class.</summary>
    public GrantTypesModel(IConfigurationDbContext context) => _context = context;

    /// <summary>Gets the client.</summary>
    public Client Client { get; private set; } = new();

    /// <summary>Loads the client with grant types.</summary>
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var client = await _context.Clients
            .Include(c => c.AllowedGrantTypes)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (client is null)
        {
            return NotFound();
        }

        Client = client;
        return Page();
    }
}
