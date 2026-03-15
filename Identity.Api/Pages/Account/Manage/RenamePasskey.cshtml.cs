namespace Identity.Pages.Account.Manage;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using static System.Buffers.Text.Base64Url;

public class RenamePasskeyModel : PageModel
{
    private readonly UserManager<IdentityUser<Guid>> _userManager;
    private readonly ApplicationDbContext _dbContext;

    public RenamePasskeyModel(
        UserManager<IdentityUser<Guid>> userManager,
        ApplicationDbContext dbContext)
    {
        _userManager = userManager;
        _dbContext = dbContext;
    }

    [BindProperty]
    public InputModel? Input { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        byte[] credentialId;
        try
        {
            credentialId = DecodeFromChars(id);
        }
        catch (FormatException)
        {
            StatusMessage = "The specified passkey ID had an invalid format.";
            return RedirectToPage("./Passkeys");
        }

        var passkey = await _userManager.GetPasskeyAsync(user, credentialId);
        if (passkey is null)
        {
            return NotFound($"Unable to load passkey ID '{_userManager.GetUserId(User)}'.");
        }

        Input = new InputModel
        {
            CredentialId = id,
            Name = passkey.Name
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        byte[] credentialId;
        try
        {
            credentialId = DecodeFromChars(Input?.CredentialId);
        }
        catch (FormatException)
        {
            StatusMessage = "The specified passkey ID had an invalid format.";
            return RedirectToPage("./Passkeys");
        }

        var passkey = await _userManager.GetPasskeyAsync(user, credentialId);
        if (passkey is null)
        {
            return NotFound($"Unable to load passkey ID '{_userManager.GetUserId(User)}'.");
        }

        passkey.Name = Input?.Name;
        var result = await _userManager.AddOrUpdatePasskeyAsync(user, passkey);
        if (!result.Succeeded)
        {
            var userId = await _userManager.GetUserIdAsync(user);
            throw new InvalidOperationException($"Unexpected error occurred removing passkey for user with ID '{userId}'.");
        }

        var passkeyEntity = await _dbContext.UserPasskeys.SingleOrDefaultAsync(x => x.CredentialId == credentialId);
        if (passkeyEntity is not null)
        {
            passkeyEntity.Data.Name = Input?.Name;
            await _dbContext.SaveChangesAsync();
        }

        StatusMessage = "The passkey was updated.";
        return RedirectToPage("./Passkeys");
    }

    public class InputModel
    {
        public string? CredentialId { get; set; }

        public string? Name { get; set; }
    }
}
