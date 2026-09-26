namespace Identity.Tests.E2E.Infrastructure;

using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using Azure.Messaging.ServiceBus;
using Identity.Pages.Account;

public sealed class EmailCaptureSender : ServiceBusSender
{
    private readonly ConcurrentDictionary<string, ConcurrentQueue<ServiceBusMessage>> _messagesByAddress =
        new(StringComparer.OrdinalIgnoreCase);

    public static string ExtractLink(string htmlBody, AccountEmailSettings settings)
    {
        ArgumentNullException.ThrowIfNull(htmlBody);
        ArgumentNullException.ThrowIfNull(settings);

        foreach (var format in new[] { settings.ConfirmAccountHtmlFormat, settings.ResetPasswordHtmlFormat })
        {
            var marker = Guid.NewGuid().ToString("N");
            var rendered = string.Format(CultureInfo.InvariantCulture, format, marker);
            var markerAt = rendered.IndexOf(marker, StringComparison.Ordinal);
            var before = rendered[..markerAt];
            var after = rendered[(markerAt + marker.Length)..];
            if (htmlBody.StartsWith(before, StringComparison.Ordinal) && htmlBody.EndsWith(after, StringComparison.Ordinal))
            {
                return WebUtility.HtmlDecode(htmlBody[before.Length..^after.Length]);
            }
        }

        throw new InvalidOperationException("The email body matches no configured account email template.");
    }

    public override Task SendMessageAsync(ServiceBusMessage message, CancellationToken cancellationToken = default)
    {
        var queue = _messagesByAddress.GetOrAdd(message.To, _ => new ConcurrentQueue<ServiceBusMessage>());
        queue.Enqueue(message);
        return Task.CompletedTask;
    }

    public string TakeEmail(string toAddress)
    {
        var queue = _messagesByAddress.GetOrAdd(toAddress, _ => new ConcurrentQueue<ServiceBusMessage>());
        return queue.TryDequeue(out var message)
            ? message.Body.ToString()
            : throw new InvalidOperationException(
                $"No email has been sent to '{toAddress}'. Identity awaits every send before it responds, so assert the page the submit lands on before taking the email.");
    }

    public bool HasEmailFor(string toAddress) =>
        _messagesByAddress.TryGetValue(toAddress, out var queue) && !queue.IsEmpty;

    public void Clear() => _messagesByAddress.Clear();
}
