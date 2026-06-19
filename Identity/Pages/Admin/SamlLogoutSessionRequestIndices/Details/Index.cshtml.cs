namespace Identity.Pages.Admin.SamlLogoutSessionRequestIndices.Details;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Shows SAML logout session request index details.</summary>
public class IndexModel : PageModel
{
    private readonly IPersistedGrantDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="IndexModel"/> class.</summary>
    public IndexModel(IPersistedGrantDbContext context) => _context = context;

    /// <summary>Gets the SAML logout session request index.</summary>
    public SamlLogoutSessionRequestIndex SamlLogoutSessionRequestIndex { get; private set; } = new();

    /// <summary>Loads the SAML logout session request index by id.</summary>
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
