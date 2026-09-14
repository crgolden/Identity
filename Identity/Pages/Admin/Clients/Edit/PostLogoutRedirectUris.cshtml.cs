namespace Identity.Pages.Admin.Clients.Edit;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class PostLogoutRedirectUrisModel : PageModel
{
    internal const string DetailsPageName = "/Admin/Clients/Details/PostLogoutRedirectUris";

    private readonly IConfigurationDbContext _context;

    public PostLogoutRedirectUrisModel(IConfigurationDbContext context) => _context = context;

    public Client Client { get; private set; } = new();

    [BindProperty]
    public List<ClientPostLogoutRedirectUri> PostLogoutRedirectUris { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var client = await _context.Clients
            .Include(c => c.PostLogoutRedirectUris)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (client is null)
        {
            return NotFound();
        }

        Client = client;
        PostLogoutRedirectUris = client.PostLogoutRedirectUris;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var client = await _context.Clients
            .Include(c => c.PostLogoutRedirectUris)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (client is null)
        {
            return NotFound();
        }

        var postedIds = PostLogoutRedirectUris.Where(u => u.Id > 0).Select(u => u.Id).ToHashSet();
        client.PostLogoutRedirectUris.RemoveAll(u => !postedIds.Contains(u.Id));

        foreach (var posted in PostLogoutRedirectUris.Where(u => u.Id > 0))
        {
            var existing = client.PostLogoutRedirectUris.FirstOrDefault(u => u.Id == posted.Id);
            if (existing is not null)
            {
                existing.PostLogoutRedirectUri = posted.PostLogoutRedirectUri;
            }
        }

        foreach (var posted in PostLogoutRedirectUris.Where(u => u.Id == 0))
        {
            client.PostLogoutRedirectUris.Add(new ClientPostLogoutRedirectUri { PostLogoutRedirectUri = posted.PostLogoutRedirectUri, ClientId = id });
        }

        client.Updated = DateTimeOffset.UtcNow.UtcDateTime;
        await _context.SaveChangesAsync();
        return RedirectToPage(DetailsPageName, new { id });
    }

    public async Task<IActionResult> OnPostAddRowAsync(int id)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(c => c.Id == id);
        if (client is null)
        {
            return NotFound();
        }

        Client = client;
        PostLogoutRedirectUris.Add(new ClientPostLogoutRedirectUri());
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
        if (index >= 0 && index < PostLogoutRedirectUris.Count)
        {
            PostLogoutRedirectUris.RemoveAt(index);
        }

        return Page();
    }
}