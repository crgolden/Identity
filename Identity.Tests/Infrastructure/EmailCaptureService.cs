namespace Identity.Tests.Infrastructure;

using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;

/// <summary>
/// Test double for <see cref="IEmailSender"/> and <see cref="IEmailSender{TUser}"/> that
/// captures sent emails in memory so E2E tests can extract confirmation/reset links.
/// Registered as Singleton so the same instance is shared between the app and test code.
/// Emails are keyed per recipient address and dequeued on consumption so that successive
/// calls to <see cref="WaitForEmailAsync"/> for the same address each return the next
/// unread email rather than the same first one.
/// </summary>
public sealed class EmailCaptureService : IEmailSender, IEmailSender<IdentityUser<Guid>>
{
    private readonly ConcurrentDictionary<string, ConcurrentQueue<CapturedEmail>> _emailsByAddress =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Extracts the first URL from an HTML body whose value matches <paramref name="urlPattern"/>.
    /// HTML entities in the href are decoded before the URL is returned.
    /// </summary>
    public static string ExtractLink(string htmlBody, string urlPattern)
    {
        var matches = Regex.Matches(htmlBody, $@"href=['""]({urlPattern}[^'""]*)['""]");
        if (matches.Count == 0)
        {
            throw new InvalidOperationException($"No link matching '{urlPattern}' found in email body.");
        }

        return System.Net.WebUtility.HtmlDecode(matches[0].Groups[1].Value);
    }

    /// <inheritdoc/>
    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var queue = _emailsByAddress.GetOrAdd(email, _ => new ConcurrentQueue<CapturedEmail>());
        queue.Enqueue(new CapturedEmail(email, subject, htmlMessage));
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SendConfirmationLinkAsync(IdentityUser<Guid> user, string email, string confirmationLink) =>
        SendEmailAsync(email, "Confirm your email", $"<a href='{confirmationLink}'>Confirm</a>");

    /// <inheritdoc/>
    public Task SendPasswordResetLinkAsync(IdentityUser<Guid> user, string email, string resetLink) =>
        SendEmailAsync(email, "Reset your password", $"<a href='{resetLink}'>Reset</a>");

    /// <inheritdoc/>
    public Task SendPasswordResetCodeAsync(IdentityUser<Guid> user, string email, string resetCode) =>
        SendEmailAsync(email, "Reset your password", $"Your code: {resetCode}");

    /// <summary>
    /// Waits until an email is received for <paramref name="toAddress"/>, dequeues and returns it.
    /// Throws <see cref="TimeoutException"/> if no email arrives within the timeout.
    /// </summary>
    public async Task<CapturedEmail> WaitForEmailAsync(string toAddress, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(10));
        var queue = _emailsByAddress.GetOrAdd(toAddress, _ => new ConcurrentQueue<CapturedEmail>());
        while (DateTime.UtcNow < deadline)
        {
            if (queue.TryDequeue(out var email))
            {
                return email;
            }

            await Task.Delay(100);
        }

        throw new TimeoutException($"No email received for '{toAddress}' within {timeout ?? TimeSpan.FromSeconds(10)}.");
    }

    /// <summary>Clears all captured emails.</summary>
    public void Clear() => _emailsByAddress.Clear();
}

/// <summary>Represents an email captured by <see cref="EmailCaptureService"/> during testing.</summary>
public sealed record CapturedEmail(string To, string Subject, string HtmlBody);