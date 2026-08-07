namespace Identity.Pages.Admin.DeviceFlowCodes.Details;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class IndexModel : PageModel
{
    private readonly IPersistedGrantDbContext _context;

    public IndexModel(IPersistedGrantDbContext context) => _context = context;

    public DeviceFlowCodes DeviceFlowCode { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(string deviceCode)
    {
        var code = await _context.DeviceFlowCodes.FirstOrDefaultAsync(d => d.DeviceCode == deviceCode);
        if (code is null)
        {
            return NotFound();
        }

        DeviceFlowCode = code;
        return Page();
    }
}