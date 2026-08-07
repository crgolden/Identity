namespace Identity.Pages.Admin.Users.Details;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Users;

public class LoginsModel : UserSubPageModelBase
{
    public LoginsModel(UserManager<IdentityUser<Guid>> userManager)
        : base(userManager)
    {
    }

    public IList<UserLoginInfo> Logins { get; private set; } = [];

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