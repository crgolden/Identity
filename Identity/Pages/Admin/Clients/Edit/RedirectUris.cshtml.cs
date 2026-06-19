namespace Identity.Pages.Admin.Clients.Edit;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Edits redirect URIs for a client.</summary>
public class RedirectUrisModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="RedirectUrisModel"/> class.</summary>
    public RedirectUrisModel(IConfigurationDbContext context) => _context = context;

    /// <summary>Gets the client.</summary>
    public Client Client { get; private set; } = new();

    /// <summary>Gets or sets the posted redirect URIs.</summary>
    [BindProperty]
    public List<ClientRedirectUri> RedirectUris { get; set; } = [];

    /// <summary>Loads the client with redirect URIs for editing.</summary>
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
        RedirectUris = client.RedirectUris ?? [];
        return Page();
    }

    /// <summary>Saves the redirect URIs for the client.</summary>
    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var client = await _context.Clients
            .Include(c => c.RedirectUris)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (client is null)
        {
            return NotFound();
        }

        client.RedirectUris ??= [];
        var postedIds = RedirectUris.Where(u => u.Id > 0).Select(u => u.Id).ToHashSet();
        client.RedirectUris.RemoveAll(u => !postedIds.Contains(u.Id));

        foreach (var posted in RedirectUris.Where(u => u.Id > 0))
        {
            var existing = client.RedirectUris.FirstOrDefault(u => u.Id == posted.Id);
            if (existing is not null)
            {
                existing.RedirectUri = posted.RedirectUri;
            }
        }

        foreach (var posted in RedirectUris.Where(u => u.Id == 0))
        {
            client.RedirectUris.Add(new ClientRedirectUri { RedirectUri = posted.RedirectUri, ClientId = id });
        }

        client.Updated = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return RedirectToPage("/Admin/Clients/Details/RedirectUris", new { id });
    }
}
