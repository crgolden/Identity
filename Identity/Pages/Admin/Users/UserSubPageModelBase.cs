namespace Identity.Pages.Admin.Users;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;

public abstract class UserSubPageModelBase : PageModel
{
    protected UserSubPageModelBase(UserManager<IdentityUser<Guid>> userManager) => UserManager = userManager;

    public IdentityUser<Guid> AppUser { get; protected set; } = new();

    protected UserManager<IdentityUser<Guid>> UserManager { get; }

    protected async Task<bool> TryLoadUserAsync(string id)
    {
        var user = await UserManager.FindByIdAsync(id);
        if (user is null)
        {
            return false;
        }

        AppUser = user;
        return true;
    }
}