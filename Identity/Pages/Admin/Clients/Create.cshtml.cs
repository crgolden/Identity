namespace Identity.Pages.Admin.Clients;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>Creates a new client.</summary>
public class CreateModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="CreateModel"/> class.</summary>
    public CreateModel(IConfigurationDbContext context) => _context = context;

    /// <summary>Gets or sets the client to create.</summary>
    [BindProperty]
    public Client Client { get; set; } = new();

    /// <summary>Displays the create form.</summary>
    public IActionResult OnGet() => Page();

    /// <summary>Creates the client.</summary>
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        _context.Clients.Add(Client);
        await _context.SaveChangesAsync();
        return RedirectToPage("./Details/Index", new { id = Client.Id });
    }
}
