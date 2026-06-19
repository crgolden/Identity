namespace Identity.Pages.Admin.SamlSigninStates.Details;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Shows SAML sign-in state details.</summary>
public class IndexModel : PageModel
{
    private readonly IPersistedGrantDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="IndexModel"/> class.</summary>
    public IndexModel(IPersistedGrantDbContext context) => _context = context;

    /// <summary>Gets the SAML sign-in state.</summary>
    public SamlSigninState SamlSigninState { get; private set; } = new();

    /// <summary>Loads the SAML sign-in state by id.</summary>
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
