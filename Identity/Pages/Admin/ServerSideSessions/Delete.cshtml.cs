namespace Identity.Pages.Admin.ServerSideSessions;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class DeleteModel : PageModel
{
    private readonly IPersistedGrantDbContext _context;

    public DeleteModel(IPersistedGrantDbContext context) => _context = context;

    public ServerSideSession ServerSideSession { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(string key)
    {
        var session = await _context.ServerSideSessions.FirstOrDefaultAsync(s => s.Key == key);
        if (session is null)
        {
            return NotFound();
        }

        ServerSideSession = session;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string key)
    {
        var session = await _context.ServerSideSessions.FirstOrDefaultAsync(s => s.Key == key);
        if (session is null)
        {
            return NotFound();
        }

        _context.ServerSideSessions.Remove(session);
        await _context.SaveChangesAsync();
        return RedirectToPage("./Index");
    }
}