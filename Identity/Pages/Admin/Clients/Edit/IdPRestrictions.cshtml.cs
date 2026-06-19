namespace Identity.Pages.Admin.Clients.Edit;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Edits identity provider restrictions for a client.</summary>
public class IdPRestrictionsModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="IdPRestrictionsModel"/> class.</summary>
    public IdPRestrictionsModel(IConfigurationDbContext context) => _context = context;

    /// <summary>Gets the client.</summary>
    public Client Client { get; private set; } = new();

    /// <summary>Gets or sets the posted IdP restrictions.</summary>
    [BindProperty]
    public List<ClientIdPRestriction> IdPRestrictions { get; set; } = [];

    /// <summary>Loads the client with IdP restrictions for editing.</summary>
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var client = await _context.Clients
            .Include(c => c.IdentityProviderRestrictions)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (client is null)
        {
            return NotFound();
        }

        Client = client;
        IdPRestrictions = client.IdentityProviderRestrictions ?? [];
        return Page();
    }

    /// <summary>Saves the IdP restrictions for the client.</summary>
    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var client = await _context.Clients
            .Include(c => c.IdentityProviderRestrictions)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (client is null)
        {
            return NotFound();
        }

        client.IdentityProviderRestrictions ??= [];
        var postedIds = IdPRestrictions.Where(r => r.Id > 0).Select(r => r.Id).ToHashSet();
        client.IdentityProviderRestrictions.RemoveAll(r => !postedIds.Contains(r.Id));

        foreach (var posted in IdPRestrictions.Where(r => r.Id > 0))
        {
            var existing = client.IdentityProviderRestrictions.FirstOrDefault(r => r.Id == posted.Id);
            if (existing is not null)
            {
                existing.Provider = posted.Provider;
            }
        }

        foreach (var posted in IdPRestrictions.Where(r => r.Id == 0))
        {
            client.IdentityProviderRestrictions.Add(new ClientIdPRestriction { Provider = posted.Provider, ClientId = id });
        }

        client.Updated = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return RedirectToPage("/Admin/Clients/Details/IdPRestrictions", new { id });
    }
}
