namespace Identity.Tests.Infrastructure;

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

    public async Task<CapturedEmail> WaitForEmailAsync(string toAddress, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(10));
        var queue = _messagesByAddress.GetOrAdd(toAddress, _ => new ConcurrentQueue<ServiceBusMessage>());
        while (DateTime.UtcNow < deadline)
        {
            if (queue.TryDequeue(out var msg))
            {
                return new CapturedEmail(msg.To, msg.Subject, msg.Body.ToString());
            }

            await Task.Delay(100);
        }

        throw new TimeoutException($"No email received for '{toAddress}' within {timeout ?? TimeSpan.FromSeconds(10)}.");
    }

    public void Clear() => _messagesByAddress.Clear();
}

public sealed record CapturedEmail(string To, string Subject, string HtmlBody);
