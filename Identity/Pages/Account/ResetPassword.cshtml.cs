namespace Identity.Pages.Account;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>Page model for the Reset Password page.</summary>
[AllowAnonymous]
public class ResetPasswordModel : PageModel
{
    private readonly UserManager<IdentityUser<Guid>> _userManager;

    public ResetPasswordModel(UserManager<IdentityUser<Guid>> userManager)
    {
        _userManager = userManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new InputModel();

    /// <summary>Handles the GET request to display the password reset form.</summary>
    /// <param name="code">The base64url-encoded password reset token.</param>
    /// <param name="email">The email address of the account to reset.</param>
    /// <returns>A redirect or page result.</returns>
    public IActionResult OnGet(string? code = null, string? email = null)
    {
        if (IsNullOrWhiteSpace(code))
        {
            return BadRequest("A code must be supplied for password reset.");
        }

        var bytes = Base64UrlDecode(code);
        code = UTF8.GetString(bytes);
        Input = new InputModel
        {
            Code = code,
            Email = email
        };
        return Page();
    }

    /// <summary>Handles the POST request to reset the user's password.</summary>
    /// <returns>A task that resolves to the page result or a redirect.</returns>
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid || IsNullOrWhiteSpace(Input.Email) || IsNullOrWhiteSpace(Input.Code) || IsNullOrWhiteSpace(Input.Password))
        {
            return Page();
        }

        var user = await _userManager.FindByEmailAsync(Input.Email);
        if (user is null)
        {
            return RedirectToPage("./ResetPasswordConfirmation");
        }

        var result = await _userManager.ResetPasswordAsync(user, Input.Code, Input.Password);
        if (result.Succeeded)
        {
            return RedirectToPage("./ResetPasswordConfirmation");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(Empty, error.Description);
        }

        return Page();
    }

    /// <summary>Provides the form input values bound from the request.</summary>
    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string? Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string? ConfirmPassword { get; set; }

        [Required]
        public string? Code { get; set; }
    }
}
