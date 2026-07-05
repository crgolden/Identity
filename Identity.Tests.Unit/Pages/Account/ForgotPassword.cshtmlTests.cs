namespace Identity.Tests.Unit.Pages.Account;
using Infrastructure;

using Azure.Messaging.ServiceBus;
using Identity.Pages.Account;
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

        // Make ModelState invalid
        model.PageContext = new PageContext { HttpContext = new DefaultHttpContext() };
        model.ModelState.AddModelError("Email", "Required");

        model.Input = new ForgotPasswordModel.InputModel { Email = "user@example.com" };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);

        // Ensure no calls were made to user manager or email sender
        userManagerMock.Verify(um => um.FindByEmailAsync(It.IsAny<string>()), Times.Never);
        senderMock.Verify(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    public async Task OnPostAsync_UserNullOrUnconfirmed_RedirectsToConfirmation_DoesNotSendEmail(bool userExists, bool isConfirmed)
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();

        var (factory, senderMock) = CreateSenderFactoryWithMock();

        var model = new ForgotPasswordModel(userManagerMock.Object, factory);

        model.PageContext = new PageContext { HttpContext = new DefaultHttpContext() };
        model.Input = new ForgotPasswordModel.InputModel { Email = "user@example.com" };

        if (!userExists)
        {
            userManagerMock.Setup(um => um.FindByEmailAsync(It.Is<string>(s => s == model.Input.Email)))
                .ReturnsAsync((IdentityUser<Guid>?)null)
                .Verifiable();
        }
        else
        {
            var user = new IdentityUser<Guid> { UserName = "u", Email = model.Input.Email };
            userManagerMock.Setup(um => um.FindByEmailAsync(It.Is<string>(s => s == model.Input.Email)))
                .ReturnsAsync(user)
                .Verifiable();

            userManagerMock.Setup(um => um.IsEmailConfirmedAsync(It.Is<IdentityUser<Guid>>(u => u == user)))
                .ReturnsAsync(isConfirmed)
                .Verifiable();
        }

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./ForgotPasswordConfirmation", redirect.PageName);

        // Email should not be sent in these cases
        senderMock.Verify(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Never);

        userManagerMock.Verify();
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
