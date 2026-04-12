namespace Identity;

using System.Diagnostics;
using Microsoft.AspNetCore.Identity.UI.Services;
using Resend;

/// <summary>Sends transactional email via the Resend API, implementing <see cref="IEmailSender"/> for ASP.NET Core Identity.</summary>
public class EmailSender : IEmailSender
{
    private readonly IResend _resend;

    public EmailSender(IResend resend)
    {
        _resend = resend;
    }

    /// <inheritdoc/>
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var msg = new EmailMessage
        {
            From = "noreply@crgolden.com",
            Subject = subject,
            TextBody = htmlMessage,
            HtmlBody = htmlMessage
        };
        msg.To.Add(email);
        using var activity = Telemetry.ActivitySource.StartActivity("identity.resend.send_email");
        activity?.SetTag("messaging.system", "resend");
        await _resend.EmailSendAsync(msg);
    }
}
