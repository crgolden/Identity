namespace Identity.Pages.Admin.SamlSigninStates;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class DeleteModel : PageModel
{
    private readonly IPersistedGrantDbContext _context;

    public DeleteModel(IPersistedGrantDbContext context) => _context = context;

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

    public async Task<IActionResult> OnPostAsync(long id)
    {
        var state = await _context.SamlSigninStates.FirstOrDefaultAsync(s => s.Id == id);
        if (state is null)
        {
            return NotFound();
        }

        _context.SamlSigninStates.Remove(state);
        await _context.SaveChangesAsync();
        return RedirectToPage("./Index");
    }
}