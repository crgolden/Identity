namespace Identity.Pages.Account.Manage;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class Index : PageModel
{
    internal const string PhoneNumberUpdateFailedMessage =
        "Unexpected error when trying to set phone number.";

    internal const string ProfileUpdatedMessage =
        "Your profile has been updated";

    private readonly UserManager<IdentityUser<Guid>> _userManager;
    private readonly SignInManager<IdentityUser<Guid>> _signInManager;

    public Index(
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

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound(UserMessages.UnableToLoadUser(_userManager.GetUserId(User)));
        }

        await LoadAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound(UserMessages.UnableToLoadUser(_userManager.GetUserId(User)));
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync(user);
            return Page();
        }

        var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
        if (!IsNullOrWhiteSpace(phoneNumber)
            && !IsNullOrWhiteSpace(Input.PhoneNumber)
            && !string.Equals(Input.PhoneNumber, phoneNumber, StringComparison.Ordinal))
        {
            var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
            if (!setPhoneResult.Succeeded)
            {
                StatusMessage = PhoneNumberUpdateFailedMessage;
                return RedirectToPage();
            }
        }

        await _signInManager.RefreshSignInAsync(user);
        StatusMessage = ProfileUpdatedMessage;
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

    public class InputModel
    {
        [Phone]
        [Display(Name = "Phone number")]
        public string? PhoneNumber { get; set; }
    }
}
