namespace Identity;

using Microsoft.AspNetCore.Identity.UI.Services;
using Resend;

public class EmailSender : IEmailSender
{
    private readonly IResend _resend;

    public EmailSender(IResend resend)
    {
        _resend = resend;
    }

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var msg = new EmailMessage
        {
            From = "noreply@crgolden.com",
            Subject = subject,
            TextBody = htmlMessage,
            HtmlBody = htmlMessage
        };
        msg.To.Add(email);
        return _resend.EmailSendAsync(msg);
    }
}
