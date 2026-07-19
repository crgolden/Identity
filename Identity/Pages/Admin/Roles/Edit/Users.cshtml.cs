namespace Identity.Pages.Admin.Roles.Edit;

using Roles;
using Microsoft.AspNetCore.Identity;

/// <summary>Shows users in a role (read-only from the role edit perspective).</summary>
public class UsersModel : RoleUsersModelBase
{
    /// <summary>Initializes a new instance of the <see cref="UsersModel"/> class.</summary>
    public UsersModel(RoleManager<IdentityRole<Guid>> roleManager, UserManager<IdentityUser<Guid>> userManager)
        : base(roleManager, userManager)
    {
    }
}
