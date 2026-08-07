namespace Identity.Pages.Admin.Users.Edit;

using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class ClaimsModel : PageModel
{
    private readonly UserManager<IdentityUser<Guid>> _userManager;

    public ClaimsModel(UserManager<IdentityUser<Guid>> userManager) => _userManager = userManager;

    public IdentityUser<Guid> AppUser { get; private set; } = new();

    [BindProperty]
    public List<ClaimInputModel> Claims { get; set; } = [];

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

    public class ClaimInputModel
    {
        public string? Type { get; set; }

        public string? Value { get; set; }
    }
}