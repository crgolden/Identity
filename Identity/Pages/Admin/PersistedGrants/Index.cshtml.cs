namespace Identity.Pages.Admin.PersistedGrants;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class IndexModel : PageModel
{
    private readonly IPersistedGrantDbContext _context;

    public IndexModel(IPersistedGrantDbContext context) => _context = context;

    public IList<PersistedGrant> PersistedGrants { get; private set; } = [];

    public async Task OnGetAsync()
    {
        PersistedGrants = await _context.PersistedGrants
            .OrderBy(g => g.SubjectId)
            .ThenBy(g => g.ClientId)
            .ToListAsync();
    }
}