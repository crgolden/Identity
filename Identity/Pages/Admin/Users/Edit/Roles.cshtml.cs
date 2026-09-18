namespace Identity.Pages.Admin.Users.Edit;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class RolesModel : PageModel
{
    internal const string DetailsPageName = "/Admin/Users/Details/Roles";

    private readonly UserManager<IdentityUser<Guid>> _userManager;

    public RolesModel(UserManager<IdentityUser<Guid>> userManager) => _userManager = userManager;

    public IdentityUser<Guid> AppUser { get; private set; } = new();

    [BindProperty]
    public List<string?> Roles { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        AppUser = user;
        Roles = [.. await _userManager.GetRolesAsync(user)];
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
        var chosenRoles = Roles.OfType<string>().Where(role => !IsNullOrWhiteSpace(role)).ToList();
        if (chosenRoles.Count > 0)
        {
            await _userManager.AddToRolesAsync(user, chosenRoles);
        }

        return RedirectToPage(DetailsPageName, new { id });
    }

    public async Task<IActionResult> OnPostAddRowAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        AppUser = user;
        Roles.Add(null);
        return Page();
    }

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