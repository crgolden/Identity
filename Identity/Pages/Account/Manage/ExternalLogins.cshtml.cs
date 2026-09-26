namespace Identity.Pages.Account.Manage;

using Identity.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class ExternalLogins : PageModel
{
    internal const string ExternalLoginsPagePath = "/Account/Manage/ExternalLogins";

    internal const string LinkLoginCallbackPageHandler = "LinkLoginCallback";

    internal const string LoginAddedMessage = "The external login was added.";

    internal const string LoginNotAddedMessage =
        "The external login was not added. External logins can only be associated with one account.";

    internal const string LoginRemovedMessage = "The external login was removed.";

    internal const string LoginNotRemovedMessage = "The external login was not removed.";

    private readonly UserManager<IdentityUser<Guid>> _userManager;
    private readonly SignInManager<IdentityUser<Guid>> _signInManager;
    private readonly IUserStore<IdentityUser<Guid>> _userStore;

    public ExternalLogins(
        UserManager<IdentityUser<Guid>> userManager,
        SignInManager<IdentityUser<Guid>> signInManager,
        IUserStore<IdentityUser<Guid>> userStore)
    {
        ThrowIfNull(userManager);
        ThrowIfNull(signInManager);
        ThrowIfNull(userStore);
        _userManager = userManager;
        _signInManager = signInManager;
        _userStore = userStore;
    }

    public IList<UserLoginInfo> CurrentLogins { get; set; } = new List<UserLoginInfo>();

    public IList<AuthenticationScheme> OtherLogins { get; set; } = new List<AuthenticationScheme>();

    public bool ShowRemoveButton { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound(UserMessages.UnableToLoadUser(_userManager.GetUserId(User)));
        }

        CurrentLogins = await _userManager.GetLoginsAsync(user);
        OtherLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync())
            .Where(x => CurrentLogins.All(y => !string.Equals(x.Name, y.LoginProvider, StringComparison.Ordinal)))
            .ToList();

        string? passwordHash = null;
        if (_userStore is IUserPasswordStore<IdentityUser<Guid>> userPasswordStore)
        {
            passwordHash = await userPasswordStore.GetPasswordHashAsync(user, HttpContext.RequestAborted);
        }

        ShowRemoveButton = !IsNullOrWhiteSpace(passwordHash) || CurrentLogins.Count > 1;
        return Page();
    }

    public async Task<IActionResult> OnPostRemoveLoginAsync(string loginProvider, string providerKey)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound(UserMessages.UnableToLoadUser(_userManager.GetUserId(User)));
        }

        var result = await _userManager.RemoveLoginAsync(user, loginProvider, providerKey);
        if (!result.Succeeded)
        {
            StatusMessage = LoginNotRemovedMessage;
            return RedirectToPage();
        }

        await _signInManager.RefreshSignInAsync(user);
        StatusMessage = LoginRemovedMessage;
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostLinkLoginAsync(string provider)
    {
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        var redirectUrl = Url.Page(ExternalLoginsPagePath, pageHandler: LinkLoginCallbackPageHandler);
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl, _userManager.GetUserId(User));
        return new ChallengeResult(provider, properties);
    }

    public async Task<IActionResult> OnGetLinkLoginCallbackAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound(UserMessages.UnableToLoadUser(_userManager.GetUserId(User)));
        }

        var userId = await _userManager.GetUserIdAsync(user);
        var info = await _signInManager.GetExternalLoginInfoAsync(userId);
        if (info is null)
        {
            throw new InvalidOperationException(UserMessages.UnexpectedErrorLoadingExternalLoginInfo);
        }

        var result = await _userManager.AddLoginAsync(user, info);
        if (!result.Succeeded)
        {
            StatusMessage = LoginNotAddedMessage;
            return RedirectToPage();
        }

        await _userManager.AddMissingClaimsAsync(user, info.Principal);

        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        StatusMessage = LoginAddedMessage;
        return RedirectToPage();
    }
}
