namespace Identity.Pages.Admin.SamlSigninStates;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class IndexModel : PageModel
{
    private readonly IPersistedGrantDbContext _context;

    public IndexModel(IPersistedGrantDbContext context) => _context = context;

    public IList<SamlSigninState> SamlSigninStates { get; private set; } = [];

    public async Task OnGetAsync()
    {
        SamlSigninStates = await _context.SamlSigninStates
            .OrderByDescending(s => s.ExpiresAtUtc)
            .ToListAsync();
    }
}