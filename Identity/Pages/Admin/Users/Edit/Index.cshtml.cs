namespace Identity.Pages.Admin.Users.Edit;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>Edits user scalar fields.</summary>
public class IndexModel : PageModel
{
    private readonly UserManager<IdentityUser<Guid>> _userManager;

    /// <summary>Initializes a new instance of the <see cref="IndexModel"/> class.</summary>
    public IndexModel(UserManager<IdentityUser<Guid>> userManager) => _userManager = userManager;

    /// <summary>Gets or sets the user being edited.</summary>
    [BindProperty]
    public IdentityUser<Guid> AppUser { get; set; } = new();

    /// <summary>Loads the user for editing.</summary>
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

    /// <summary>Saves the updated user fields.</summary>
    public async Task<IActionResult> OnPostAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        user.UserName = AppUser.UserName;
        user.Email = AppUser.Email;
        user.PhoneNumber = AppUser.PhoneNumber;
        user.LockoutEnabled = AppUser.LockoutEnabled;
        user.EmailConfirmed = AppUser.EmailConfirmed;
        await _userManager.UpdateAsync(user);
        return RedirectToPage("/Admin/Users/Details/Index", new { id });
    }
}
