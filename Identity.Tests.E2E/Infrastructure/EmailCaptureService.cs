namespace Identity.Tests.E2E.Infrastructure;

using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Azure.Messaging.ServiceBus;

public sealed class EmailCaptureSender : ServiceBusSender
{
    private readonly ConcurrentDictionary<string, ConcurrentQueue<ServiceBusMessage>> _messagesByAddress =
        new(StringComparer.OrdinalIgnoreCase);

    public static string ExtractLink(string htmlBody, string urlPattern)
    {
        var matches = Regex.Matches(htmlBody, $@"href=['""]({urlPattern}[^'""]*)['""]");
        if (matches.Count == 0)
        {
            throw new InvalidOperationException($"No link matching '{urlPattern}' found in email body.");
        }

        return System.Net.WebUtility.HtmlDecode(matches[0].Groups[1].Value);
    }

    public override Task SendMessageAsync(ServiceBusMessage message, CancellationToken cancellationToken = default)
    {
        var queue = _messagesByAddress.GetOrAdd(message.To, _ => new ConcurrentQueue<ServiceBusMessage>());
        queue.Enqueue(message);
        return Task.CompletedTask;
    }

    public CapturedEmail TakeEmail(string toAddress)
    {
        var queue = _messagesByAddress.GetOrAdd(toAddress, _ => new ConcurrentQueue<ServiceBusMessage>());
        return queue.TryDequeue(out var message)
            ? new CapturedEmail(message.To, message.Subject, message.Body.ToString())
            : throw new InvalidOperationException(
                $"No email has been sent to '{toAddress}'. Identity awaits every send before it responds, so assert the page the submit lands on before taking the email.");
    }

    public void Clear() => _messagesByAddress.Clear();
}

public sealed record CapturedEmail(string To, string Subject, string HtmlBody);