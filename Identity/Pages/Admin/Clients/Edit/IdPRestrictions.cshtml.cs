namespace Identity.Pages.Admin.Clients.Edit;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class IdPRestrictionsModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    public IdPRestrictionsModel(IConfigurationDbContext context) => _context = context;

    public Client Client { get; private set; } = new();

    [BindProperty]
    public List<ClientIdPRestriction> IdPRestrictions { get; set; } = [];

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

    public async Task<IActionResult> OnPostAddRowAsync(int id)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(c => c.Id == id);
        if (client is null)
        {
            return NotFound();
        }

        Client = client;
        IdPRestrictions.Add(new ClientIdPRestriction());
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
        if (index >= 0 && index < IdPRestrictions.Count)
        {
            IdPRestrictions.RemoveAt(index);
        }

        return Page();
    }
}