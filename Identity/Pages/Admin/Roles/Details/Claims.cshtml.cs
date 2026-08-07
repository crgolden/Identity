namespace Identity.Pages.Admin.Roles.Details;

using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class ClaimsModel : PageModel
{
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public ClaimsModel(RoleManager<IdentityRole<Guid>> roleManager) => _roleManager = roleManager;

    public IdentityRole<Guid> AppRole { get; private set; } = new();

    public IList<Claim> Claims { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role is null)
        {
            return NotFound();
        }

        AppRole = role;
        Claims = await _roleManager.GetClaimsAsync(role);
        return Page();
    }
}