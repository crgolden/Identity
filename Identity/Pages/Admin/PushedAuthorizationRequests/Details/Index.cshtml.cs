namespace Identity.Pages.Admin.PushedAuthorizationRequests.Details;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Shows pushed authorization request details.</summary>
public class IndexModel : PageModel
{
    private readonly IPersistedGrantDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="IndexModel"/> class.</summary>
    public IndexModel(IPersistedGrantDbContext context) => _context = context;

    /// <summary>Gets the pushed authorization request.</summary>
    public PushedAuthorizationRequest PushedAuthorizationRequest { get; private set; } = new();

    /// <summary>Loads the pushed authorization request by id.</summary>
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
