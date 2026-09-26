namespace Identity.Pages.Account;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

[AllowAnonymous]
public class ResetPassword : PageModel
{
    internal const string CodeRequiredMessage =
        "A code must be supplied for password reset.";

    private readonly UserManager<IdentityUser<Guid>> _userManager;

    public ResetPassword(UserManager<IdentityUser<Guid>> userManager)
    {
        _userManager = userManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new InputModel();

    public IActionResult OnGet(string? code = null, string? email = null)
    {
        if (IsNullOrWhiteSpace(code))
        {
            return BadRequest(CodeRequiredMessage);
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

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid || IsNullOrWhiteSpace(Input.Email) || IsNullOrWhiteSpace(Input.Code) || IsNullOrWhiteSpace(Input.Password))
        {
            return Page();
        }

        var user = await _userManager.FindByEmailAsync(Input.Email);
        if (user is null)
        {
            return RedirectToPage(PageRoutes.SiblingResetPasswordConfirmation);
        }

        var result = await _userManager.ResetPasswordAsync(user, Input.Code, Input.Password);
        if (result.Succeeded)
        {
            return RedirectToPage(PageRoutes.SiblingResetPasswordConfirmation);
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(Empty, error.Description);
        }

        return Page();
    }

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
