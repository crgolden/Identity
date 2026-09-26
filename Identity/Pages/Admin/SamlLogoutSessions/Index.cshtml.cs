namespace Identity.Pages.Admin.SamlLogoutSessions;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class Index : PageModel
{
    private readonly IPersistedGrantDbContext _context;

    public Index(IPersistedGrantDbContext context) => _context = context;

    public IList<SamlLogoutSession> SamlLogoutSessions { get; private set; } = [];

    public async Task OnGetAsync()
    {
        SamlLogoutSessions = await _context.SamlLogoutSessions
            .OrderByDescending(s => s.ExpiresAtUtc)
            .ToListAsync();
    }
}
