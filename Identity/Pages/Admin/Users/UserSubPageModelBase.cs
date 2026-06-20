namespace Identity.Pages.Admin.Users;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>Base model for user admin sub-pages.</summary>
public abstract class UserSubPageModelBase : PageModel
{
    /// <summary>Initializes a new instance of the <see cref="UserSubPageModelBase"/> class.</summary>
    protected UserSubPageModelBase(UserManager<IdentityUser<Guid>> userManager) => UserManager = userManager;

    /// <summary>Gets the user.</summary>
    public IdentityUser<Guid> AppUser { get; protected set; } = new();

    /// <summary>Gets the user manager.</summary>
    protected UserManager<IdentityUser<Guid>> UserManager { get; }

    /// <summary>Loads the user by ID; sets <see cref="AppUser"/> and returns true, or returns false when not found.</summary>
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
