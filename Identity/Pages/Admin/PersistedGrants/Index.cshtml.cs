namespace Identity.Pages.Admin.PersistedGrants;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Lists all persisted grants.</summary>
public class IndexModel : PageModel
{
    private readonly IPersistedGrantDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="IndexModel"/> class.</summary>
    public IndexModel(IPersistedGrantDbContext context) => _context = context;

    /// <summary>Gets the persisted grants.</summary>
    public IList<PersistedGrant> PersistedGrants { get; private set; } = [];

    /// <summary>Loads all persisted grants.</summary>
    public async Task OnGetAsync()
    {
        PersistedGrants = await _context.PersistedGrants
            .OrderBy(g => g.SubjectId)
            .ThenBy(g => g.ClientId)
            .ToListAsync();
    }
}
