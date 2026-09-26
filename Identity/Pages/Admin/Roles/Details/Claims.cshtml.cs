namespace Identity.Pages.Admin.Roles.Details;

using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

public class Claims : ResourcesBase<Claim>
{
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public Claims(RoleManager<IdentityRole<Guid>> roleManager) => _roleManager = roleManager;

    public IdentityRole<Guid> AppRole { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role is null)
        {
            return NotFound();
        }

        AppRole = role;
        Resources = await _roleManager.GetClaimsAsync(role);
        return Page();
    }
}
