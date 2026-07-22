namespace Identity.Pages.Admin.Users.Edit;

using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>Edits user claims.</summary>
public class ClaimsModel : PageModel
{
    private readonly UserManager<IdentityUser<Guid>> _userManager;

    /// <summary>Initializes a new instance of the <see cref="ClaimsModel"/> class.</summary>
    public ClaimsModel(UserManager<IdentityUser<Guid>> userManager) => _userManager = userManager;

    /// <summary>Gets the user.</summary>
    public IdentityUser<Guid> AppUser { get; private set; } = new();

    /// <summary>Gets or sets the claims to save.</summary>
    [BindProperty]
    public List<ClaimInputModel> Claims { get; set; } = [];

    /// <summary>Loads the user's current claims.</summary>
    public async Task<IActionResult> OnGetAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        AppUser = user;
        var existing = await _userManager.GetClaimsAsync(user);
        Claims = existing.Select(c => new ClaimInputModel { Type = c.Type, Value = c.Value }).ToList();
        return Page();
    }

    /// <summary>Replaces the user's claims with the posted set.</summary>
    public async Task<IActionResult> OnPostAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var existing = await _userManager.GetClaimsAsync(user);
        await _userManager.RemoveClaimsAsync(user, existing);
        if (Claims.Count > 0)
        {
            await _userManager.AddClaimsAsync(user, Claims.Select(c => new Claim(c.Type ?? Empty, c.Value ?? Empty)));
        }

        return RedirectToPage("/Admin/Users/Details/Claims", new { id });
    }

    /// <summary>Adds a blank claim row.</summary>
    public async Task<IActionResult> OnPostAddRowAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        AppUser = user;
        Claims.Add(new ClaimInputModel());
        return Page();
    }

    /// <summary>Removes a claim row.</summary>
    public async Task<IActionResult> OnPostRemoveRowAsync(string id, int index)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        AppUser = user;
        if (index >= 0 && index < Claims.Count)
        {
            Claims.RemoveAt(index);
        }

        return Page();
    }

    /// <summary>Represents a claim type/value pair for form binding.</summary>
    public class ClaimInputModel
    {
        /// <summary>Gets or sets the claim type.</summary>
        public string? Type { get; set; }

        /// <summary>Gets or sets the claim value.</summary>
        public string? Value { get; set; }
    }
}
