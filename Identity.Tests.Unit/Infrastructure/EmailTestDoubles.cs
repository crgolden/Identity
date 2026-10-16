namespace Identity.Tests.Unit.Infrastructure;

using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Azure;
using Moq;

internal static class EmailTestDoubles
{
    internal static IUrlHelper UrlHelperRecordingPages(List<string> routedPages, bool callbackResolves)
    {
        var urlHelperMock = new Mock<IUrlHelper>(MockBehavior.Strict);
        urlHelperMock.SetupGet(u => u.ActionContext).Returns(new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()));
        urlHelperMock.Setup(u => u.RouteUrl(It.IsAny<UrlRouteContext>()))
            .Callback<UrlRouteContext>(context => RoutedPages.Record(context, routedPages))
            .Returns(callbackResolves ? Generated.NewCallbackAddress() : null);
        return urlHelperMock.Object;
    }

    internal static IAzureClientFactory<ServiceBusClient> ServiceBusCapturing(List<ServiceBusMessage> sent)
    {
        var senderMock = new Mock<ServiceBusSender>(MockBehavior.Strict);
        senderMock.Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Callback<ServiceBusMessage, CancellationToken>((message, _) => sent.Add(message))
            .Returns(Task.CompletedTask);
        var clientMock = new Mock<ServiceBusClient>(MockBehavior.Strict);
        clientMock.Setup(c => c.CreateSender(ServiceBusNames.EmailQueueName)).Returns(senderMock.Object);
        var factoryMock = new Mock<IAzureClientFactory<ServiceBusClient>>(MockBehavior.Strict);
        factoryMock.Setup(f => f.CreateClient(ServiceBusNames.ClientName)).Returns(clientMock.Object);
        return factoryMock.Object;
    }
}
