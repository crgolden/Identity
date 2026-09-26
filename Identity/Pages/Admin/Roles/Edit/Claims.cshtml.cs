namespace Identity.Pages.Admin.Roles.Edit;

using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

public class Claims : EditableResourcesBase<string, Claims.ClaimInputModel>
{
    internal const string DetailsPageName = "/Admin/Roles/Details/Claims";

    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public Claims(RoleManager<IdentityRole<Guid>> roleManager) => _roleManager = roleManager;

    public string? RoleName { get; private set; }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role is null)
        {
            return NotFound();
        }

        RoleName = role.Name;
        var existing = await _roleManager.GetClaimsAsync(role);
        Resources = existing.Select(c => new ClaimInputModel { Type = c.Type, Value = c.Value }).ToList();
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

        foreach (var claim in Resources)
        {
            if (IsNullOrWhiteSpace(claim.Type) || IsNullOrWhiteSpace(claim.Value))
            {
                continue;
            }

            await _roleManager.AddClaimAsync(role, new Claim(claim.Type, claim.Value));
        }

        return RedirectToPage(DetailsPageName, new { id });
    }

    protected override async Task<bool> LoadContextAsync(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role is null)
        {
            return false;
        }

        RoleName = role.Name;
        return true;
    }

    protected override ClaimInputModel NewResource() => new();

    public class ClaimInputModel
    {
        public string? Type { get; set; }

        public string? Value { get; set; }
    }
}
