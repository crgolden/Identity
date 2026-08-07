namespace Identity.Pages.Admin.Roles;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public abstract class RoleUsersModelBase : PageModel
{
    protected RoleUsersModelBase(RoleManager<IdentityRole<Guid>> roleManager, UserManager<IdentityUser<Guid>> userManager)
    {
        RoleManager = roleManager;
        UserManager = userManager;
    }

    public IdentityRole<Guid> AppRole { get; private set; } = new();

    public IList<IdentityUser<Guid>> Users { get; private set; } = [];

    protected RoleManager<IdentityRole<Guid>> RoleManager { get; }

    protected UserManager<IdentityUser<Guid>> UserManager { get; }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var role = await RoleManager.FindByIdAsync(id);
        if (role is null)
        {
            return NotFound();
        }

        AppRole = role;
        Users = await UserManager.GetUsersInRoleAsync(role.Name!);
        return Page();
    }
}