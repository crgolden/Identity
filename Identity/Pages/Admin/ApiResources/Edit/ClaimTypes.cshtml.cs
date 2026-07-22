namespace Identity.Pages.Admin.ApiResources.Edit;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Edits claim types for an API resource.</summary>
public class ClaimTypesModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="ClaimTypesModel"/> class.</summary>
    public ClaimTypesModel(IConfigurationDbContext context) => _context = context;

    /// <summary>Gets the API resource id.</summary>
    public int ResourceId { get; private set; }

    /// <summary>Gets the API resource name.</summary>
    public string ResourceName { get; private set; } = Empty;

    /// <summary>Gets or sets the claim types.</summary>
    [BindProperty]
    public List<ApiResourceClaim> ClaimTypes { get; set; } = [];

    /// <summary>Loads claim types for editing.</summary>
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var resource = await _context.ApiResources.Include(r => r.UserClaims).FirstOrDefaultAsync(r => r.Id == id);
        if (resource is null)
        {
            return NotFound();
        }

        ResourceId = resource.Id;
        ResourceName = resource.Name;
        ClaimTypes = resource.UserClaims;
        return Page();
    }

    /// <summary>Saves claim type changes.</summary>
    public async Task<IActionResult> OnPostAsync(int id)
    {
        var resource = await _context.ApiResources.Include(r => r.UserClaims).FirstOrDefaultAsync(r => r.Id == id);
        if (resource is null)
        {
            return NotFound();
        }

        resource.UserClaims.RemoveAll(c => !ClaimTypes.Any(p => p.Id == c.Id));

        foreach (var posted in ClaimTypes.Where(p => p.Id > 0))
        {
            var existing = resource.UserClaims.FirstOrDefault(c => c.Id == posted.Id);
            if (existing is not null)
            {
                existing.Type = posted.Type;
            }
        }

        resource.UserClaims.AddRange(
            ClaimTypes.Where(p => p.Id == 0).Select(p => new ApiResourceClaim { Type = p.Type, ApiResourceId = id }));

        await _context.SaveChangesAsync();
        return RedirectToPage("/Admin/ApiResources/Details/ClaimTypes", new { id });
    }

    /// <summary>Adds a blank claim type row.</summary>
    public async Task<IActionResult> OnPostAddRowAsync(int id)
    {
        var resource = await _context.ApiResources.FirstOrDefaultAsync(r => r.Id == id);
        if (resource is null)
        {
            return NotFound();
        }

        ResourceId = resource.Id;
        ResourceName = resource.Name;
        ClaimTypes.Add(new ApiResourceClaim());
        return Page();
    }

    /// <summary>Removes a claim type row.</summary>
    public async Task<IActionResult> OnPostRemoveRowAsync(int id, int index)
    {
        var resource = await _context.ApiResources.FirstOrDefaultAsync(r => r.Id == id);
        if (resource is null)
        {
            return NotFound();
        }

        ResourceId = resource.Id;
        ResourceName = resource.Name;
        if (index >= 0 && index < ClaimTypes.Count)
        {
            ClaimTypes.RemoveAt(index);
        }

        return Page();
    }
}
