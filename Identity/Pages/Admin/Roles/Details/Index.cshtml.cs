namespace Identity.Pages.Admin.Roles.Details;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>Shows role details.</summary>
public class IndexModel : PageModel
{
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    /// <summary>Initializes a new instance of the <see cref="IndexModel"/> class.</summary>
    public IndexModel(RoleManager<IdentityRole<Guid>> roleManager) => _roleManager = roleManager;

    /// <summary>Gets the role.</summary>
    public IdentityRole<Guid> AppRole { get; private set; } = new();

    /// <summary>Loads the role.</summary>
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
