namespace Identity.Pages.Admin.Roles.Edit;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class IndexModel : PageModel
{
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public IndexModel(RoleManager<IdentityRole<Guid>> roleManager) => _roleManager = roleManager;

    [BindProperty]
    public IdentityRole<Guid> AppRole { get; set; } = new();

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