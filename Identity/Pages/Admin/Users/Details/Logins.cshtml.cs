namespace Identity.Pages.Admin.Users.Details;

using Identity.Pages.Admin.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

/// <summary>Shows user external logins.</summary>
public class LoginsModel : UserSubPageModelBase
{
    /// <summary>Initializes a new instance of the <see cref="LoginsModel"/> class.</summary>
    public LoginsModel(UserManager<IdentityUser<Guid>> userManager)
        : base(userManager)
    {
    }

    /// <summary>Gets the user's external logins.</summary>
    public IList<UserLoginInfo> Logins { get; private set; } = [];

    /// <summary>Loads the user's external logins.</summary>
    public async Task<IActionResult> OnGetAsync(string id)
    {
        if (!await TryLoadUserAsync(id))
        {
            return NotFound();
        }

        Logins = await UserManager.GetLoginsAsync(AppUser);
        return Page();
    }
}
