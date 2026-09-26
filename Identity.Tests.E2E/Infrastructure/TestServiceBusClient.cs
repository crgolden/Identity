namespace Identity.Tests.E2E.Infrastructure;

using Azure.Messaging.ServiceBus;

internal sealed class TestServiceBusClient : ServiceBusClient
{
    private readonly ServiceBusSender _sender;

    public TestServiceBusClient(ServiceBusSender sender) => _sender = sender;

    public override ServiceBusSender CreateSender(string queueOrTopicName) => _sender;
}
