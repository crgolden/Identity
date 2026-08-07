namespace Identity.Pages.Admin.IdentityResources;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class CreateModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    public CreateModel(IConfigurationDbContext context) => _context = context;

    [BindProperty]
    public IdentityResource Resource { get; set; } = new();

    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        _context.IdentityResources.Add(Resource);
        await _context.SaveChangesAsync();
        return RedirectToPage("./Details/Index", new { id = Resource.Id });
    }
}