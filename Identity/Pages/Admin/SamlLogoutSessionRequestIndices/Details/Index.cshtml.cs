namespace Identity.Pages.Admin.SamlLogoutSessionRequestIndices.Details;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class IndexModel : PageModel
{
    private readonly IPersistedGrantDbContext _context;

    public IndexModel(IPersistedGrantDbContext context) => _context = context;

    public SamlLogoutSessionRequestIndex SamlLogoutSessionRequestIndex { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(long id)
    {
        var index = await _context.SamlLogoutSessionRequestIndices.FirstOrDefaultAsync(i => i.Id == id);
        if (index is null)
        {
            return NotFound();
        }

        SamlLogoutSessionRequestIndex = index;
        return Page();
    }
}