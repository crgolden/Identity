namespace Identity.Pages.Admin.SamlLogoutSessions;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class IndexModel : PageModel
{
    private readonly IPersistedGrantDbContext _context;

    public IndexModel(IPersistedGrantDbContext context) => _context = context;

    public IList<SamlLogoutSession> SamlLogoutSessions { get; private set; } = [];

    public async Task OnGetAsync()
    {
        SamlLogoutSessions = await _context.SamlLogoutSessions
            .OrderByDescending(s => s.ExpiresAtUtc)
            .ToListAsync();
    }
}