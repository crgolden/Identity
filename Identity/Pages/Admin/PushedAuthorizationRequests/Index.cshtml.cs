namespace Identity.Pages.Admin.PushedAuthorizationRequests;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Lists all pushed authorization requests.</summary>
public class IndexModel : PageModel
{
    private readonly IPersistedGrantDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="IndexModel"/> class.</summary>
    public IndexModel(IPersistedGrantDbContext context) => _context = context;

    /// <summary>Gets the pushed authorization requests.</summary>
    public IList<PushedAuthorizationRequest> PushedAuthorizationRequests { get; private set; } = [];

    /// <summary>Loads all pushed authorization requests.</summary>
    public async Task OnGetAsync()
    {
        PushedAuthorizationRequests = await _context.PushedAuthorizationRequests
            .OrderBy(p => p.ExpiresAtUtc)
            .ToListAsync();
    }
}
