namespace Identity.Pages.Account.Manage;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using static System.Net.Mime.MediaTypeNames.Application;
using static System.Text.Json.JsonSerializer;
using static Attribute;
using static Microsoft.Net.Http.Headers.HeaderNames;

public class DownloadPersonalDataModel : PageModel
{
    private readonly UserManager<IdentityUser<Guid>> _userManager;

    public DownloadPersonalDataModel(UserManager<IdentityUser<Guid>> userManager)
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
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        var personalData = typeof(IdentityUser<Guid>)
            .GetProperties()
            .Where(x => IsDefined(x, typeof(PersonalDataAttribute)))
            .ToDictionary(p => p.Name, p => p.GetValue(user)?.ToString());
        var userLoginInfos = await _userManager.GetLoginsAsync(user);
        foreach (var userLoginInfo in userLoginInfos)
        {
            personalData.Add($"{userLoginInfo.LoginProvider} external login provider key", userLoginInfo.ProviderKey);
        }

        personalData.Add("Authenticator Key", await _userManager.GetAuthenticatorKeyAsync(user));
        Response.Headers.TryAdd(ContentDisposition, "attachment; filename=PersonalData.json");
        var fileContents = SerializeToUtf8Bytes(personalData);
        return new FileContentResult(fileContents, Json);
    }
}