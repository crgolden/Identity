namespace Identity.Pages.Admin.Users;

using Microsoft.AspNetCore.Identity;

public abstract class UserResourcesBase<TResource> : UserSubPageModelBase
{
    protected UserResourcesBase(UserManager<IdentityUser<Guid>> userManager)
        : base(userManager)
    {
    }

    public IList<TResource> Resources { get; protected set; } = [];
}
