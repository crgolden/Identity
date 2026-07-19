namespace Identity.Pages.Admin.Users.Edit;

using Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using static System.Buffers.Text.Base64Url;

/// <summary>Manages user passkeys.</summary>
public class PasskeysModel : UserSubPageModelBase
{
    /// <summary>Initializes a new instance of the <see cref="PasskeysModel"/> class.</summary>
    public PasskeysModel(UserManager<IdentityUser<Guid>> userManager)
        : base(userManager)
    {
    }

    /// <summary>Gets the user's passkeys.</summary>
    public IList<UserPasskeyInfo> Passkeys { get; private set; } = [];

    /// <summary>Loads the user's passkeys.</summary>
    public async Task<IActionResult> OnGetAsync(string id)
    {
        if (!await TryLoadUserAsync(id))
        {
            return NotFound();
        }

        Passkeys = await UserManager.GetPasskeysAsync(AppUser);
        return Page();
    }

    /// <summary>Removes a passkey.</summary>
    public async Task<IActionResult> OnPostRemoveAsync(string id, string credentialId)
    {
        if (!await TryLoadUserAsync(id))
        {
            return NotFound();
        }

        var credentialIdBytes = DecodeFromChars(credentialId);
        await UserManager.RemovePasskeyAsync(AppUser, credentialIdBytes);
        return RedirectToPage(new { id });
    }
}
