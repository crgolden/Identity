namespace Identity.Pages.Admin.Keys;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class IndexModel : PageModel
{
    private readonly IPersistedGrantDbContext _context;

    public IndexModel(IPersistedGrantDbContext context) => _context = context;

    public IList<Key> Keys { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Keys = await _context.Keys
            .OrderByDescending(k => k.Created)
            .ToListAsync();
    }
}