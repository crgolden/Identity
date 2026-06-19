namespace Identity.Pages.Admin.SamlLogoutSessions;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Deletes a SAML logout session.</summary>
public class DeleteModel : PageModel
{
    private readonly IPersistedGrantDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="DeleteModel"/> class.</summary>
    public DeleteModel(IPersistedGrantDbContext context) => _context = context;

    /// <summary>Gets the SAML logout session to delete.</summary>
    public SamlLogoutSession SamlLogoutSession { get; private set; } = new();

    /// <summary>Loads the SAML logout session for confirmation.</summary>
    public async Task<IActionResult> OnGetAsync(long id)
    {
        var session = await _context.SamlLogoutSessions.FirstOrDefaultAsync(s => s.Id == id);
        if (session is null)
        {
            return NotFound();
        }

        SamlLogoutSession = session;
        return Page();
    }

    /// <summary>Deletes the SAML logout session.</summary>
    public async Task<IActionResult> OnPostAsync(long id)
    {
        var session = await _context.SamlLogoutSessions.FirstOrDefaultAsync(s => s.Id == id);
        if (session is null)
        {
            return NotFound();
        }

        _context.SamlLogoutSessions.Remove(session);
        await _context.SaveChangesAsync();
        return RedirectToPage("./Index");
    }
}
