namespace Identity.Pages.Admin.Users.Edit;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>Edits user role membership.</summary>
public class RolesModel : PageModel
{
    private readonly UserManager<IdentityUser<Guid>> _userManager;

    /// <summary>Initializes a new instance of the <see cref="RolesModel"/> class.</summary>
    public RolesModel(UserManager<IdentityUser<Guid>> userManager) => _userManager = userManager;

    /// <summary>Gets the user.</summary>
    public IdentityUser<Guid> AppUser { get; private set; } = new();

    /// <summary>Gets or sets the role names to assign.</summary>
    [BindProperty]
    public List<string> Roles { get; set; } = [];

    /// <summary>Loads the user's current roles.</summary>
    public async Task<IActionResult> OnGetAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        AppUser = user;
        Roles = (await _userManager.GetRolesAsync(user)).ToList();
        return Page();
    }

    /// <summary>Replaces the user's roles with the posted set.</summary>
    public async Task<IActionResult> OnPostAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var existing = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, existing);
        if (Roles.Count > 0)
        {
            await _userManager.AddToRolesAsync(user, Roles);
        }

        return RedirectToPage("/Admin/Users/Details/Roles", new { id });
    }

    /// <summary>Adds a blank role row.</summary>
    public async Task<IActionResult> OnPostAddRowAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        AppUser = user;
        Roles.Add(string.Empty);
        return Page();
    }

    /// <summary>Removes a role row.</summary>
    public async Task<IActionResult> OnPostRemoveRowAsync(string id, int index)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        AppUser = user;
        if (index >= 0 && index < Roles.Count)
        {
            Roles.RemoveAt(index);
        }

        return Page();
    }
}
