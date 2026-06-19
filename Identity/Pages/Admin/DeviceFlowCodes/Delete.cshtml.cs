namespace Identity.Pages.Admin.DeviceFlowCodes;

using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

/// <summary>Deletes a device flow code.</summary>
public class DeleteModel : PageModel
{
    private readonly IPersistedGrantDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="DeleteModel"/> class.</summary>
    public DeleteModel(IPersistedGrantDbContext context) => _context = context;

    /// <summary>Gets the device flow code to delete.</summary>
    public DeviceFlowCodes DeviceFlowCode { get; private set; } = new();

    /// <summary>Loads the device flow code for confirmation.</summary>
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

    /// <summary>Deletes the device flow code.</summary>
    public async Task<IActionResult> OnPostAsync(string deviceCode)
    {
        var code = await _context.DeviceFlowCodes.FirstOrDefaultAsync(d => d.DeviceCode == deviceCode);
        if (code is null)
        {
            return NotFound();
        }

        _context.DeviceFlowCodes.Remove(code);
        await _context.SaveChangesAsync();
        return RedirectToPage("./Index");
    }
}
