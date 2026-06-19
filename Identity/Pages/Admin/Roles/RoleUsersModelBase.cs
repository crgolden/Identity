namespace Identity.Pages.Admin.Roles;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>Base model for role user sub-pages.</summary>
public abstract class RoleUsersModelBase : PageModel
{
    /// <summary>Initializes a new instance of the <see cref="RoleUsersModelBase"/> class.</summary>
    protected RoleUsersModelBase(RoleManager<IdentityRole<Guid>> roleManager, UserManager<IdentityUser<Guid>> userManager)
    {
        RoleManager = roleManager;
        UserManager = userManager;
    }

    /// <summary>Gets the role.</summary>
    public IdentityRole<Guid> AppRole { get; private set; } = new();

    /// <summary>Gets the users in the role.</summary>
    public IList<IdentityUser<Guid>> Users { get; private set; } = [];

    /// <summary>Gets the role manager.</summary>
    protected RoleManager<IdentityRole<Guid>> RoleManager { get; }

    /// <summary>Gets the user manager.</summary>
    protected UserManager<IdentityUser<Guid>> UserManager { get; }

    /// <summary>Loads users in the role.</summary>
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
