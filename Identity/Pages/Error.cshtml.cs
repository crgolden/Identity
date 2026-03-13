namespace Identity.Pages;

using System.Diagnostics;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

[AllowAnonymous]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel : PageModel
{
    private readonly ILogger _logger;
    private readonly IIdentityServerInteractionService _interactionService;

    /// <inheritdoc cref="PageModel" />
    public ErrorModel(
        ILogger<ErrorModel> logger,
        IIdentityServerInteractionService interactionService)
    {
        _logger = logger;
        _interactionService = interactionService;
    }

    public string? RequestId { get; set; }

    public bool ShowRequestId => !IsNullOrWhiteSpace(RequestId);

    public async Task OnGetAsync(string? errorId = null)
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        if (!IsNullOrWhiteSpace(errorId))
        {
            var errorMessage = await _interactionService.GetErrorContextAsync(errorId);
            _logger.LogError("{ErrorMessage}", errorMessage);
        }
    }
}
