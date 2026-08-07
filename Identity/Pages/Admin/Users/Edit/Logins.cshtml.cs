namespace Identity.Pages.Admin.Users.Edit;

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