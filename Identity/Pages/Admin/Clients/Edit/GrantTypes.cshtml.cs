namespace Identity.Pages.Admin.Clients.Edit;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class GrantTypesModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    public GrantTypesModel(IConfigurationDbContext context) => _context = context;

    public Client Client { get; private set; } = new();

    [BindProperty]
    public List<ClientGrantType> GrantTypes { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var client = await _context.Clients
            .Include(c => c.AllowedGrantTypes)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (client is null)
        {
            return NotFound();
        }

        Client = client;
        GrantTypes = client.AllowedGrantTypes ?? [];
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var client = await _context.Clients
            .Include(c => c.AllowedGrantTypes)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (client is null)
        {
            return NotFound();
        }

        client.AllowedGrantTypes ??= [];
        var postedIds = GrantTypes.Where(g => g.Id > 0).Select(g => g.Id).ToHashSet();
        client.AllowedGrantTypes.RemoveAll(g => !postedIds.Contains(g.Id));

        foreach (var posted in GrantTypes.Where(g => g.Id > 0))
        {
            var existing = client.AllowedGrantTypes.FirstOrDefault(g => g.Id == posted.Id);
            if (existing is not null)
            {
                existing.GrantType = posted.GrantType;
            }
        }

        foreach (var posted in GrantTypes.Where(g => g.Id == 0))
        {
            client.AllowedGrantTypes.Add(new ClientGrantType { GrantType = posted.GrantType, ClientId = id });
        }

        client.Updated = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return RedirectToPage("/Admin/Clients/Details/GrantTypes", new { id });
    }

    public async Task<IActionResult> OnPostAddRowAsync(int id)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(c => c.Id == id);
        if (client is null)
        {
            return NotFound();
        }

        Client = client;
        GrantTypes.Add(new ClientGrantType());
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
        if (index >= 0 && index < GrantTypes.Count)
        {
            GrantTypes.RemoveAt(index);
        }

        return Page();
    }
}