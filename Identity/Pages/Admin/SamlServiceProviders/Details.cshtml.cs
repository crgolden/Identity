namespace Identity.Pages.Admin.SamlServiceProviders;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class DetailsModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    public DetailsModel(IConfigurationDbContext context) => _context = context;

    public SamlServiceProvider SamlServiceProvider { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var sp = await _context.SamlServiceProviders.FirstOrDefaultAsync(s => s.Id == id);
        if (sp is null)
        {
            return NotFound();
        }

        SamlServiceProvider = sp;
        return Page();
    }
}