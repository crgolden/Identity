namespace Identity.Pages.Admin.Roles.Details;

using Identity.Pages.Admin.Roles;
using Microsoft.AspNetCore.Identity;

public class Users : RoleUsersModelBase
{
    internal const string PageName = "/Admin/Roles/Details/Users";

    public Users(RoleManager<IdentityRole<Guid>> roleManager, UserManager<IdentityUser<Guid>> userManager)
        : base(roleManager, userManager)
    {
    }
}
