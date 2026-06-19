namespace Identity.Pages.Admin.Roles;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Lists all roles.</summary>
public class IndexModel : PageModel
{
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    /// <summary>Initializes a new instance of the <see cref="IndexModel"/> class.</summary>
    public IndexModel(RoleManager<IdentityRole<Guid>> roleManager) => _roleManager = roleManager;

    /// <summary>Gets the roles.</summary>
    public IList<IdentityRole<Guid>> Roles { get; private set; } = [];

    /// <summary>Loads all roles ordered by name.</summary>
    public async Task OnGetAsync()
    {
        Roles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
    }
}
