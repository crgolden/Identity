namespace Identity.Pages.Admin.DeviceFlowCodes;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class IndexModel : PageModel
{
    private readonly IPersistedGrantDbContext _context;

    public IndexModel(IPersistedGrantDbContext context) => _context = context;

    public IList<DeviceFlowCodes> DeviceFlowCodes { get; private set; } = [];

    public async Task OnGetAsync()
    {
        DeviceFlowCodes = await _context.DeviceFlowCodes
            .OrderBy(d => d.ClientId)
            .ToListAsync();
    }
}