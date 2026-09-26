namespace Identity.Pages.Admin.Clients;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class Create : PageModel
{
    private readonly IConfigurationDbContext _context;

    public Create(IConfigurationDbContext context) => _context = context;

    [BindProperty]
    public Client Client { get; set; } = new();

    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        _context.Clients.Add(Client);
        await _context.SaveChangesAsync();
        return RedirectToPage(PageRoutes.SiblingDetailsIndex, new { id = Client.Id });
    }
}
