namespace Identity.Pages.Admin.Roles.Details;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class IndexModel : PageModel
{
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public IndexModel(RoleManager<IdentityRole<Guid>> roleManager) => _roleManager = roleManager;

    public IdentityRole<Guid> AppRole { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role is null)
        {
            return NotFound();
        }

        AppRole = role;
        return Page();
    }
}