namespace Identity.Pages.Admin.Users;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class IndexModel : PageModel
{
    private readonly UserManager<IdentityUser<Guid>> _userManager;

    public IndexModel(UserManager<IdentityUser<Guid>> userManager) => _userManager = userManager;

    public IList<IdentityUser<Guid>> Users { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Users = await _userManager.Users.OrderBy(u => u.UserName).ToListAsync();
    }
}