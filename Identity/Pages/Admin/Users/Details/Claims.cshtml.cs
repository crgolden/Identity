namespace Identity.Pages.Admin.Users.Details;

using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>Shows user claims.</summary>
public class ClaimsModel : PageModel
{
    private readonly UserManager<IdentityUser<Guid>> _userManager;

    /// <summary>Initializes a new instance of the <see cref="ClaimsModel"/> class.</summary>
    public ClaimsModel(UserManager<IdentityUser<Guid>> userManager) => _userManager = userManager;

    /// <summary>Gets the user.</summary>
    public IdentityUser<Guid> AppUser { get; private set; } = new();

    /// <summary>Gets the user's claims.</summary>
    public IList<Claim> Claims { get; private set; } = [];

    /// <summary>Loads the user's claims.</summary>
    public async Task<IActionResult> OnGetAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        AppUser = user;
        Claims = await _userManager.GetClaimsAsync(user);
        return Page();
    }
}
