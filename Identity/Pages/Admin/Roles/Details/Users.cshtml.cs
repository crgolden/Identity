namespace Identity.Pages.Admin.Roles.Details;

using Microsoft.AspNetCore.Identity;
using Roles;

public class UsersModel : RoleUsersModelBase
{
    public UsersModel(RoleManager<IdentityRole<Guid>> roleManager, UserManager<IdentityUser<Guid>> userManager)
        : base(roleManager, userManager)
    {
    }
}