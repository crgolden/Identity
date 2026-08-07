namespace Identity.Pages.Admin.SamlLogoutSessionRequestIndices;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class IndexModel : PageModel
{
    private readonly IPersistedGrantDbContext _context;

    public IndexModel(IPersistedGrantDbContext context) => _context = context;

    public IList<SamlLogoutSessionRequestIndex> SamlLogoutSessionRequestIndices { get; private set; } = [];

    public async Task OnGetAsync()
    {
        SamlLogoutSessionRequestIndices = await _context.SamlLogoutSessionRequestIndices
            .OrderBy(i => i.Id)
            .ToListAsync();
    }
}