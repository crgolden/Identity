namespace Identity.Pages.Admin.Roles.Details;

using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>Shows role claims.</summary>
public class ClaimsModel : PageModel
{
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    /// <summary>Initializes a new instance of the <see cref="ClaimsModel"/> class.</summary>
    public ClaimsModel(RoleManager<IdentityRole<Guid>> roleManager) => _roleManager = roleManager;

    /// <summary>Gets the role.</summary>
    public IdentityRole<Guid> AppRole { get; private set; } = new();

    /// <summary>Gets the claims.</summary>
    public IList<Claim> Claims { get; private set; } = [];

    /// <summary>Loads role claims.</summary>
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
