namespace Identity.Pages;

using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

[AllowAnonymous]
[SecurityHeaders]
public class CibaModel : PageModel
{
    private readonly IBackchannelAuthenticationInteractionService _backchannelInteraction;

    public CibaModel(IBackchannelAuthenticationInteractionService backchannelInteraction)
    {
        ThrowIfNull(backchannelInteraction);
        _backchannelInteraction = backchannelInteraction;
    }

    public BackchannelUserLoginRequest? LoginRequest { get; set; }

    public async Task<IActionResult> OnGetAsync(string? id)
    {
        if (IsNullOrWhiteSpace(id))
        {
            return RedirectToPage("/Error");
        }

        var result = await _backchannelInteraction.GetLoginRequestByInternalIdAsync(id, HttpContext.RequestAborted);
        if (result == null)
        {
            return RedirectToPage("/Error");
        }

        LoginRequest = result;
        return Page();
    }
}