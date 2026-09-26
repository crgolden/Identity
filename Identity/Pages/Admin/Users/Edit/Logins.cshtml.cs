namespace Identity.Pages.Admin.Users.Edit;

using Identity.Pages.Admin.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

public class Logins : UserResourcesBase<UserLoginInfo>
{
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

    public async Task<IActionResult> OnPostRemoveAsync(string id, string loginProvider, string providerKey)
    {
        if (!await TryLoadUserAsync(id))
        {
            return NotFound();
        }

        await UserManager.RemoveLoginAsync(AppUser, loginProvider, providerKey);
        return RedirectToPage(new { id });
    }
}
