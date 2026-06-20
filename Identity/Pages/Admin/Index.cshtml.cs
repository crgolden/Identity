namespace Identity.Pages.Admin;

using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>Admin landing page.</summary>
public class IndexModel : PageModel
{
    /// <summary>Returns the admin landing page.</summary>
    public void OnGet()
    {
        // No model data needed for the landing page card grid.
    }
}
