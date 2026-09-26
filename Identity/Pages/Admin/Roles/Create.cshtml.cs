namespace Identity.Pages.Admin.Roles;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class Create : PageModel
{
    internal const string RoleNameRequiredMessage = "Role name is required.";

    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public Create(RoleManager<IdentityRole<Guid>> roleManager) => _roleManager = roleManager;

    [BindProperty]
    public string? RoleName { get; set; }

    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var roleName = RoleName;
        if (IsNullOrWhiteSpace(roleName))
        {
            ModelState.AddModelError(nameof(RoleName), RoleNameRequiredMessage);
            return Page();
        }

        var role = new IdentityRole<Guid>(roleName);
        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(Empty, error.Description);
            }

            return Page();
        }

        return RedirectToPage(PageRoutes.SiblingDetailsIndex, new { id = role.Id });
    }
}
