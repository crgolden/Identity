namespace Identity.Pages.Admin.Users;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Lists all users.</summary>
public class IndexModel : PageModel
{
    private readonly UserManager<IdentityUser<Guid>> _userManager;

    /// <summary>Initializes a new instance of the <see cref="IndexModel"/> class.</summary>
    public IndexModel(UserManager<IdentityUser<Guid>> userManager) => _userManager = userManager;

    /// <summary>Gets the users.</summary>
    public IList<IdentityUser<Guid>> Users { get; private set; } = [];

    /// <summary>Loads all users ordered by username.</summary>
    public async Task OnGetAsync()
    {
        Users = await _userManager.Users.OrderBy(u => u.UserName).ToListAsync();
    }
}
