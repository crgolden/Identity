namespace Identity.Pages.Admin.Roles.Edit;

using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>Edits role claims.</summary>
public class ClaimsModel : PageModel
{
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    /// <summary>Initializes a new instance of the <see cref="ClaimsModel"/> class.</summary>
    public ClaimsModel(RoleManager<IdentityRole<Guid>> roleManager) => _roleManager = roleManager;

    /// <summary>Gets the role name for display.</summary>
    public string RoleName { get; private set; } = string.Empty;

    /// <summary>Gets or sets the claims.</summary>
    [BindProperty]
    public List<Claim> Claims { get; set; } = [];

    /// <summary>Loads the role and its claims.</summary>
    public async Task<IActionResult> OnGetAsync(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role is null)
        {
            return NotFound();
        }

        RoleName = role.Name ?? string.Empty;
        Claims = (await _roleManager.GetClaimsAsync(role)).ToList();
        return Page();
    }

    /// <summary>Replaces all claims with the posted set.</summary>
    public async Task<IActionResult> OnPostAsync(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role is null)
        {
            return NotFound();
        }

        var existing = await _roleManager.GetClaimsAsync(role);
        foreach (var claim in existing)
        {
            await _roleManager.RemoveClaimAsync(role, claim);
        }

        foreach (var claim in Claims)
        {
            await _roleManager.AddClaimAsync(role, claim);
        }

        return RedirectToPage("/Admin/Roles/Details/Claims", new { id });
    }
}
