namespace Identity.Pages.Account;

using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Azure;

[AllowAnonymous]
public class ForgotPassword : AccountEmailPageBase
{
    public ForgotPassword(
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
        if (user is null || !(await UserManager.IsEmailConfirmedAsync(user)))
        {
            return RedirectToPage(PageRoutes.SiblingForgotPasswordConfirmation);
        }

        var code = await UserManager.GeneratePasswordResetTokenAsync(user);
        var input = UTF8.GetBytes(code);
        code = Base64UrlEncode(input);
        var callbackUrl = Url.Page(
            "/Account/ResetPassword",
            pageHandler: null,
            values: new { code, email = Input.Email },
            protocol: Request.Scheme);

        if (!IsNullOrWhiteSpace(callbackUrl))
        {
            await SendAccountEmailAsync(AccountEmailSettings.ResetPasswordHtml(callbackUrl), UserMessages.ResetPasswordSubject);
        }

        return RedirectToPage(PageRoutes.SiblingForgotPasswordConfirmation);
    }
}
