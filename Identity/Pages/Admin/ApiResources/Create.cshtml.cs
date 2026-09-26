namespace Identity.Pages.Admin.ApiResources;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class Create : PageModel
{
    private readonly IConfigurationDbContext _context;

    public Create(IConfigurationDbContext context) => _context = context;

    [BindProperty]
    public ApiResource Resource { get; set; } = new();

    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        _context.ApiResources.Add(Resource);
        await _context.SaveChangesAsync();
        return RedirectToPage(PageRoutes.SiblingDetailsIndex, new { id = Resource.Id });
    }
}
