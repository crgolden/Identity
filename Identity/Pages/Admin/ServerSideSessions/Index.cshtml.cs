namespace Identity.Pages.Admin.ServerSideSessions;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Lists all server-side sessions.</summary>
public class IndexModel : PageModel
{
    private readonly IPersistedGrantDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="IndexModel"/> class.</summary>
    public IndexModel(IPersistedGrantDbContext context) => _context = context;

    /// <summary>Gets the server-side sessions.</summary>
    public IList<ServerSideSession> ServerSideSessions { get; private set; } = [];

    /// <summary>Loads all server-side sessions.</summary>
    public async Task OnGetAsync()
    {
        ServerSideSessions = await _context.ServerSideSessions
            .OrderBy(s => s.SubjectId)
            .ToListAsync();
    }
}
