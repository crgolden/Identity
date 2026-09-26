namespace Identity.Pages.Admin.Users.Details;

using Identity.Pages.Admin.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

public class Logins : UserResourcesBase<UserLoginInfo>
{
    internal const string PageName = "/Admin/Users/Details/Logins";

    public Logins(UserManager<IdentityUser<Guid>> userManager)
        : base(userManager)
    {
    }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        if (!await TryLoadUserAsync(id))
        {
            return NotFound();
        }

        Resources = await UserManager.GetLoginsAsync(AppUser);
        return Page();
    }
}
