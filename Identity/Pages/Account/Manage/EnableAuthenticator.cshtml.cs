namespace Identity.Pages.Account.Manage;

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using static System.String;

public class EnableAuthenticator : PageModel
{
    internal const int RecoveryCodeCount = 10;

    internal const string CodeModelStateKey =
        "Input.Code";

    internal const string InvalidVerificationCodeMessage =
        "Verification code is invalid.";

    internal const string AuthenticatorVerifiedMessage =
        "Your authenticator app has been verified.";

    private const string OtpAuthScheme = "otpauth";
    private const string TotpUriFormat = "totp/{0}:{1}?secret={2}&issuer={0}&digits=6";

    private static readonly string AuthenticatorUriFormat = OtpAuthScheme + Uri.SchemeDelimiter + TotpUriFormat;

    private readonly UserManager<IdentityUser<Guid>> _userManager;
    private readonly UrlEncoder _urlEncoder;

    public EnableAuthenticator(
        UserManager<IdentityUser<Guid>> userManager,
        UrlEncoder urlEncoder)
    {
        _userManager = userManager;
        _urlEncoder = urlEncoder;
    }

    public string? SharedKey { get; set; }

    public string? AuthenticatorUri { get; set; }

    [TempData]
    public string[] RecoveryCodes { get; set; } = Array.Empty<string>();

    [TempData]
    public string? StatusMessage { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new InputModel();

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound(UserMessages.UnableToLoadUser(_userManager.GetUserId(User)));
        }

        await LoadSharedKeyAndQrCodeUriAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound(UserMessages.UnableToLoadUser(_userManager.GetUserId(User)));
        }

        if (!ModelState.IsValid || IsNullOrWhiteSpace(Input.Code))
        {
            await LoadSharedKeyAndQrCodeUriAsync(user);
            return Page();
        }

        var verificationCode = Input.Code
            .Replace(" ", Empty, StringComparison.Ordinal)
            .Replace("-", Empty, StringComparison.Ordinal);
        var tokenProvider = _userManager.Options.Tokens.AuthenticatorTokenProvider;
        var is2faTokenValid = await _userManager.VerifyTwoFactorTokenAsync(user, tokenProvider, verificationCode);
        if (!is2faTokenValid)
        {
            ModelState.AddModelError(CodeModelStateKey, InvalidVerificationCodeMessage);
            await LoadSharedKeyAndQrCodeUriAsync(user);
            return Page();
        }

        await _userManager.SetTwoFactorEnabledAsync(user, true);
        StatusMessage = AuthenticatorVerifiedMessage;
        if (await _userManager.CountRecoveryCodesAsync(user) > 0)
        {
            return RedirectToPage(PageRoutes.SiblingTwoFactorAuthentication);
        }

        var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, RecoveryCodeCount);
        RecoveryCodes = recoveryCodes?.ToArray() ?? RecoveryCodes;
        return RedirectToPage(PageRoutes.SiblingShowRecoveryCodes);
    }

    private static string FormatKey(string unformattedKey)
    {
        var result = new StringBuilder();
        var currentPosition = 0;
        const int length = 4;
        ReadOnlySpan<char> value;
        while (currentPosition + length < unformattedKey.Length)
        {
            value = unformattedKey.AsSpan(currentPosition, length);
            result.Append(value).Append(' ');
            currentPosition += length;
        }

        if (currentPosition >= unformattedKey.Length)
        {
            return result.ToString().ToLowerInvariant();
        }

        value = unformattedKey.AsSpan(currentPosition);
        result.Append(value);
        return result.ToString().ToLowerInvariant();
    }

    private async Task LoadSharedKeyAndQrCodeUriAsync(IdentityUser<Guid> user)
    {
        var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
        if (IsNullOrWhiteSpace(unformattedKey))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
        }

        if (IsNullOrWhiteSpace(unformattedKey))
        {
            return;
        }

        SharedKey = FormatKey(unformattedKey);
        var email = await _userManager.GetEmailAsync(user);
        if (IsNullOrWhiteSpace(email))
        {
            return;
        }

        AuthenticatorUri = GenerateQrCodeUri(email, unformattedKey);
    }

    private string GenerateQrCodeUri(string email, string unformattedKey)
    {
        return Format(
            CultureInfo.InvariantCulture,
            AuthenticatorUriFormat,
            _urlEncoder.Encode("crgolden"),
            _urlEncoder.Encode(email),
            unformattedKey);
    }

    public class InputModel
    {
        [Required]
        [StringLength(7, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Text)]
        [Display(Name = "Verification Code")]
        public string? Code { get; set; }
    }
}
