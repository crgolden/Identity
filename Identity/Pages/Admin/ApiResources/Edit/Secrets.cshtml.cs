namespace Identity.Pages.Admin.ApiResources.Edit;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Edits secrets for an API resource.</summary>
public class SecretsModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="SecretsModel"/> class.</summary>
    public SecretsModel(IConfigurationDbContext context) => _context = context;

    /// <summary>Gets the API resource id.</summary>
    public int ResourceId { get; private set; }

    /// <summary>Gets the API resource name.</summary>
    public string ResourceName { get; private set; } = Empty;

    /// <summary>Gets or sets the secrets.</summary>
    [BindProperty]
    public List<ApiResourceSecret> Secrets { get; set; } = [];

    /// <summary>Loads secrets for editing.</summary>
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var resource = await _context.ApiResources.Include(r => r.Secrets).FirstOrDefaultAsync(r => r.Id == id);
        if (resource is null)
        {
            return NotFound();
        }

        ResourceId = resource.Id;
        ResourceName = resource.Name;
        Secrets = resource.Secrets;
        return Page();
    }

    /// <summary>Saves secret changes.</summary>
    public async Task<IActionResult> OnPostAsync(int id)
    {
        var resource = await _context.ApiResources.Include(r => r.Secrets).FirstOrDefaultAsync(r => r.Id == id);
        if (resource is null)
        {
            return NotFound();
        }

        resource.Secrets.RemoveAll(s => !Secrets.Any(p => p.Id == s.Id));

        foreach (var posted in Secrets.Where(p => p.Id > 0))
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
            Secrets.Where(p => p.Id == 0).Select(p => new ApiResourceSecret
            {
                Description = p.Description,
                Value = p.Value,
                Type = p.Type,
                Expiration = p.Expiration,
                ApiResourceId = id,
            }));

        await _context.SaveChangesAsync();
        return RedirectToPage("/Admin/ApiResources/Details/Secrets", new { id });
    }
}
