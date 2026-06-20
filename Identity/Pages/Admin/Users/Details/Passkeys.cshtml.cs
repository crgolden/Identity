namespace Identity.Pages.Admin.Users.Details;

using Identity.Pages.Admin.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

/// <summary>Shows user passkeys.</summary>
public class PasskeysModel : UserSubPageModelBase
{
    /// <summary>Initializes a new instance of the <see cref="PasskeysModel"/> class.</summary>
    public PasskeysModel(UserManager<IdentityUser<Guid>> userManager)
        : base(userManager)
    {
    }

    /// <summary>Gets the user's passkeys.</summary>
    public IList<UserPasskeyInfo> Passkeys { get; private set; } = [];

    /// <summary>Loads the user's passkeys.</summary>
    public async Task<IActionResult> OnGetAsync(string id)
    {
        if (!await TryLoadUserAsync(id))
        {
            return NotFound();
        }

        Passkeys = await UserManager.GetPasskeysAsync(AppUser);
        return Page();
    }
}
