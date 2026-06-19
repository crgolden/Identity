namespace Identity.Pages.Admin.SamlSigninStates;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Lists all SAML sign-in states.</summary>
public class IndexModel : PageModel
{
    private readonly IPersistedGrantDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="IndexModel"/> class.</summary>
    public IndexModel(IPersistedGrantDbContext context) => _context = context;

    /// <summary>Gets the SAML sign-in states.</summary>
    public IList<SamlSigninState> SamlSigninStates { get; private set; } = [];

    /// <summary>Loads all SAML sign-in states.</summary>
    public async Task OnGetAsync()
    {
        SamlSigninStates = await _context.SamlSigninStates
            .OrderByDescending(s => s.ExpiresAtUtc)
            .ToListAsync();
    }
}
