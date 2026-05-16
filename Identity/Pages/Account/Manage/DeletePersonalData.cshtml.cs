namespace Identity.Pages.Account.Manage;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>Page model for the Delete Personal Data page.</summary>
public class DeletePersonalDataModel : PageModel
{
    private readonly UserManager<IdentityUser<Guid>> _userManager;
    private readonly SignInManager<IdentityUser<Guid>> _signInManager;
    private readonly ILogger<DeletePersonalDataModel> _logger;

    public DeletePersonalDataModel(
        UserManager<IdentityUser<Guid>> userManager,
        SignInManager<IdentityUser<Guid>> signInManager,
        ILogger<DeletePersonalDataModel> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new InputModel();

    public bool RequirePassword { get; set; }

    /// <summary>Handles the GET request to display the delete personal data confirmation form.</summary>
    /// <returns>A task that resolves to the page result or a redirect.</returns>
    public async Task<IActionResult> OnGet()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        RequirePassword = await _userManager.HasPasswordAsync(user);
        return Page();
    }

    /// <summary>Handles the POST request to permanently delete the user's account and personal data.</summary>
    /// <returns>A task that resolves to the page result or a redirect.</returns>
    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        RequirePassword = await _userManager.HasPasswordAsync(user);
        if (RequirePassword && (IsNullOrWhiteSpace(Input?.Password) || !await _userManager.CheckPasswordAsync(user, Input.Password)))
        {
            ModelState.AddModelError(Empty, "Incorrect password.");
            return Page();
        }

        var result = await _userManager.DeleteAsync(user);
        var userId = await _userManager.GetUserIdAsync(user);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Unexpected error occurred deleting user.");
        }

        await _signInManager.SignOutAsync();

        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("User with ID '{UserId}' deleted themselves.", userId);
        }

        return Redirect("~/");
    }

    /// <summary>Provides the form input values bound from the request.</summary>
    public class InputModel
    {
        [Required]
        [DataType(DataType.Password)]
        public string? Password { get; set; }
    }
}
