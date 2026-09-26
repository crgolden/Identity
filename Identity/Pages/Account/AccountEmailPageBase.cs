namespace Identity.Pages.Account;

using System.ComponentModel.DataAnnotations;
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Azure;

public abstract class AccountEmailPageBase : PageModel
{
    private readonly ServiceBusClient _serviceBusClient;

    protected AccountEmailPageBase(
        UserManager<IdentityUser<Guid>> userManager,
        IAzureClientFactory<ServiceBusClient> serviceBusClientFactory,
        AccountEmailSettings accountEmailSettings)
    {
        ThrowIfNull(userManager);
        ThrowIfNull(serviceBusClientFactory);
        ThrowIfNull(accountEmailSettings);
        UserManager = userManager;
        _serviceBusClient = serviceBusClientFactory.CreateClient(ServiceBusNames.ClientName);
        AccountEmailSettings = accountEmailSettings;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new InputModel();

    protected UserManager<IdentityUser<Guid>> UserManager { get; }

    protected AccountEmailSettings AccountEmailSettings { get; }

    protected async Task SendAccountEmailAsync(string htmlBody, string subject)
    {
        var message = new ServiceBusMessage(htmlBody)
        {
            ReplyTo = AccountEmailSettings.Sender,
            Subject = subject,
            To = Input.Email
        };
        var serviceBusSender = _serviceBusClient.CreateSender(ServiceBusNames.EmailQueueName);
        await serviceBusSender.SendMessageAsync(message, HttpContext.RequestAborted);
    }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string? Email { get; set; }
    }
}
