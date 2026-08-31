namespace Identity.Tests.Unit.Pages.Account;

using Azure.Messaging.ServiceBus;
using Identity.Pages.Account;
using Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Azure;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class ForgotPasswordModelTests
{
    [Fact]
    public async Task OnPostAsync_ModelStateInvalid_ReturnsPage()
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();

        var (factory, senderMock) = CreateSenderFactoryWithMock();

        var model = new ForgotPasswordModel(userManagerMock.Object, factory);

        model.PageContext = new PageContext { HttpContext = new DefaultHttpContext() };
        model.ModelState.AddModelError("Email", "Required");

        model.Input = new ForgotPasswordModel.InputModel { Email = TestValues.NewEmailAddress() };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        userManagerMock.Verify(um => um.FindByEmailAsync(It.IsAny<string>()), Times.Never);
        senderMock.Verify(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_UnknownEmail_RedirectsToConfirmationWithoutSendingEmail()
    {
        // Arrange
        var unknownEmail = TestValues.NewEmailAddress();
        var userManagerMock = MockHelpers.MockUserManager();

        var (factory, senderMock) = CreateSenderFactoryWithMock();

        var model = new ForgotPasswordModel(userManagerMock.Object, factory);

        model.PageContext = new PageContext { HttpContext = new DefaultHttpContext() };
        model.Input = new ForgotPasswordModel.InputModel { Email = unknownEmail };

        userManagerMock.Setup(um => um.FindByEmailAsync(unknownEmail))
            .ReturnsAsync((IdentityUser<Guid>?)null)
            .Verifiable();

        // Act
        var result = await model.OnPostAsync();

        // Assert
        AssertRedirectedToConfirmationWithoutEmail(result, senderMock);
        userManagerMock.Verify();
    }

    [Fact]
    public async Task OnPostAsync_UnconfirmedEmail_RedirectsToConfirmationWithoutSendingEmail()
    {
        // Arrange
        var unconfirmedEmail = TestValues.NewEmailAddress();
        var userManagerMock = MockHelpers.MockUserManager();

        var (factory, senderMock) = CreateSenderFactoryWithMock();

        var model = new ForgotPasswordModel(userManagerMock.Object, factory);

        model.PageContext = new PageContext { HttpContext = new DefaultHttpContext() };
        model.Input = new ForgotPasswordModel.InputModel { Email = unconfirmedEmail };

        var unconfirmedUser = new IdentityUser<Guid>
        {
            UserName = unconfirmedEmail,
            Email = unconfirmedEmail
        };
        userManagerMock.Setup(um => um.FindByEmailAsync(unconfirmedEmail))
            .ReturnsAsync(unconfirmedUser)
            .Verifiable();
        userManagerMock.Setup(um => um.IsEmailConfirmedAsync(unconfirmedUser))
            .ReturnsAsync(false)
            .Verifiable();

        // Act
        var result = await model.OnPostAsync();

        // Assert
        AssertRedirectedToConfirmationWithoutEmail(result, senderMock);
        userManagerMock.Verify();
    }

    private static void AssertRedirectedToConfirmationWithoutEmail(
        IActionResult result,
        Mock<ServiceBusSender> senderMock)
    {
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./ForgotPasswordConfirmation", redirect.PageName);
        senderMock.Verify(
            s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static (IAzureClientFactory<ServiceBusClient> factory, Mock<ServiceBusSender> senderMock) CreateSenderFactoryWithMock()
    {
        var senderMock = new Mock<ServiceBusSender>(MockBehavior.Strict);
        senderMock.Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var clientMock = new Mock<ServiceBusClient>(MockBehavior.Strict);
        clientMock.Setup(c => c.CreateSender("email")).Returns(senderMock.Object);
        var factoryMock = new Mock<IAzureClientFactory<ServiceBusClient>>(MockBehavior.Strict);
        factoryMock.Setup(f => f.CreateClient("crgolden")).Returns(clientMock.Object);
        return (factoryMock.Object, senderMock);
    }
}