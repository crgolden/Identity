namespace Identity.Pages.Admin.Clients.Edit;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Edits allowed CORS origins for a client.</summary>
public class CorsOriginsModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="CorsOriginsModel"/> class.</summary>
    public CorsOriginsModel(IConfigurationDbContext context) => _context = context;

    /// <summary>Gets the client.</summary>
    public Client Client { get; private set; } = new();

    /// <summary>Gets or sets the posted CORS origins.</summary>
    [BindProperty]
    public List<ClientCorsOrigin> CorsOrigins { get; set; } = [];

    /// <summary>Loads the client with CORS origins for editing.</summary>
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var client = await _context.Clients
            .Include(c => c.AllowedCorsOrigins)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (client is null)
        {
            return NotFound();
        }

        Client = client;
        CorsOrigins = client.AllowedCorsOrigins ?? [];
        return Page();
    }

    /// <summary>Saves the CORS origins for the client.</summary>
    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var client = await _context.Clients
            .Include(c => c.AllowedCorsOrigins)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (client is null)
        {
            return NotFound();
        }

        client.AllowedCorsOrigins ??= [];
        var postedIds = CorsOrigins.Where(o => o.Id > 0).Select(o => o.Id).ToHashSet();
        client.AllowedCorsOrigins.RemoveAll(o => !postedIds.Contains(o.Id));

        foreach (var posted in CorsOrigins.Where(o => o.Id > 0))
        {
            var existing = client.AllowedCorsOrigins.FirstOrDefault(o => o.Id == posted.Id);
            if (existing is not null)
            {
                existing.Origin = posted.Origin;
            }
        }

        foreach (var posted in CorsOrigins.Where(o => o.Id == 0))
        {
            client.AllowedCorsOrigins.Add(new ClientCorsOrigin { Origin = posted.Origin, ClientId = id });
        }

        client.Updated = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return RedirectToPage("/Admin/Clients/Details/CorsOrigins", new { id });
    }
}
