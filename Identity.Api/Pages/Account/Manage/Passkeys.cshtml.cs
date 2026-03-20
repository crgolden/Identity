namespace Identity.Pages.Account.Manage;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using static System.Buffers.Text.Base64Url;

/// <summary>Page model for the Passkeys management page.</summary>
public class PasskeysModel : PageModel
{
    private readonly UserManager<IdentityUser<Guid>> _userManager;
    private readonly SignInManager<IdentityUser<Guid>> _signInManager;

    public PasskeysModel(UserManager<IdentityUser<Guid>> userManager, SignInManager<IdentityUser<Guid>> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public IList<UserPasskeyInfo> CurrentPasskeys { get; set; } = new List<UserPasskeyInfo>();

    [BindProperty]
    public InputModel Input { get; set; } = new InputModel();

    [TempData]
    public string? StatusMessage { get; set; }

    /// <summary>Handles the GET request to display the passkeys management page.</summary>
    /// <returns>A task that resolves to the page result or a redirect.</returns>
    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        CurrentPasskeys = await _userManager.GetPasskeysAsync(user);
        return Page();
    }

    /// <summary>Handles the POST request to rename or delete a passkey.</summary>
    /// <returns>A task that resolves to the page result or a redirect.</returns>
    public async Task<IActionResult> OnPostUpdatePasskeyAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        if (IsNullOrWhiteSpace(Input?.CredentialId))
        {
            StatusMessage = "Could not find the passkey.";
            return RedirectToPage();
        }

        byte[] credentialId;
        try
        {
            credentialId = DecodeFromChars(Input.CredentialId);
        }
        catch (FormatException)
        {
            StatusMessage = "The specified passkey ID had an invalid format.";
            return RedirectToPage();
        }

        switch (Input.Action)
        {
            case "rename":
                return RedirectToPage("./RenamePasskey", new { id = Input.CredentialId });
            case "delete":
                return await DeletePasskey(user, credentialId);
            default:
                StatusMessage = "Unknown action.";
                return RedirectToPage();
        }
    }

    /// <summary>Handles the POST request to add a new passkey to the user's account.</summary>
    /// <returns>A task that resolves to the page result or a redirect.</returns>
    public async Task<IActionResult> OnPostAddPasskeyAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        if (!IsNullOrWhiteSpace(Input?.Passkey?.Error))
        {
            StatusMessage = $"Could not add a passkey: {Input.Passkey.Error}";
            return RedirectToPage();
        }

        if (IsNullOrWhiteSpace(Input?.Passkey?.CredentialJson))
        {
            StatusMessage = "The browser did not provide a passkey.";
            return RedirectToPage();
        }

        var attestationResult = await _signInManager.PerformPasskeyAttestationAsync(Input.Passkey.CredentialJson);
        if (!attestationResult.Succeeded)
        {
            StatusMessage = $"Could not add the passkey: {attestationResult.Failure.Message}.";
            return RedirectToPage();
        }

        var setPasskeyResult = await _userManager.AddOrUpdatePasskeyAsync(user, attestationResult.Passkey);
        if (!setPasskeyResult.Succeeded)
        {
            StatusMessage = "The passkey could not be added to your account.";
            return RedirectToPage();
        }

        StatusMessage = "The passkey was added to your account. You can now use it to sign in. Give it an easy to remember name.";
        return RedirectToPage("./RenamePasskey", new { id = EncodeToString(attestationResult.Passkey.CredentialId) });
    }

    private async Task<IActionResult> DeletePasskey(IdentityUser<Guid> user, byte[] credentialId)
    {
        var result = await _userManager.RemovePasskeyAsync(user, credentialId);
        if (!result.Succeeded)
        {
            var userId = await _userManager.GetUserIdAsync(user);
            throw new InvalidOperationException($"Unexpected error occurred removing passkey for user with ID '{userId}'.");
        }

        StatusMessage = "The passkey was removed.";
        return RedirectToPage();
    }

    /// <summary>Provides the form input values bound from the request.</summary>
    public class InputModel
    {
        public string? CredentialId { get; set; }

        public string? Action { get; set; }

        public PasskeyInputModel? Passkey { get; set; }
    }
}
