namespace Identity.Pages.Admin.Roles.Edit;

using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class ClaimsModel : PageModel
{
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public ClaimsModel(RoleManager<IdentityRole<Guid>> roleManager) => _roleManager = roleManager;

    public string RoleName { get; private set; } = Empty;

    [BindProperty]
    public List<ClaimInputModel> Claims { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role is null)
        {
            return NotFound();
        }

        RoleName = role.Name ?? Empty;
        var existing = await _roleManager.GetClaimsAsync(role);
        Claims = existing.Select(c => new ClaimInputModel { Type = c.Type, Value = c.Value }).ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role is null)
        {
            return NotFound();
        }

        var existing = await _roleManager.GetClaimsAsync(role);
        foreach (var claim in existing)
        {
            await _roleManager.RemoveClaimAsync(role, claim);
        }

        foreach (var claim in Claims)
        {
            await _roleManager.AddClaimAsync(role, new Claim(claim.Type ?? Empty, claim.Value ?? Empty));
        }

        return RedirectToPage("/Admin/Roles/Details/Claims", new { id });
    }

    public async Task<IActionResult> OnPostAddRowAsync(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role is null)
        {
            return NotFound();
        }

        RoleName = role.Name ?? Empty;
        Claims.Add(new ClaimInputModel());
        return Page();
    }

    public async Task<IActionResult> OnPostRemoveRowAsync(string id, int index)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role is null)
        {
            return NotFound();
        }

        RoleName = role.Name ?? Empty;
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