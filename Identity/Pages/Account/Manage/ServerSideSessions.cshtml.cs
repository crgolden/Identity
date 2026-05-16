namespace Identity.Pages.Account.Manage;

using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>Page model for viewing and managing active server-side user sessions.</summary>
[Authorize]
[SecurityHeaders]
public class ServerSideSessionsModel : PageModel
{
    private readonly ISessionManagementService? _sessionManagement;

    public ServerSideSessionsModel(ISessionManagementService? sessionManagement = null)
    {
        _sessionManagement = sessionManagement;
    }

    /// <summary>Gets or sets the paged session query result.</summary>
    public QueryResult<UserSession>? UserSessions { get; set; }

    /// <summary>Gets or sets the display name filter applied to session queries.</summary>
    [BindProperty(SupportsGet = true)]
    public string? DisplayNameFilter { get; set; }

    /// <summary>Gets or sets the session ID filter applied to session queries.</summary>
    [BindProperty(SupportsGet = true)]
    public string? SessionIdFilter { get; set; }

    /// <summary>Gets or sets the subject ID filter applied to session queries.</summary>
    [BindProperty(SupportsGet = true)]
    public string? SubjectIdFilter { get; set; }

    /// <summary>Gets or sets the pagination token for the next page of results.</summary>
    [BindProperty(SupportsGet = true)]
    public string? Token { get; set; }

    /// <summary>Gets or sets a flag indicating whether to navigate to the previous page of results.</summary>
    [BindProperty(SupportsGet = true)]
    public string? Prev { get; set; }

    /// <summary>Gets or sets the session ID to remove, bound from the remove form.</summary>
    [BindProperty]
    public string? SessionId { get; set; }

    /// <summary>Handles the GET request to query and display current user sessions.</summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task OnGetAsync()
    {
        if (_sessionManagement != null)
        {
            UserSessions = await _sessionManagement.QuerySessionsAsync(new SessionQuery
            {
                ResultsToken = Token,
                RequestPriorResults = Prev == "true",
                DisplayName = DisplayNameFilter,
                SessionId = SessionIdFilter,
                SubjectId = SubjectIdFilter,
            });
        }
    }

    /// <summary>Handles the POST request to remove a specific session.</summary>
    /// <returns>A task resolving to a redirect back to this page with the current filters applied.</returns>
    public async Task<IActionResult> OnPostAsync()
    {
        if (_sessionManagement != null)
        {
            await _sessionManagement.RemoveSessionsAsync(new RemoveSessionsContext
            {
                SessionId = SessionId,
            });
        }

        return RedirectToPage("/Account/Manage/ServerSideSessions", new
        {
            Token,
            DisplayNameFilter,
            SessionIdFilter,
            SubjectIdFilter,
            Prev,
        });
    }
}
