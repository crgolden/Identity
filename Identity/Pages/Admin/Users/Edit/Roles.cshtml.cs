namespace Identity.Pages.Admin.Users.Edit;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

public class Roles : EditableResourcesBase<string, string?>
{
    internal const string DetailsPageName = "/Admin/Users/Details/Roles";

    internal const string RowIdPrefix = "role-";

    internal const string RemoveRowIdPrefix = "role-remove-";

    private readonly UserManager<IdentityUser<Guid>> _userManager;

    public Roles(UserManager<IdentityUser<Guid>> userManager) => _userManager = userManager;

    public IdentityUser<Guid> AppUser { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        AppUser = user;
        Resources = [.. await _userManager.GetRolesAsync(user)];
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var existing = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, existing);
        var chosenRoles = Resources.OfType<string>().Where(role => !IsNullOrWhiteSpace(role)).ToList();
        if (chosenRoles.Count > 0)
        {
            await _userManager.AddToRolesAsync(user, chosenRoles);
        }

        return RedirectToPage(DetailsPageName, new { id });
    }

    protected override async Task<bool> LoadContextAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return false;
        }

        AppUser = user;
        return true;
    }

    protected override string? NewResource() => null;
}
