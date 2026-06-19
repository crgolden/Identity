namespace Identity.Pages.Admin.Keys;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Lists all automatic key management keys.</summary>
public class IndexModel : PageModel
{
    private readonly IPersistedGrantDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="IndexModel"/> class.</summary>
    public IndexModel(IPersistedGrantDbContext context) => _context = context;

    /// <summary>Gets the keys.</summary>
    public IList<Key> Keys { get; private set; } = [];

    /// <summary>Loads all keys.</summary>
    public async Task OnGetAsync()
    {
        Keys = await _context.Keys
            .OrderByDescending(k => k.Created)
            .ToListAsync();
    }
}
