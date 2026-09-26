namespace Identity.Pages.Admin.Users.Edit;

using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

public class Claims : EditableResourcesBase<string, Claims.ClaimInputModel>
{
    internal const string DetailsPageName = "/Admin/Users/Details/Claims";

    private readonly UserManager<IdentityUser<Guid>> _userManager;

    public Claims(UserManager<IdentityUser<Guid>> userManager) => _userManager = userManager;

    public IdentityUser<Guid> AppUser { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        AppUser = user;
        var existing = await _userManager.GetClaimsAsync(user);
        Resources = existing.Select(c => new ClaimInputModel { Type = c.Type, Value = c.Value }).ToList();
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
        var claims = new List<Claim>();
        foreach (var input in Resources)
        {
            if (IsNullOrWhiteSpace(input.Type) || IsNullOrWhiteSpace(input.Value))
            {
                continue;
            }

            claims.Add(new Claim(input.Type, input.Value));
        }

        if (claims.Count > 0)
        {
            await _userManager.AddClaimsAsync(user, claims);
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

    protected override ClaimInputModel NewResource() => new();

    public class ClaimInputModel
    {
        public string? Type { get; set; }

        public string? Value { get; set; }
    }
}
