namespace Identity.Pages.Admin.PushedAuthorizationRequests;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Deletes a pushed authorization request.</summary>
public class DeleteModel : PageModel
{
    private readonly IPersistedGrantDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="DeleteModel"/> class.</summary>
    public DeleteModel(IPersistedGrantDbContext context) => _context = context;

    /// <summary>Gets the pushed authorization request to delete.</summary>
    public PushedAuthorizationRequest PushedAuthorizationRequest { get; private set; } = new();

    /// <summary>Loads the pushed authorization request for confirmation.</summary>
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

    /// <summary>Deletes the pushed authorization request.</summary>
    public async Task<IActionResult> OnPostAsync(long id)
    {
        var par = await _context.PushedAuthorizationRequests.FirstOrDefaultAsync(p => p.Id == id);
        if (par is null)
        {
            return NotFound();
        }

        _context.PushedAuthorizationRequests.Remove(par);
        await _context.SaveChangesAsync();
        return RedirectToPage("./Index");
    }
}
