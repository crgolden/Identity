namespace Identity.Pages.Admin.IdentityProviders;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class CreateModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    public CreateModel(IConfigurationDbContext context) => _context = context;

    [BindProperty]
    public IdentityProvider IdentityProvider { get; set; } = new();

    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        _context.IdentityProviders.Add(IdentityProvider);
        await _context.SaveChangesAsync();
        return RedirectToPage("./Details", new { id = IdentityProvider.Id });
    }
}