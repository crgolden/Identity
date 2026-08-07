namespace Identity.Pages.Admin.Roles;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class IndexModel : PageModel
{
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public IndexModel(RoleManager<IdentityRole<Guid>> roleManager) => _roleManager = roleManager;

    public IList<IdentityRole<Guid>> Roles { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Roles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
    }
}