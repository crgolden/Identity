namespace Identity.Pages.Account.Manage;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using static System.Buffers.Text.Base64Url;

public class RenamePasskey : PageModel
{
    internal const string IdRouteValueName = "id";

    internal const string InvalidCredentialIdFormatMessage = "The specified passkey ID had an invalid format.";

    internal const string PasskeyUpdatedMessage = "The passkey was updated.";

    private readonly UserManager<IdentityUser<Guid>> _userManager;

    public RenamePasskey(UserManager<IdentityUser<Guid>> userManager) => _userManager = userManager;

    [BindProperty]
    public InputModel Input { get; set; } = new InputModel();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound(UserMessages.UnableToLoadUser(_userManager.GetUserId(User)));
        }

        byte[] credentialId;
        try
        {
            credentialId = DecodeFromChars(id);
        }
        catch (FormatException)
        {
            StatusMessage = InvalidCredentialIdFormatMessage;
            return RedirectToPage(PageRoutes.SiblingPasskeys);
        }

        var passkey = await _userManager.GetPasskeyAsync(user, credentialId);
        if (passkey is null)
        {
            return NotFound(UserMessages.UnableToLoadPasskey(_userManager.GetUserId(User)));
        }

        Input = new InputModel
        {
            CredentialId = id,
            Name = passkey.Name
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound(UserMessages.UnableToLoadUser(_userManager.GetUserId(User)));
        }

        byte[] credentialId;
        try
        {
            credentialId = DecodeFromChars(Input.CredentialId);
        }
        catch (FormatException)
        {
            StatusMessage = InvalidCredentialIdFormatMessage;
            return RedirectToPage(PageRoutes.SiblingPasskeys);
        }

        var passkey = await _userManager.GetPasskeyAsync(user, credentialId);
        if (passkey is null)
        {
            return NotFound(UserMessages.UnableToLoadPasskey(_userManager.GetUserId(User)));
        }

        passkey.Name = Input.Name;
        var result = await _userManager.AddOrUpdatePasskeyAsync(user, passkey);
        if (!result.Succeeded)
        {
            var userId = await _userManager.GetUserIdAsync(user);
            throw new InvalidOperationException($"Unexpected error occurred removing passkey for user with ID '{userId}'.");
        }

        StatusMessage = PasskeyUpdatedMessage;
        return RedirectToPage(PageRoutes.SiblingPasskeys);
    }

    public class InputModel
    {
        public string? CredentialId { get; set; }

        public string? Name { get; set; }
    }
}
