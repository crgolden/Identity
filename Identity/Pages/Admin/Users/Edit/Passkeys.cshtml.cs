namespace Identity.Pages.Admin.Users.Edit;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using static System.Buffers.Text.Base64Url;

/// <summary>Manages user passkeys.</summary>
public class PasskeysModel : PageModel
{
    private readonly UserManager<IdentityUser<Guid>> _userManager;

    /// <summary>Initializes a new instance of the <see cref="PasskeysModel"/> class.</summary>
    public PasskeysModel(UserManager<IdentityUser<Guid>> userManager) => _userManager = userManager;

    /// <summary>Gets the user.</summary>
    public IdentityUser<Guid> AppUser { get; private set; } = new();

    /// <summary>Gets the user's passkeys.</summary>
    public IList<UserPasskeyInfo> Passkeys { get; private set; } = [];

    /// <summary>Loads the user's passkeys.</summary>
    public async Task<IActionResult> OnGetAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        AppUser = user;
        Passkeys = await _userManager.GetPasskeysAsync(user);
        return Page();
    }

    /// <summary>Removes a passkey.</summary>
    public async Task<IActionResult> OnPostRemoveAsync(string id, string credentialId)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var credentialIdBytes = DecodeFromChars(credentialId);
        await _userManager.RemovePasskeyAsync(user, credentialIdBytes);
        return RedirectToPage(new { id });
    }
}
