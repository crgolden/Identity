namespace Identity.Pages.Admin.Clients.Details;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Displays claims for a client.</summary>
public class ClaimsModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="ClaimsModel"/> class.</summary>
    public ClaimsModel(IConfigurationDbContext context) => _context = context;

    /// <summary>Gets the client.</summary>
    public Client Client { get; private set; } = new();

    /// <summary>Loads the client with claims.</summary>
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var client = await _context.Clients
            .Include(c => c.Claims)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (client is null)
        {
            return NotFound();
        }

        Client = client;
        return Page();
    }
}
