namespace Identity.Pages.Admin.Clients.Edit;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class ScopesModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    public ScopesModel(IConfigurationDbContext context) => _context = context;

    public Client Client { get; private set; } = new();

    [BindProperty]
    public List<ClientScope> Scopes { get; set; } = [];

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

    public async Task<IActionResult> OnPostAddRowAsync(int id)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(c => c.Id == id);
        if (client is null)
        {
            return NotFound();
        }

        Client = client;
        Scopes.Add(new ClientScope());
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
        if (index >= 0 && index < Scopes.Count)
        {
            Scopes.RemoveAt(index);
        }

        return Page();
    }
}