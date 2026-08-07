namespace Identity.Pages.Admin.ApiScopes;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class CreateModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    public CreateModel(IConfigurationDbContext context) => _context = context;

    [BindProperty]
    public ApiScope Scope { get; set; } = new();

    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        _context.ApiScopes.Add(Scope);
        await _context.SaveChangesAsync();
        return RedirectToPage("./Details/Index", new { id = Scope.Id });
    }
}