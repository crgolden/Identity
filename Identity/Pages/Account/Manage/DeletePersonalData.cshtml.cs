namespace Identity.Pages.Account.Manage;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class DeletePersonalData : PageModel
{
    internal const string IncorrectPasswordMessage =
        "Incorrect password.";

    internal const string DeleteFailedMessage =
        "Unexpected error occurred deleting user.";

    private readonly UserManager<IdentityUser<Guid>> _userManager;
    private readonly SignInManager<IdentityUser<Guid>> _signInManager;

    public DeletePersonalData(
        UserManager<IdentityUser<Guid>> userManager,
        SignInManager<IdentityUser<Guid>> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new InputModel();

    public bool RequirePassword { get; set; }

    public async Task<IActionResult> OnGet()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound(UserMessages.UnableToLoadUser(_userManager.GetUserId(User)));
        }

        RequirePassword = await _userManager.HasPasswordAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound(UserMessages.UnableToLoadUser(_userManager.GetUserId(User)));
        }

        RequirePassword = await _userManager.HasPasswordAsync(user);
        if (RequirePassword && (IsNullOrWhiteSpace(Input.Password) || !await _userManager.CheckPasswordAsync(user, Input.Password)))
        {
            ModelState.AddModelError(Empty, IncorrectPasswordMessage);
            return Page();
        }

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(DeleteFailedMessage);
        }

        await _signInManager.SignOutAsync();
        return Redirect(PageRoutes.ContentRoot);
    }

    public class InputModel
    {
        [Required]
        [DataType(DataType.Password)]
        public string? Password { get; set; }
    }
}
