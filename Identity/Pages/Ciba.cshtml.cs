namespace Identity.Pages;

using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

[AllowAnonymous]
public class Ciba : PageModel
{
    private readonly IBackchannelAuthenticationInteractionService _backchannelInteraction;

    public Ciba(IBackchannelAuthenticationInteractionService backchannelInteraction)
    {
        ThrowIfNull(backchannelInteraction);
        _backchannelInteraction = backchannelInteraction;
    }

    public BackchannelUserLoginRequest? LoginRequest { get; set; }

    public async Task<IActionResult> OnGetAsync(string? id)
    {
        if (IsNullOrWhiteSpace(id))
        {
            return RedirectToPage(PageRoutes.Error);
        }

        var result = await _backchannelInteraction.GetLoginRequestByInternalIdAsync(id, HttpContext.RequestAborted);
        if (result == null)
        {
            return RedirectToPage(PageRoutes.Error);
        }

        LoginRequest = result;
        return Page();
    }
}
