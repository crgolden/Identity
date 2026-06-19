namespace Identity.Pages.Admin.Roles.Details;

using Identity.Pages.Admin.Roles;
using Microsoft.AspNetCore.Identity;

/// <summary>Shows users in a role.</summary>
public class UsersModel : RoleUsersModelBase
{
    /// <summary>Initializes a new instance of the <see cref="UsersModel"/> class.</summary>
    public UsersModel(RoleManager<IdentityRole<Guid>> roleManager, UserManager<IdentityUser<Guid>> userManager)
        : base(roleManager, userManager)
    {
    }
}
