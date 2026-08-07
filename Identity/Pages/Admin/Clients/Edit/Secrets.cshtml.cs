namespace Identity.Pages.Admin.Clients.Edit;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class SecretsModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    public SecretsModel(IConfigurationDbContext context) => _context = context;

    public Client Client { get; private set; } = new();

    [BindProperty]
    public List<ClientSecret> Secrets { get; set; } = [];

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

    public async Task<IActionResult> OnPostAddRowAsync(int id)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(c => c.Id == id);
        if (client is null)
        {
            return NotFound();
        }

        Client = client;
        Secrets.Add(new ClientSecret { Type = "SharedSecret" });
        return Page();
    }

    public async Task<IActionResult> OnPostRemoveRowAsync(int id, int index)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(c => c.Id == id);
        if (client is null)
        {
            return NotFound();
        }

        Client = client;
        if (index >= 0 && index < Secrets.Count)
        {
            Secrets.RemoveAt(index);
        }

        return Page();
    }
}