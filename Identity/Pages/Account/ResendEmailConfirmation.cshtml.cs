namespace Identity.Pages.Account;

using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Azure;

[AllowAnonymous]
public class ResendEmailConfirmation : AccountEmailPageBase
{
    internal const string VerificationEmailSentMessage =
        "Verification email sent. Please check your email.";

    public ResendEmailConfirmation(
        UserManager<IdentityUser<Guid>> userManager,
        IAzureClientFactory<ServiceBusClient> serviceBusClientFactory,
        AccountEmailSettings accountEmailSettings)
        : base(userManager, serviceBusClientFactory, accountEmailSettings)
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid || IsNullOrWhiteSpace(Input.Email))
        {
            return Page();
        }

        var user = await UserManager.FindByEmailAsync(Input.Email);
        if (user is null)
        {
            ModelState.AddModelError(Empty, VerificationEmailSentMessage);
            return Page();
        }

        var userId = await UserManager.GetUserIdAsync(user);
        var code = await UserManager.GenerateEmailConfirmationTokenAsync(user);
        var input = UTF8.GetBytes(code);
        code = Base64UrlEncode(input);
        var callbackUrl = Url.Page(
            "/Account/ConfirmEmail",
            pageHandler: null,
            values: new { userId, code },
            protocol: Request.Scheme);
        if (!IsNullOrWhiteSpace(callbackUrl))
        {
            await SendAccountEmailAsync(AccountEmailSettings.ConfirmAccountHtml(callbackUrl), UserMessages.ConfirmEmailSubject);
        }

        ModelState.AddModelError(Empty, VerificationEmailSentMessage);
        return Page();
    }
}
