namespace Identity.Tests.Unit.Pages.Account;

using Azure.Messaging.ServiceBus;
using Identity.Pages.Account;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Azure;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class ResendEmailConfirmationTests
{
    public static IEnumerable<object[]> EmailTestCases()
    {
        yield return [string.Empty];
        yield return [Generated.NewWhitespaceValue()];
        yield return [Generated.NewEmailAddress()];
        yield return [Generated.NewOverlongValue()];
        yield return [Generated.NewControlAndSymbolValue()];
    }

    public static TheoryData<string> UnknownEmailAddresses() => new()
    {
        Generated.NewEmailAddress(),
    };

    [Fact]
    public async Task OnPostAsync_ModelStateInvalid_ReturnsPageWithoutCallingDependencies()
    {
        // Arrange
        var mockUserManager = MockHelpers.MockUserManager();

        var (factory, senderMock) = CreateSenderFactoryWithMock();

        var model = new ResendEmailConfirmation(mockUserManager.Object, factory, TestValues.NewAccountEmailSettings())
        {
            Input = new AccountEmailPageBase.InputModel { Email = Generated.NewEmailAddress() }
        };

        model.PageContext = new PageContext { HttpContext = new DefaultHttpContext() };
        model.ModelState.AddModelError(Generated.NewClaimType(), Generated.NewFailureReason());

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        mockUserManager.Verify(m => m.FindByEmailAsync(It.IsAny<string>()), Times.Never);
        senderMock.Verify(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [MemberData(nameof(UnknownEmailAddresses))]
    public async Task OnPostAsync_UserNotFound_AddsModelErrorAndReturnsPage(string email)
    {
        // Arrange
        var mockUserManager = MockHelpers.MockUserManager();

        mockUserManager
            .Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((IdentityUser<Guid>?)null);

        var (factory, senderMock) = CreateSenderFactoryWithMock();

        var model = new ResendEmailConfirmation(mockUserManager.Object, factory, TestValues.NewAccountEmailSettings())
        {
            Input = new AccountEmailPageBase.InputModel { Email = email }
        };

        model.PageContext = new PageContext { HttpContext = new DefaultHttpContext() };
        model.PageContext.HttpContext.Request.Scheme = Uri.UriSchemeHttps;

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);

        Assert.True(model.ModelState.ErrorCount > 0);
        var entry = model.ModelState[string.Empty];
        Assert.NotNull(entry);
        Assert.NotEmpty(entry.Errors);
        Assert.Equal(ResendEmailConfirmation.VerificationEmailSentMessage, entry.Errors[0].ErrorMessage);

        mockUserManager.Verify(m => m.FindByEmailAsync(It.Is<string>(s => s == email)), Times.Once);
        senderMock.Verify(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static (IAzureClientFactory<ServiceBusClient> Factory, Mock<ServiceBusSender> SenderMock) CreateSenderFactoryWithMock()
    {
        var senderMock = new Mock<ServiceBusSender>(MockBehavior.Strict);
        senderMock.Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var clientMock = new Mock<ServiceBusClient>(MockBehavior.Strict);
        clientMock.Setup(c => c.CreateSender(ServiceBusNames.EmailQueueName)).Returns(senderMock.Object);
        var factoryMock = new Mock<IAzureClientFactory<ServiceBusClient>>(MockBehavior.Strict);
        factoryMock.Setup(f => f.CreateClient(ServiceBusNames.ClientName)).Returns(clientMock.Object);
        return (factoryMock.Object, senderMock);
    }
}
