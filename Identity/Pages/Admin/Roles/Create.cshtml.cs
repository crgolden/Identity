namespace Identity.Pages.Admin.Roles;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>Creates a new role.</summary>
public class CreateModel : PageModel
{
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    /// <summary>Initializes a new instance of the <see cref="CreateModel"/> class.</summary>
    public CreateModel(RoleManager<IdentityRole<Guid>> roleManager) => _roleManager = roleManager;

    /// <summary>Gets or sets the role name.</summary>
    [BindProperty]
    public string RoleName { get; set; } = Empty;

    /// <summary>Returns the create page.</summary>
    public IActionResult OnGet() => Page();

    /// <summary>Creates the role.</summary>
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var role = new IdentityRole<Guid>(RoleName);
        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(Empty, error.Description);
            }

            return Page();
        }

        return RedirectToPage("./Details/Index", new { id = role.Id });
    }
}
