namespace Identity.Pages.Account.Manage;

using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using static System.Buffers.Text.Base64Url;

public class PasskeysModel : PageModel
{
    internal const string RenameAction = "rename";

    internal const string DeleteAction = "delete";

    internal const string PasskeyNotFoundMessage = "Could not find the passkey.";

    internal const string UnknownActionMessage = "Unknown action.";

    internal const string BrowserProvidedNoPasskeyMessage = "The browser did not provide a passkey.";

    internal const string PasskeyNotAddedMessage = "The passkey could not be added to your account.";

    internal const string PasskeyAddedMessage =
        "The passkey was added to your account. You can now use it to sign in. Give it an easy to remember name.";

    internal const string PasskeyRemovedMessage = "The passkey was removed.";

    internal const string BrowserErrorMessageFormat = "Could not add a passkey: {0}";

    internal const string AttestationFailedMessageFormat = "Could not add the passkey: {0}.";

    internal const string RegisterActivityName = "identity.passkey.register";

    internal const string DeleteActivityName = "identity.passkey.delete";

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

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound(UserMessages.UnableToLoadUser(_userManager.GetUserId(User)));
        }

        CurrentPasskeys = await _userManager.GetPasskeysAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostUpdatePasskeyAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound(UserMessages.UnableToLoadUser(_userManager.GetUserId(User)));
        }

        if (IsNullOrWhiteSpace(Input?.CredentialId))
        {
            StatusMessage = PasskeyNotFoundMessage;
            return RedirectToPage();
        }

        byte[] credentialId;
        try
        {
            credentialId = DecodeFromChars(Input.CredentialId);
        }
        catch (FormatException)
        {
            StatusMessage = RenamePasskeyModel.InvalidCredentialIdFormatMessage;
            return RedirectToPage();
        }

        switch (Input.Action)
        {
            case RenameAction:
                return RedirectToPage(PageRoutes.SiblingRenamePasskey, RenamePasskeyRoute(Input.CredentialId));
            case DeleteAction:
                return await DeletePasskey(user, credentialId);
            default:
                StatusMessage = UnknownActionMessage;
                return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnPostAddPasskeyAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound(UserMessages.UnableToLoadUser(_userManager.GetUserId(User)));
        }

        if (!IsNullOrWhiteSpace(Input?.Passkey?.Error))
        {
            StatusMessage = Format(CultureInfo.InvariantCulture, BrowserErrorMessageFormat, Input.Passkey.Error);
            return RedirectToPage();
        }

        if (IsNullOrWhiteSpace(Input?.Passkey?.CredentialJson))
        {
            StatusMessage = BrowserProvidedNoPasskeyMessage;
            return RedirectToPage();
        }

        using var activity = Telemetry.StartActivity(RegisterActivityName);
        var attestationResult = await _signInManager.PerformPasskeyAttestationAsync(Input.Passkey.CredentialJson);
        if (!attestationResult.Succeeded)
        {
            activity?.SetTag(Telemetry.Metrics.SucceededTagName, false);
            StatusMessage = Format(CultureInfo.InvariantCulture, AttestationFailedMessageFormat, attestationResult.Failure.Message);
            return RedirectToPage();
        }

        var setPasskeyResult = await _userManager.AddOrUpdatePasskeyAsync(user, attestationResult.Passkey);
        if (!setPasskeyResult.Succeeded)
        {
            activity?.SetTag(Telemetry.Metrics.SucceededTagName, false);
            StatusMessage = PasskeyNotAddedMessage;
            return RedirectToPage();
        }

        activity?.SetTag(Telemetry.Metrics.SucceededTagName, true);
        StatusMessage = PasskeyAddedMessage;
        return RedirectToPage(
            PageRoutes.SiblingRenamePasskey,
            RenamePasskeyRoute(EncodeToString(attestationResult.Passkey.CredentialId)));
    }

    private static RouteValueDictionary RenamePasskeyRoute(string? credentialId) =>
        new() { [RenamePasskeyModel.IdRouteValueName] = credentialId };

    private async Task<IActionResult> DeletePasskey(IdentityUser<Guid> user, byte[] credentialId)
    {
        using var activity = Telemetry.StartActivity(DeleteActivityName);
        var result = await _userManager.RemovePasskeyAsync(user, credentialId);
        if (!result.Succeeded)
        {
            var userId = await _userManager.GetUserIdAsync(user);
            throw new InvalidOperationException($"Unexpected error occurred removing passkey for user with ID '{userId}'.");
        }

        StatusMessage = PasskeyRemovedMessage;
        return RedirectToPage();
    }

    public class InputModel
    {
        public string? CredentialId { get; set; }

        public string? Action { get; set; }

        public PasskeyInputModel? Passkey { get; set; }
    }
}