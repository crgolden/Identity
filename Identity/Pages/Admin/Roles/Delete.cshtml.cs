namespace Identity.Pages.Admin.Roles;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>Deletes a role.</summary>
public class DeleteModel : PageModel
{
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    /// <summary>Initializes a new instance of the <see cref="DeleteModel"/> class.</summary>
    public DeleteModel(RoleManager<IdentityRole<Guid>> roleManager) => _roleManager = roleManager;

    /// <summary>Gets the role to delete.</summary>
    public IdentityRole<Guid> AppRole { get; private set; } = new();

    /// <summary>Loads the role for deletion confirmation.</summary>
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

    /// <summary>Deletes the role.</summary>
    public async Task<IActionResult> OnPostAsync(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role is null)
        {
            return NotFound();
        }

        await _roleManager.DeleteAsync(role);
        return RedirectToPage("./Index");
    }
}
