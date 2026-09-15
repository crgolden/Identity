namespace Identity.Pages.Admin.Roles.Edit;

using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class ClaimsModel : PageModel
{
    internal const string DetailsPageName = "/Admin/Roles/Details/Claims";

    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public ClaimsModel(RoleManager<IdentityRole<Guid>> roleManager) => _roleManager = roleManager;

    public string? RoleName { get; private set; }

    [BindProperty]
    public List<ClaimInputModel> Claims { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role is null)
        {
            return NotFound();
        }

        RoleName = role.Name;
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
            if (IsNullOrWhiteSpace(claim.Type) || IsNullOrWhiteSpace(claim.Value))
            {
                continue;
            }

            await _roleManager.AddClaimAsync(role, new Claim(claim.Type, claim.Value));
        }

        return RedirectToPage(DetailsPageName, new { id });
    }

    public async Task<IActionResult> OnPostAddRowAsync(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role is null)
        {
            return NotFound();
        }

        RoleName = role.Name;
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

        RoleName = role.Name;
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