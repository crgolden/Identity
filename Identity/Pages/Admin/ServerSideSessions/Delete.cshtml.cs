namespace Identity.Pages.Admin.ServerSideSessions;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Deletes a server-side session.</summary>
public class DeleteModel : PageModel
{
    private readonly IPersistedGrantDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="DeleteModel"/> class.</summary>
    public DeleteModel(IPersistedGrantDbContext context) => _context = context;

    /// <summary>Gets the server-side session to delete.</summary>
    public ServerSideSession ServerSideSession { get; private set; } = new();

    /// <summary>Loads the server-side session for confirmation.</summary>
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

    /// <summary>Deletes the server-side session.</summary>
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
