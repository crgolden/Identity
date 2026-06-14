namespace Identity.Pages.Account.Manage;

using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Azure;
using static String;
using static System.Text.Encoding;
using static Microsoft.AspNetCore.WebUtilities.WebEncoders;

/// <summary>Page model for the Email management page.</summary>
public class EmailModel : PageModel
{
    private const string From = "noreply@crgolden.com";
    private readonly UserManager<IdentityUser<Guid>> _userManager;
    private readonly ServiceBusClient _serviceBusClient;

    public EmailModel(UserManager<IdentityUser<Guid>> userManager, IAzureClientFactory<ServiceBusClient> serviceBusClientFactory)
    {
        _userManager = userManager;
        _serviceBusClient = serviceBusClientFactory.CreateClient("crgolden");
    }

    public string? Email { get; set; }

    public bool IsEmailConfirmed { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new InputModel();

    /// <summary>Handles the GET request to display the email management page.</summary>
    /// <returns>A task that resolves to the page result or a redirect.</returns>
    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        var email = await _userManager.GetEmailAsync(user);
        Email = email;
        Input = new InputModel
        {
            NewEmail = email
        };

        IsEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);
        return Page();
    }

    /// <summary>Handles the POST request to initiate an email address change.</summary>
    /// <returns>A task that resolves to the page result or a redirect.</returns>
    public async Task<IActionResult> OnPostChangeEmailAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        var email = await _userManager.GetEmailAsync(user);
        if (!ModelState.IsValid)
        {
            Email = email;
            Input = new InputModel
            {
                NewEmail = email
            };

            IsEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);
            return Page();
        }

        if (!IsNullOrWhiteSpace(Input.NewEmail) && !string.Equals(Input.NewEmail, email))
        {
            var userId = await _userManager.GetUserIdAsync(user);
            var code = await _userManager.GenerateChangeEmailTokenAsync(user, Input.NewEmail);
            var input = UTF8.GetBytes(code);
            code = Base64UrlEncode(input);
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmailChange",
                pageHandler: null,
                values: new { userId, email = Input.NewEmail, code },
                protocol: Request.Scheme);
            if (!IsNullOrWhiteSpace(callbackUrl))
            {
                var link = HtmlEncoder.Default.Encode(callbackUrl);
                var htmlMessage = $"Please confirm your account by <a href='{link}'>clicking here</a>.";
                var sbMessage = new ServiceBusMessage(htmlMessage)
                {
                    ReplyTo = From,
                    Subject = "Confirm your email",
                    To = Input.NewEmail
                };
                var serviceBusSender = _serviceBusClient.CreateSender("email");
                await serviceBusSender.SendMessageAsync(sbMessage, HttpContext.RequestAborted);
            }

            StatusMessage = "Confirmation link to change email sent. Please check your email.";
            return RedirectToPage();
        }

        StatusMessage = "Your email is unchanged.";
        return RedirectToPage();
    }

    /// <summary>Handles the POST request to resend the email verification link.</summary>
    /// <returns>A task that resolves to the page result or a redirect.</returns>
    public async Task<IActionResult> OnPostSendVerificationEmailAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        var email = await _userManager.GetEmailAsync(user);
        if (!ModelState.IsValid)
        {
            Email = email;
            Input = new InputModel
            {
                NewEmail = email
            };

            IsEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);
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
        if (!IsNullOrWhiteSpace(email) && !IsNullOrWhiteSpace(callbackUrl))
        {
            var link = HtmlEncoder.Default.Encode(callbackUrl);
            var htmlMessage = $"Please confirm your account by <a href='{link}'>clicking here</a>.";
            var sbMessage = new ServiceBusMessage(htmlMessage)
            {
                ReplyTo = From,
                Subject = "Confirm your email",
                To = email
            };
            var serviceBusSender = _serviceBusClient.CreateSender("email");
            await serviceBusSender.SendMessageAsync(sbMessage, HttpContext.RequestAborted);
        }

        StatusMessage = "Verification email sent. Please check your email.";
        return RedirectToPage();
    }

    /// <summary>Provides the form input values bound from the request.</summary>
    public class InputModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "New email")]
        public string? NewEmail { get; set; }
    }
}
