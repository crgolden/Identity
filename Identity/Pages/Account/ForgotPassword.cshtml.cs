namespace Identity.Pages.Account;

using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Azure;

/// <summary>Page model for the Forgot Password page.</summary>
[AllowAnonymous]
public class ForgotPasswordModel : PageModel
{
    private const string From = "noreply@crgolden.com";
    private readonly UserManager<IdentityUser<Guid>> _userManager;
    private readonly ServiceBusClient _serviceBusClient;

    public ForgotPasswordModel(UserManager<IdentityUser<Guid>> userManager, IAzureClientFactory<ServiceBusClient> serviceBusClientFactory)
    {
        ArgumentNullException.ThrowIfNull(userManager);
        ArgumentNullException.ThrowIfNull(serviceBusClientFactory);
        _userManager = userManager;
        _serviceBusClient = serviceBusClientFactory.CreateClient("crgolden");
    }

    [BindProperty]
    public InputModel Input { get; set; } = new InputModel();

    /// <summary>Handles the POST request to send a password reset email.</summary>
    /// <returns>A task that resolves to the page result or a redirect.</returns>
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid || IsNullOrWhiteSpace(Input.Email))
        {
            return Page();
        }

        var user = await _userManager.FindByEmailAsync(Input.Email);
        if (user is null || !(await _userManager.IsEmailConfirmedAsync(user)))
        {
            return RedirectToPage("./ForgotPasswordConfirmation");
        }

        var code = await _userManager.GeneratePasswordResetTokenAsync(user);
        var input = UTF8.GetBytes(code);
        code = Base64UrlEncode(input);
        var callbackUrl = Url.Page(
            "/Account/ResetPassword",
            pageHandler: null,
            values: new { code, email = Input.Email },
            protocol: Request.Scheme);

        if (!IsNullOrWhiteSpace(callbackUrl))
        {
            var htmlMessage = $"Please reset your password by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.";
            var message = new ServiceBusMessage(htmlMessage)
            {
                ReplyTo = From,
                Subject = "Reset Password",
                To = Input.Email
            };
            var serviceBusSender = _serviceBusClient.CreateSender("email");
            await serviceBusSender.SendMessageAsync(message, HttpContext.RequestAborted);
        }

        return RedirectToPage("./ForgotPasswordConfirmation");
    }

    /// <summary>Provides the form input values bound from the request.</summary>
    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string? Email { get; set; }
    }
}
