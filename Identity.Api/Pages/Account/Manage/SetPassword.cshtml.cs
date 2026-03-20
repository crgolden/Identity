namespace Identity.Pages.Account.Manage;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>Page model for the Set Password page.</summary>
public class SetPasswordModel : PageModel
{
    private readonly UserManager<IdentityUser<Guid>> _userManager;
    private readonly SignInManager<IdentityUser<Guid>> _signInManager;

    public SetPasswordModel(
        UserManager<IdentityUser<Guid>> userManager,
        SignInManager<IdentityUser<Guid>> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new InputModel();

    [TempData]
    public string? StatusMessage { get; set; }

    /// <summary>Handles the GET request to display the set password form.</summary>
    /// <returns>A task that resolves to the page result or a redirect.</returns>
    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        var hasPassword = await _userManager.HasPasswordAsync(user);
        if (hasPassword)
        {
            return RedirectToPage("./ChangePassword");
        }

        return Page();
    }

    /// <summary>Handles the POST request to set a password for accounts that only have external logins.</summary>
    /// <returns>A task that resolves to the page result or a redirect.</returns>
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid || IsNullOrWhiteSpace(Input?.NewPassword))
        {
            return Page();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        var addPasswordResult = await _userManager.AddPasswordAsync(user, Input.NewPassword);
        if (!addPasswordResult.Succeeded)
        {
            foreach (var error in addPasswordResult.Errors)
            {
                ModelState.AddModelError(Empty, error.Description);
            }

            return Page();
        }

        await _signInManager.RefreshSignInAsync(user);
        StatusMessage = "Your password has been set.";
        return RedirectToPage();
    }

    /// <summary>Provides the form input values bound from the request.</summary>
    public class InputModel
    {
        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string? ConfirmPassword { get; set; }
    }
}
