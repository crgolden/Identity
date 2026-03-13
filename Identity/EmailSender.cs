namespace Identity;

using Microsoft.AspNetCore.Identity.UI.Services;
using Resend;

/// <inheritdoc/>
public class EmailSender : IEmailSender
{
    private readonly IResend _resend;

    /// <inheritdoc cref="IEmailSender"/>
    public EmailSender(IResend resend)
    {
        _resend = resend;
    }

    /// <inheritdoc/>
    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var msg = new EmailMessage
        {
            From = "noreply@crgolden.com",
            Subject = subject,
            HtmlBody = htmlMessage,
            TextBody = htmlMessage,
        };
        msg.To.Add(email);
        return _resend.EmailSendAsync(msg);
    }
}
