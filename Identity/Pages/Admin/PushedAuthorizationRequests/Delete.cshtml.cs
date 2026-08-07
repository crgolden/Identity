namespace Identity.Pages.Admin.PushedAuthorizationRequests;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class DeleteModel : PageModel
{
    private readonly IPersistedGrantDbContext _context;

    public DeleteModel(IPersistedGrantDbContext context) => _context = context;

    public PushedAuthorizationRequest PushedAuthorizationRequest { get; private set; } = new();

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