namespace Identity.Pages.Account.Manage;

using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using static System.Attribute;
using static System.Net.Mime.MediaTypeNames.Application;
using static System.Text.Json.JsonSerializer;
using static Microsoft.Net.Http.Headers.HeaderNames;

public class DownloadPersonalData : PageModel
{
    internal const string ExternalLoginKeyNameFormat = "{0} external login provider key";

    internal const string AuthenticatorKeyName = "Authenticator Key";

    internal const string AttachmentDisposition = "attachment; filename=PersonalData.json";

    private readonly UserManager<IdentityUser<Guid>> _userManager;

    public DownloadPersonalData(UserManager<IdentityUser<Guid>> userManager)
    {
        ThrowIfNull(userManager);
        _userManager = userManager;
    }

    public IActionResult OnGet()
    {
        return NotFound();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound(UserMessages.UnableToLoadUser(_userManager.GetUserId(User)));
        }

        var personalData = typeof(IdentityUser<Guid>)
            .GetProperties()
            .Where(x => IsDefined(x, typeof(PersonalDataAttribute)))
            .ToDictionary(p => p.Name, p => p.GetValue(user)?.ToString(), StringComparer.Ordinal);
        var userLoginInfos = await _userManager.GetLoginsAsync(user);
        foreach (var userLoginInfo in userLoginInfos)
        {
            personalData.Add(Format(CultureInfo.InvariantCulture, ExternalLoginKeyNameFormat, userLoginInfo.LoginProvider), userLoginInfo.ProviderKey);
        }

        personalData.Add(AuthenticatorKeyName, await _userManager.GetAuthenticatorKeyAsync(user));
        Response.Headers.TryAdd(ContentDisposition, AttachmentDisposition);
        var fileContents = SerializeToUtf8Bytes(personalData);
        return new FileContentResult(fileContents, Json);
    }
}
