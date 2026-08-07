namespace Identity.Pages.Admin.SamlLogoutSessions;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class DeleteModel : PageModel
{
    private readonly IPersistedGrantDbContext _context;

    public DeleteModel(IPersistedGrantDbContext context) => _context = context;

    public SamlLogoutSession SamlLogoutSession { get; private set; } = new();

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