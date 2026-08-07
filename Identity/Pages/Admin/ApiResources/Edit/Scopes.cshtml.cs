namespace Identity.Pages.Admin.ApiResources.Edit;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class ScopesModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    public ScopesModel(IConfigurationDbContext context) => _context = context;

    public int ResourceId { get; private set; }

    public string ResourceName { get; private set; } = Empty;

    [BindProperty]
    public List<ApiResourceScope> Scopes { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var resource = await _context.ApiResources.Include(r => r.Scopes).FirstOrDefaultAsync(r => r.Id == id);
        if (resource is null)
        {
            return NotFound();
        }

        ResourceId = resource.Id;
        ResourceName = resource.Name;
        Scopes = resource.Scopes;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var resource = await _context.ApiResources.Include(r => r.Scopes).FirstOrDefaultAsync(r => r.Id == id);
        if (resource is null)
        {
            return NotFound();
        }

        resource.Scopes.RemoveAll(s => !Scopes.Any(p => p.Id == s.Id));

        foreach (var posted in Scopes.Where(p => p.Id > 0))
        {
            var existing = resource.Scopes.FirstOrDefault(s => s.Id == posted.Id);
            if (existing is not null)
            {
                existing.Scope = posted.Scope;
            }
        }

        resource.Scopes.AddRange(
            Scopes.Where(p => p.Id == 0).Select(p => new ApiResourceScope { Scope = p.Scope, ApiResourceId = id }));

        await _context.SaveChangesAsync();
        return RedirectToPage("/Admin/ApiResources/Details/Scopes", new { id });
    }

    public async Task<IActionResult> OnPostAddRowAsync(int id)
    {
        var resource = await _context.ApiResources.FirstOrDefaultAsync(r => r.Id == id);
        if (resource is null)
        {
            return NotFound();
        }

        ResourceId = resource.Id;
        ResourceName = resource.Name;
        Scopes.Add(new ApiResourceScope());
        return Page();
    }

    public async Task<IActionResult> OnPostRemoveRowAsync(int id, int index)
    {
        var resource = await _context.ApiResources.FirstOrDefaultAsync(r => r.Id == id);
        if (resource is null)
        {
            return NotFound();
        }

        ResourceId = resource.Id;
        ResourceName = resource.Name;
        if (index >= 0 && index < Scopes.Count)
        {
            Scopes.RemoveAt(index);
        }

        return Page();
    }
}