namespace Identity.Pages.Admin.Users.Details;

using Identity.Pages.Admin.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

public class Passkeys : UserResourcesBase<UserPasskeyInfo>
{
    internal const string PageName = "/Admin/Users/Details/Passkeys";

    public Passkeys(UserManager<IdentityUser<Guid>> userManager)
        : base(userManager)
    {
    }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        if (!await TryLoadUserAsync(id))
        {
            return NotFound();
        }

        Resources = await UserManager.GetPasskeysAsync(AppUser);
        return Page();
    }
}
