namespace Identity.Pages.Admin.Users.Details;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class RolesModel : PageModel
{
    private readonly UserManager<IdentityUser<Guid>> _userManager;

    public RolesModel(UserManager<IdentityUser<Guid>> userManager) => _userManager = userManager;

    public IdentityUser<Guid> AppUser { get; private set; } = new();

    public IList<string> Roles { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        AppUser = user;
        Roles = await _userManager.GetRolesAsync(user);
        return Page();
    }
}