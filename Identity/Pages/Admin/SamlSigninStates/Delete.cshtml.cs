namespace Identity.Pages.Admin.SamlSigninStates;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Deletes a SAML sign-in state.</summary>
public class DeleteModel : PageModel
{
    private readonly IPersistedGrantDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="DeleteModel"/> class.</summary>
    public DeleteModel(IPersistedGrantDbContext context) => _context = context;

    /// <summary>Gets the SAML sign-in state to delete.</summary>
    public SamlSigninState SamlSigninState { get; private set; } = new();

    /// <summary>Loads the SAML sign-in state for confirmation.</summary>
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

    /// <summary>Deletes the SAML sign-in state.</summary>
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
