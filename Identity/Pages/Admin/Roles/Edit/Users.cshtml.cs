namespace Identity.Pages.Admin.Roles.Edit;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>Shows users in a role (read-only from the role edit perspective).</summary>
public class UsersModel : PageModel
{
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly UserManager<IdentityUser<Guid>> _userManager;

    /// <summary>Initializes a new instance of the <see cref="UsersModel"/> class.</summary>
    public UsersModel(RoleManager<IdentityRole<Guid>> roleManager, UserManager<IdentityUser<Guid>> userManager)
    {
        _roleManager = roleManager;
        _userManager = userManager;
    }

    /// <summary>Gets the role.</summary>
    public IdentityRole<Guid> AppRole { get; private set; } = new();

    /// <summary>Gets the users in the role.</summary>
    public IList<IdentityUser<Guid>> Users { get; private set; } = [];

    /// <summary>Loads users in the role.</summary>
    public async Task<IActionResult> OnGetAsync(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role is null)
        {
            return NotFound();
        }

        AppRole = role;
        Users = await _userManager.GetUsersInRoleAsync(role.Name!);
        return Page();
    }
}
