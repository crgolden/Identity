namespace Identity.Pages.Admin.Users.Edit;

using Identity.Pages.Admin.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using static System.Buffers.Text.Base64Url;

public class Passkeys : UserResourcesBase<UserPasskeyInfo>
{
    public Passkeys(UserManager<IdentityUser<Guid>> userManager)
        : base(userManager)
    {
    }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        if (!await TryLoadUserAsync(id))
        {
            return NotFound();
        }

        Resources = await UserManager.GetPasskeysAsync(AppUser);
        return Page();
    }

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
