namespace Identity.Pages.Admin.PushedAuthorizationRequests;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class IndexModel : PageModel
{
    private readonly IPersistedGrantDbContext _context;

    public IndexModel(IPersistedGrantDbContext context) => _context = context;

    public IList<PushedAuthorizationRequest> PushedAuthorizationRequests { get; private set; } = [];

    public async Task OnGetAsync()
    {
        PushedAuthorizationRequests = await _context.PushedAuthorizationRequests
            .OrderBy(p => p.ExpiresAtUtc)
            .ToListAsync();
    }
}