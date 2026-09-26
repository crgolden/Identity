namespace Identity.Pages.Admin.Users.Details;

using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

public class Claims : ResourcesBase<Claim>
{
    private readonly UserManager<IdentityUser<Guid>> _userManager;

    public Claims(UserManager<IdentityUser<Guid>> userManager) => _userManager = userManager;

    public IdentityUser<Guid> AppUser { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        AppUser = user;
        Resources = await _userManager.GetClaimsAsync(user);
        return Page();
    }
}
