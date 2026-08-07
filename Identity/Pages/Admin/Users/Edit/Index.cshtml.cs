namespace Identity.Pages.Admin.Users.Edit;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class IndexModel : PageModel
{
    private readonly UserManager<IdentityUser<Guid>> _userManager;

    public IndexModel(UserManager<IdentityUser<Guid>> userManager) => _userManager = userManager;

    [BindProperty]
    public IdentityUser<Guid> AppUser { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        AppUser = user;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        user.UserName = AppUser.UserName;
        user.Email = AppUser.Email;
        user.PhoneNumber = AppUser.PhoneNumber;
        user.LockoutEnabled = AppUser.LockoutEnabled;
        user.EmailConfirmed = AppUser.EmailConfirmed;
        await _userManager.UpdateAsync(user);
        return RedirectToPage("/Admin/Users/Details/Index", new { id });
    }
}