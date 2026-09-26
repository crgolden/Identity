namespace Identity.Pages.Admin.ApiResources.Edit;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class Secrets : EditableResourcesBase<int, ApiResourceSecret>
{
    internal const string DetailsPageName = "/Admin/ApiResources/Details/Secrets";

    private readonly IConfigurationDbContext _context;

    public Secrets(IConfigurationDbContext context) => _context = context;

    public int ResourceId { get; private set; }

    public string? ResourceName { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var resource = await _context.ApiResources.Include(r => r.Secrets).FirstOrDefaultAsync(r => r.Id == id);
        if (resource is null)
        {
            return NotFound();
        }

        ResourceId = resource.Id;
        ResourceName = resource.Name;
        Resources = resource.Secrets;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var resource = await _context.ApiResources.Include(r => r.Secrets).FirstOrDefaultAsync(r => r.Id == id);
        if (resource is null)
        {
            return NotFound();
        }

        resource.Secrets.RemoveAll(s => !Resources.Any(p => p.Id == s.Id));

        foreach (var posted in Resources.Where(p => p.Id > 0))
        {
            var existing = resource.Secrets.FirstOrDefault(s => s.Id == posted.Id);
            if (existing is not null)
            {
                existing.Description = posted.Description;
                existing.Type = posted.Type;
                existing.Expiration = posted.Expiration;
            }
        }

        resource.Secrets.AddRange(
            Resources.Where(p => p.Id == 0).Select(p => new ApiResourceSecret
            {
                Description = p.Description,
                Value = p.Value,
                Type = p.Type,
                Expiration = p.Expiration,
                ApiResourceId = id,
            }));

        await _context.SaveChangesAsync();
        return RedirectToPage(DetailsPageName, new { id });
    }

    protected override async Task<bool> LoadContextAsync(int id)
    {
        var resource = await _context.ApiResources.FirstOrDefaultAsync(r => r.Id == id);
        if (resource is null)
        {
            return false;
        }

        ResourceId = resource.Id;
        ResourceName = resource.Name;
        return true;
    }

    protected override ApiResourceSecret NewResource() => new ApiResourceSecret { Type = "SharedSecret" };
}
