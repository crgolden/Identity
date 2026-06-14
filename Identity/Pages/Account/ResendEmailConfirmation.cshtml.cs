namespace Identity.Pages.Account;

using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Azure;

/// <summary>Page model for the Resend Email Confirmation page.</summary>
[AllowAnonymous]
public class ResendEmailConfirmationModel : PageModel
{
    private const string From = "noreply@crgolden.com";
    private readonly UserManager<IdentityUser<Guid>> _userManager;
    private readonly ServiceBusClient _serviceBusClient;

    public ResendEmailConfirmationModel(UserManager<IdentityUser<Guid>> userManager, IAzureClientFactory<ServiceBusClient> serviceBusClientFactory)
    {
        _userManager = userManager;
        _serviceBusClient = serviceBusClientFactory.CreateClient("crgolden");
    }

    [BindProperty]
    public InputModel Input { get; set; } = new InputModel();

    /// <summary>Handles the POST request to resend the email confirmation link.</summary>
    /// <returns>A task that resolves to the page result or a redirect.</returns>
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid || IsNullOrWhiteSpace(Input.Email))
        {
            return Page();
        }

        var user = await _userManager.FindByEmailAsync(Input.Email);
        if (user is null)
        {
            ModelState.AddModelError(Empty, "Verification email sent. Please check your email.");
            return Page();
        }

        var userId = await _userManager.GetUserIdAsync(user);
        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var input = UTF8.GetBytes(code);
        code = Base64UrlEncode(input);
        var callbackUrl = Url.Page(
            "/Account/ConfirmEmail",
            pageHandler: null,
            values: new { userId, code },
            protocol: Request.Scheme);
        if (!IsNullOrWhiteSpace(callbackUrl))
        {
            var htmlMessage = $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.";
            var message = new ServiceBusMessage(htmlMessage)
            {
                ReplyTo = From,
                Subject = "Confirm your email",
                To = Input.Email
            };
            var serviceBusSender = _serviceBusClient.CreateSender("email");
            await serviceBusSender.SendMessageAsync(message, HttpContext.RequestAborted);
        }

        ModelState.AddModelError(Empty, "Verification email sent. Please check your email.");
        return Page();
    }

    /// <summary>Provides the form input values bound from the request.</summary>
    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string? Email { get; set; }
    }
}
