namespace Identity.Pages.Admin.Users.Details;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Users;

public class PasskeysModel : UserSubPageModelBase
{
    public PasskeysModel(UserManager<IdentityUser<Guid>> userManager)
        : base(userManager)
    {
    }

    public IList<UserPasskeyInfo> Passkeys { get; private set; } = [];

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