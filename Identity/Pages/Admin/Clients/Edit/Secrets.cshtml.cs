namespace Identity.Pages.Admin.Clients.Edit;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Edits secrets for a client.</summary>
public class SecretsModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="SecretsModel"/> class.</summary>
    public SecretsModel(IConfigurationDbContext context) => _context = context;

    /// <summary>Gets the client.</summary>
    public Client Client { get; private set; } = new();

    /// <summary>Gets or sets the posted secrets.</summary>
    [BindProperty]
    public List<ClientSecret> Secrets { get; set; } = [];

    /// <summary>Loads the client with secrets for editing.</summary>
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var client = await _context.Clients
            .Include(c => c.ClientSecrets)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (client is null)
        {
            return NotFound();
        }

        Client = client;
        Secrets = client.ClientSecrets ?? [];
        return Page();
    }

    /// <summary>Saves the secrets for the client.</summary>
    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var client = await _context.Clients
            .Include(c => c.ClientSecrets)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (client is null)
        {
            return NotFound();
        }

        client.ClientSecrets ??= [];
        var postedIds = Secrets.Where(s => s.Id > 0).Select(s => s.Id).ToHashSet();
        client.ClientSecrets.RemoveAll(s => !postedIds.Contains(s.Id));

        foreach (var posted in Secrets.Where(s => s.Id > 0))
        {
            var existing = client.ClientSecrets.FirstOrDefault(s => s.Id == posted.Id);
            if (existing is not null)
            {
                existing.Description = posted.Description;
                existing.Type = posted.Type;
                existing.Expiration = posted.Expiration;
            }
        }

        foreach (var posted in Secrets.Where(s => s.Id == 0))
        {
            client.ClientSecrets.Add(new ClientSecret
            {
                Description = posted.Description,
                Value = posted.Value,
                Type = posted.Type,
                Expiration = posted.Expiration,
                ClientId = id,
            });
        }

        client.Updated = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return RedirectToPage("/Admin/Clients/Details/Secrets", new { id });
    }
}
