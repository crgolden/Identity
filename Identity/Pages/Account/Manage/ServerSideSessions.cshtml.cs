namespace Identity.Pages.Account.Manage;

using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

[Authorize]
[SecurityHeaders]
public class ServerSideSessionsModel : PageModel
{
    private readonly ISessionManagementService? _sessionManagement;

    public ServerSideSessionsModel(ISessionManagementService? sessionManagement = null)
    {
        _sessionManagement = sessionManagement;
    }

    public QueryResult<UserSession>? UserSessions { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? DisplayNameFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SessionIdFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SubjectIdFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Token { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Prev { get; set; }

    [BindProperty]
    public string? SessionId { get; set; }

    public async Task OnGetAsync()
    {
        if (_sessionManagement != null)
        {
            UserSessions = await _sessionManagement.QuerySessionsAsync(
                new SessionQuery
                {
                    ResultsToken = Token,
                    RequestPriorResults = Prev == "true",
                    DisplayName = DisplayNameFilter,
                    SessionId = SessionIdFilter,
                    SubjectId = SubjectIdFilter,
                },
                HttpContext.RequestAborted);
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (_sessionManagement != null)
        {
            await _sessionManagement.RemoveSessionsAsync(
                new RemoveSessionsContext
                {
                    SessionId = SessionId,
                },
                HttpContext.RequestAborted);
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