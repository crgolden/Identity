namespace Identity.Pages.Account.Manage;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class PersonalData : PageModel
{
    private readonly UserManager<IdentityUser<Guid>> _userManager;

    public PersonalData(UserManager<IdentityUser<Guid>> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IActionResult> OnGet()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound(UserMessages.UnableToLoadUser(_userManager.GetUserId(User)));
        }

        return Page();
    }
}
