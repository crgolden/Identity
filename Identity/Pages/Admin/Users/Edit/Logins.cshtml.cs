namespace Identity.Pages.Admin.Users.Edit;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>Manages user external logins.</summary>
public class LoginsModel : PageModel
{
    private readonly UserManager<IdentityUser<Guid>> _userManager;

    /// <summary>Initializes a new instance of the <see cref="LoginsModel"/> class.</summary>
    public LoginsModel(UserManager<IdentityUser<Guid>> userManager) => _userManager = userManager;

    /// <summary>Gets the user.</summary>
    public IdentityUser<Guid> AppUser { get; private set; } = new();

    /// <summary>Gets the user's external logins.</summary>
    public IList<UserLoginInfo> Logins { get; private set; } = [];

    /// <summary>Loads the user's external logins.</summary>
    public async Task<IActionResult> OnGetAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        AppUser = user;
        Logins = await _userManager.GetLoginsAsync(user);
        return Page();
    }

    /// <summary>Removes an external login.</summary>
    public async Task<IActionResult> OnPostRemoveAsync(string id, string loginProvider, string providerKey)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        await _userManager.RemoveLoginAsync(user, loginProvider, providerKey);
        return RedirectToPage(new { id });
    }
}
