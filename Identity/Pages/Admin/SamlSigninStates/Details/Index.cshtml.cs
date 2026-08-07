namespace Identity.Pages.Admin.SamlSigninStates.Details;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class IndexModel : PageModel
{
    private readonly IPersistedGrantDbContext _context;

    public IndexModel(IPersistedGrantDbContext context) => _context = context;

    public SamlSigninState SamlSigninState { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(long id)
    {
        var state = await _context.SamlSigninStates.FirstOrDefaultAsync(s => s.Id == id);
        if (state is null)
        {
            return NotFound();
        }

        SamlSigninState = state;
        return Page();
    }
}