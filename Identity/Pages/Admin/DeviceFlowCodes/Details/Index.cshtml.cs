namespace Identity.Pages.Admin.DeviceFlowCodes.Details;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Shows device flow code details.</summary>
public class IndexModel : PageModel
{
    private readonly IPersistedGrantDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="IndexModel"/> class.</summary>
    public IndexModel(IPersistedGrantDbContext context) => _context = context;

    /// <summary>Gets the device flow code.</summary>
    public DeviceFlowCodes DeviceFlowCode { get; private set; } = new();

    /// <summary>Loads the device flow code by device code.</summary>
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
