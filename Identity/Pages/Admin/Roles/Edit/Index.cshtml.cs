namespace Identity.Pages.Admin.Roles.Edit;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>Edits a role's name.</summary>
public class IndexModel : PageModel
{
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    /// <summary>Initializes a new instance of the <see cref="IndexModel"/> class.</summary>
    public IndexModel(RoleManager<IdentityRole<Guid>> roleManager) => _roleManager = roleManager;

    /// <summary>Gets or sets the role being edited.</summary>
    [BindProperty]
    public IdentityRole<Guid> AppRole { get; set; } = new();

    /// <summary>Loads the role for editing.</summary>
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

    /// <summary>Saves role name changes.</summary>
    public async Task<IActionResult> OnPostAsync(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        role.Name = AppRole.Name;
        await _roleManager.UpdateAsync(role);
        return RedirectToPage("/Admin/Roles/Details/Index", new { id });
    }
}
