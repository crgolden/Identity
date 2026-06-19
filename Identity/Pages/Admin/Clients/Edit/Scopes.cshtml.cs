namespace Identity.Pages.Admin.Clients.Edit;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Edits allowed scopes for a client.</summary>
public class ScopesModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="ScopesModel"/> class.</summary>
    public ScopesModel(IConfigurationDbContext context) => _context = context;

    /// <summary>Gets the client.</summary>
    public Client Client { get; private set; } = new();

    /// <summary>Gets or sets the posted scopes.</summary>
    [BindProperty]
    public List<ClientScope> Scopes { get; set; } = [];

    /// <summary>Loads the client with allowed scopes for editing.</summary>
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var client = await _context.Clients
            .Include(c => c.AllowedScopes)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (client is null)
        {
            return NotFound();
        }

        Client = client;
        Scopes = client.AllowedScopes ?? [];
        return Page();
    }

    /// <summary>Saves the allowed scopes for the client.</summary>
    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var client = await _context.Clients
            .Include(c => c.AllowedScopes)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (client is null)
        {
            return NotFound();
        }

        client.AllowedScopes ??= [];
        var postedIds = Scopes.Where(s => s.Id > 0).Select(s => s.Id).ToHashSet();
        client.AllowedScopes.RemoveAll(s => !postedIds.Contains(s.Id));

        foreach (var posted in Scopes.Where(s => s.Id > 0))
        {
            var existing = client.AllowedScopes.FirstOrDefault(s => s.Id == posted.Id);
            if (existing is not null)
            {
                existing.Scope = posted.Scope;
            }
        }

        foreach (var posted in Scopes.Where(s => s.Id == 0))
        {
            client.AllowedScopes.Add(new ClientScope { Scope = posted.Scope, ClientId = id });
        }

        client.Updated = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return RedirectToPage("/Admin/Clients/Details/Scopes", new { id });
    }
}
