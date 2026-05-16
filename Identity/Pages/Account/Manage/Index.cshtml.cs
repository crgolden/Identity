namespace Identity.Pages.Account.Manage;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>Page model for the Account management index page.</summary>
public class IndexModel : PageModel
{
    private readonly UserManager<IdentityUser<Guid>> _userManager;
    private readonly SignInManager<IdentityUser<Guid>> _signInManager;

    public IndexModel(
        UserManager<IdentityUser<Guid>> userManager,
        SignInManager<IdentityUser<Guid>> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public string? Username { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new InputModel();

    /// <summary>Handles the GET request to display the account management index page.</summary>
    /// <returns>A task that resolves to the page result or a redirect.</returns>
    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        await LoadAsync(user);
        return Page();
    }

    /// <summary>Handles the POST request to update the user's profile information.</summary>
    /// <returns>A task that resolves to the page result or a redirect.</returns>
    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync(user);
            return Page();
        }

        var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
        if (!IsNullOrWhiteSpace(phoneNumber) && !IsNullOrWhiteSpace(Input?.PhoneNumber) && !string.Equals(Input.PhoneNumber, phoneNumber))
        {
            var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
            if (!setPhoneResult.Succeeded)
            {
                StatusMessage = "Unexpected error when trying to set phone number.";
                return RedirectToPage();
            }
        }

        await _signInManager.RefreshSignInAsync(user);
        StatusMessage = "Your profile has been updated";
        return RedirectToPage();
    }

    private async Task LoadAsync(IdentityUser<Guid> user)
    {
        var userName = await _userManager.GetUserNameAsync(user);
        var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
        Username = userName;
        Input = new InputModel
        {
            PhoneNumber = phoneNumber
        };
    }

    /// <summary>Provides the form input values bound from the request.</summary>
    public class InputModel
    {
        [Phone]
        [Display(Name = "Phone number")]
        public string? PhoneNumber { get; set; }
    }
}
