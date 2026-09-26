namespace Identity.Tests.E2E.Infrastructure;

using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Azure;

internal sealed class TestServiceBusClientFactory : IAzureClientFactory<ServiceBusClient>
{
    private readonly TestServiceBusClient _client;

    public TestServiceBusClientFactory(ServiceBusSender sender) => _client = new TestServiceBusClient(sender);

    public ServiceBusClient CreateClient(string name) => _client;
}
