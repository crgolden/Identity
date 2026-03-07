using System.Diagnostics;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Identity.Pages
{
    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public class ErrorModel : PageModel
    {
        private readonly IIdentityServerInteractionService _interactionService;

        public ErrorModel(IIdentityServerInteractionService interactionService)
        {
            _interactionService = interactionService;
        }

        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        public async Task OnGetAsync(string? errorId = null)
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            if (!string.IsNullOrWhiteSpace(errorId))
            {
                var error = await _interactionService.GetErrorContextAsync(errorId);
            }
        }
    }

}
