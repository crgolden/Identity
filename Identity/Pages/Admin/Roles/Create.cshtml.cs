namespace Identity.Pages.Admin.Roles;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class CreateModel : PageModel
{
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public CreateModel(RoleManager<IdentityRole<Guid>> roleManager) => _roleManager = roleManager;

    [BindProperty]
    public string RoleName { get; set; } = Empty;

    public IActionResult OnGet() => Page();

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