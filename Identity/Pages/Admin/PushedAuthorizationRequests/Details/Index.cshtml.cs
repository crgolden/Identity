namespace Identity.Pages.Admin.PushedAuthorizationRequests.Details;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class IndexModel : PageModel
{
    private readonly IPersistedGrantDbContext _context;

    public IndexModel(IPersistedGrantDbContext context) => _context = context;

    public PushedAuthorizationRequest PushedAuthorizationRequest { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(long id)
    {
        var par = await _context.PushedAuthorizationRequests.FirstOrDefaultAsync(p => p.Id == id);
        if (par is null)
        {
            return NotFound();
        }

        PushedAuthorizationRequest = par;
        return Page();
    }
}