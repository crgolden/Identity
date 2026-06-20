namespace Identity.Pages.Admin.DeviceFlowCodes;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Lists all device flow codes.</summary>
public class IndexModel : PageModel
{
    private readonly IPersistedGrantDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="IndexModel"/> class.</summary>
    public IndexModel(IPersistedGrantDbContext context) => _context = context;

    /// <summary>Gets the device flow codes.</summary>
    public IList<DeviceFlowCodes> DeviceFlowCodes { get; private set; } = [];

    /// <summary>Loads all device flow codes.</summary>
    public async Task OnGetAsync()
    {
        DeviceFlowCodes = await _context.DeviceFlowCodes
            .OrderBy(d => d.ClientId)
            .ToListAsync();
    }
}
