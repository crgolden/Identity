using Microsoft.AspNetCore.Identity.UI.Services;
using Resend;

namespace Identity;

public class EmailSender : IEmailSender
{
    private readonly IResend _resend;

    public EmailSender(IResend resend)
    {
        _resend = resend;
    }

    public Task SendEmailAsync(string toEmail, string subject, string message)
    {
        var msg = new EmailMessage
        {
            From = "noreply@crgolden.com",
            Subject = subject,
            TextBody = message,
            HtmlBody =message
        };
        msg.To.Add(toEmail);
        return _resend.EmailSendAsync(msg);
    }
}
