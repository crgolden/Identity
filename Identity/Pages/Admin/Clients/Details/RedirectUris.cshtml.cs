namespace Identity.Pages.Admin.Clients.Details;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class RedirectUrisModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    public RedirectUrisModel(IConfigurationDbContext context) => _context = context;

    public Client Client { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var client = await _context.Clients
            .Include(c => c.RedirectUris)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (client is null)
        {
            return NotFound();
        }

        Client = client;
        return Page();
    }
}