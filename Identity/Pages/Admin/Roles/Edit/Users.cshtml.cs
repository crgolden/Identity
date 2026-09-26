namespace Identity.Pages.Admin.Roles.Edit;

using Identity.Pages.Admin.Roles;
using Microsoft.AspNetCore.Identity;

public class Users : RoleUsersModelBase
{
    public Users(RoleManager<IdentityRole<Guid>> roleManager, UserManager<IdentityUser<Guid>> userManager)
        : base(roleManager, userManager)
    {
    }
}
