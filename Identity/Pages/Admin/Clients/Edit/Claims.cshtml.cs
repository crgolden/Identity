namespace Identity.Pages.Admin.Clients.Edit;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Edits claims for a client.</summary>
public class ClaimsModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="ClaimsModel"/> class.</summary>
    public ClaimsModel(IConfigurationDbContext context) => _context = context;

    /// <summary>Gets the client.</summary>
    public Client Client { get; private set; } = new();

    /// <summary>Gets or sets the posted claims.</summary>
    [BindProperty]
    public List<ClientClaim> Claims { get; set; } = [];

    /// <summary>Loads the client with claims for editing.</summary>
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var client = await _context.Clients
            .Include(c => c.Claims)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (client is null)
        {
            return NotFound();
        }

        Client = client;
        Claims = client.Claims ?? [];
        return Page();
    }

    /// <summary>Saves the claims for the client.</summary>
    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var client = await _context.Clients
            .Include(c => c.Claims)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (client is null)
        {
            return NotFound();
        }

        client.Claims ??= [];
        var postedIds = Claims.Where(c => c.Id > 0).Select(c => c.Id).ToHashSet();
        client.Claims.RemoveAll(c => !postedIds.Contains(c.Id));

        foreach (var posted in Claims.Where(c => c.Id > 0))
        {
            var existing = client.Claims.FirstOrDefault(c => c.Id == posted.Id);
            if (existing is not null)
            {
                existing.Type = posted.Type;
                existing.Value = posted.Value;
            }
        }

        foreach (var posted in Claims.Where(c => c.Id == 0))
        {
            client.Claims.Add(new ClientClaim { Type = posted.Type, Value = posted.Value, ClientId = id });
        }

        client.Updated = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return RedirectToPage("/Admin/Clients/Details/Claims", new { id });
    }
}
