namespace Identity.Tests.Infrastructure;

using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;

/// <summary>
/// Test double for <see cref="IEmailSender"/> and <see cref="IEmailSender{TUser}"/> that
/// captures sent emails in memory so E2E tests can extract confirmation/reset links.
/// Registered as Singleton so the same instance is shared between the app and test code.
/// </summary>
public sealed class EmailCaptureService : IEmailSender, IEmailSender<IdentityUser<Guid>>
{
    private readonly ConcurrentQueue<CapturedEmail> _emails = new();

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        _emails.Enqueue(new CapturedEmail(email, subject, htmlMessage));
        return Task.CompletedTask;
    }

    public Task SendConfirmationLinkAsync(IdentityUser<Guid> user, string email, string confirmationLink) =>
        SendEmailAsync(email, "Confirm your email", $"<a href='{confirmationLink}'>Confirm</a>");

    public Task SendPasswordResetLinkAsync(IdentityUser<Guid> user, string email, string resetLink) =>
        SendEmailAsync(email, "Reset your password", $"<a href='{resetLink}'>Reset</a>");

    public Task SendPasswordResetCodeAsync(IdentityUser<Guid> user, string email, string resetCode) =>
        SendEmailAsync(email, "Reset your password", $"Your code: {resetCode}");

    /// <summary>
    /// Waits until an email is received for <paramref name="toAddress"/> or throws <see cref="TimeoutException"/>.
    /// </summary>
    public async Task<CapturedEmail> WaitForEmailAsync(string toAddress, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(10));
        while (DateTime.UtcNow < deadline)
        {
            var match = _emails.FirstOrDefault(e =>
                string.Equals(e.To, toAddress, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
                return match;
            await Task.Delay(100);
        }
        throw new TimeoutException($"No email received for '{toAddress}' within {timeout ?? TimeSpan.FromSeconds(10)}.");
    }

    /// <summary>
    /// Extracts the first URL from an HTML body whose value matches <paramref name="urlPattern"/>.
    /// </summary>
    public static string ExtractLink(string htmlBody, string urlPattern)
    {
        var matches = Regex.Matches(htmlBody, $@"href=['""]({urlPattern}[^'""]*)['""]");
        if (matches.Count == 0)
            throw new InvalidOperationException($"No link matching '{urlPattern}' found in email body.");
        return matches[0].Groups[1].Value;
    }

    public void Clear() => _emails.Clear();
}

public sealed record CapturedEmail(string To, string Subject, string HtmlBody);
