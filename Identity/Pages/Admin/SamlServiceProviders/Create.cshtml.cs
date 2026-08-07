namespace Identity.Pages.Admin.SamlServiceProviders;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class CreateModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    public CreateModel(IConfigurationDbContext context) => _context = context;

    [BindProperty]
    public SamlServiceProvider SamlServiceProvider { get; set; } = new();

    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        _context.SamlServiceProviders.Add(SamlServiceProvider);
        await _context.SaveChangesAsync();
        return RedirectToPage("./Details", new { id = SamlServiceProvider.Id });
    }
}