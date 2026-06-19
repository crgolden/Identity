namespace Identity.Pages.Admin.Users;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>Deletes a user.</summary>
public class DeleteModel : PageModel
{
    private readonly UserManager<IdentityUser<Guid>> _userManager;

    /// <summary>Initializes a new instance of the <see cref="DeleteModel"/> class.</summary>
    public DeleteModel(UserManager<IdentityUser<Guid>> userManager) => _userManager = userManager;

    /// <summary>Gets the user to delete.</summary>
    public IdentityUser<Guid> AppUser { get; private set; } = new();

    /// <summary>Loads the user for deletion confirmation.</summary>
    public async Task<IActionResult> OnGetAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        AppUser = user;
        return Page();
    }

    /// <summary>Deletes the user.</summary>
    public async Task<IActionResult> OnPostAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        await _userManager.DeleteAsync(user);
        return RedirectToPage("./Index");
    }
}
