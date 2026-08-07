namespace Identity.Pages.Admin.ServerSideSessions;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class IndexModel : PageModel
{
    private readonly IPersistedGrantDbContext _context;

    public IndexModel(IPersistedGrantDbContext context) => _context = context;

    public IList<ServerSideSession> ServerSideSessions { get; private set; } = [];

    public async Task OnGetAsync()
    {
        ServerSideSessions = await _context.ServerSideSessions
            .OrderBy(s => s.SubjectId)
            .ToListAsync();
    }
}