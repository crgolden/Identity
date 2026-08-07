namespace Identity.Pages.Admin.Users.Edit;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Users;
using static System.Buffers.Text.Base64Url;

public class PasskeysModel : UserSubPageModelBase
{
    public PasskeysModel(UserManager<IdentityUser<Guid>> userManager)
        : base(userManager)
    {
    }

    public IList<UserPasskeyInfo> Passkeys { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(string id)
    {
        if (!await TryLoadUserAsync(id))
        {
            return NotFound();
        }

        Passkeys = await UserManager.GetPasskeysAsync(AppUser);
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