namespace Identity.Tests.E2E.Infrastructure;

internal static class ServiceBusConstants
{
    internal const string ConnectionStringFormat =
        "Endpoint=sb://{0}.servicebus.windows.net/;SharedAccessKeyName={1};SharedAccessKey={2}";
}
