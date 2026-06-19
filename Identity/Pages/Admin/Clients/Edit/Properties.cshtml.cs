namespace Identity.Pages.Admin.Clients.Edit;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Edits properties for a client.</summary>
public class PropertiesModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="PropertiesModel"/> class.</summary>
    public PropertiesModel(IConfigurationDbContext context) => _context = context;

    /// <summary>Gets the client.</summary>
    public Client Client { get; private set; } = new();

    /// <summary>Gets or sets the posted properties.</summary>
    [BindProperty]
    public List<ClientProperty> Properties { get; set; } = [];

    /// <summary>Loads the client with properties for editing.</summary>
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var client = await _context.Clients
            .Include(c => c.Properties)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (client is null)
        {
            return NotFound();
        }

        Client = client;
        Properties = client.Properties ?? [];
        return Page();
    }

    /// <summary>Saves the properties for the client.</summary>
    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var client = await _context.Clients
            .Include(c => c.Properties)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (client is null)
        {
            return NotFound();
        }

        client.Properties ??= [];
        var postedIds = Properties.Where(p => p.Id > 0).Select(p => p.Id).ToHashSet();
        client.Properties.RemoveAll(p => !postedIds.Contains(p.Id));

        foreach (var posted in Properties.Where(p => p.Id > 0))
        {
            var existing = client.Properties.FirstOrDefault(p => p.Id == posted.Id);
            if (existing is not null)
            {
                existing.Key = posted.Key;
                existing.Value = posted.Value;
            }
        }

        foreach (var posted in Properties.Where(p => p.Id == 0))
        {
            client.Properties.Add(new ClientProperty { Key = posted.Key, Value = posted.Value, ClientId = id });
        }

        client.Updated = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return RedirectToPage("/Admin/Clients/Details/Properties", new { id });
    }
}
