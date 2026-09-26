namespace Identity.Pages.Admin.Users.Details;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

public class Roles : ResourcesBase<string>
{
    private readonly UserManager<IdentityUser<Guid>> _userManager;

    public Roles(UserManager<IdentityUser<Guid>> userManager) => _userManager = userManager;

    public IdentityUser<Guid> AppUser { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        AppUser = user;
        Resources = await _userManager.GetRolesAsync(user);
        return Page();
    }
}
