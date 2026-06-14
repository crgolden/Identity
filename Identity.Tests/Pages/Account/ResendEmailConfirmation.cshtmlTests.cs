#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
namespace Identity.Tests.Pages.Account;
using Infrastructure;

using Azure.Messaging.ServiceBus;
using Identity.Pages.Account;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Azure;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class ResendEmailConfirmationModelTests
{
    public static IEnumerable<object[]> EmailTestCases()
    {
        yield return [string.Empty];
        yield return ["   "];
        yield return ["user@example.com"];
        yield return [new string('a', 1024)];
        yield return ["special!@#$%^&*()\t\n\"<>[];:\\'/"];
    }

    [Fact]
    public async Task OnPostAsync_ModelStateInvalid_ReturnsPageWithoutCallingDependencies()
    {
        // Arrange
        var mockUserStore = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var mockUserManager = new Mock<UserManager<IdentityUser<Guid>>>(
            mockUserStore, null, null, null, null, null, null, null, null);

        var (factory, senderMock) = CreateSenderFactoryWithMock();

        var model = new ResendEmailConfirmationModel(mockUserManager.Object, factory)
        {
            Input = new ResendEmailConfirmationModel.InputModel { Email = "doesnotmatter@example.com" }
        };

        model.PageContext = new PageContext { HttpContext = new DefaultHttpContext() };

        // Mark model state invalid
        model.ModelState.AddModelError("someKey", "some error");

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        mockUserManager.Verify(m => m.FindByEmailAsync(It.IsAny<string>()), Times.Never);
        senderMock.Verify(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("user@example.com")]
    public async Task OnPostAsync_UserNotFound_AddsModelErrorAndReturnsPage(string email)
    {
        // Arrange
        var mockUserStore = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var mockUserManager = new Mock<UserManager<IdentityUser<Guid>>>(
            mockUserStore, null, null, null, null, null, null, null, null);

        mockUserManager
            .Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((IdentityUser<Guid>?)null);

        var (factory, senderMock) = CreateSenderFactoryWithMock();

        var model = new ResendEmailConfirmationModel(mockUserManager.Object, factory)
        {
            Input = new ResendEmailConfirmationModel.InputModel { Email = email }
        };

        model.PageContext = new PageContext { HttpContext = new DefaultHttpContext() };
        model.PageContext.HttpContext.Request.Scheme = "https";

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);

        Assert.True(model.ModelState.ErrorCount > 0);
        var entry = model.ModelState[string.Empty];
        Assert.NotNull(entry);
        Assert.NotEmpty(entry.Errors);
        Assert.Equal("Verification email sent. Please check your email.", entry.Errors[0].ErrorMessage);

        mockUserManager.Verify(m => m.FindByEmailAsync(It.Is<string>(s => s == email)), Times.Once);
        senderMock.Verify(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void Constructor_NullArguments_Notes()
    {
        // Arrange
        var mockStore = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var mockUserManager = new Mock<UserManager<IdentityUser<Guid>>>(mockStore, null, null, null, null, null, null, null, null);

        // Act
        var exception = Record.Exception(() => new ResendEmailConfirmationModel(mockUserManager.Object, CreateClientFactory()));

        // Assert
        Assert.Null(exception);
    }

    private static IAzureClientFactory<ServiceBusClient> CreateClientFactory()
    {
        var senderMock = new Mock<ServiceBusSender>(MockBehavior.Strict);
        senderMock.Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var clientMock = new Mock<ServiceBusClient>(MockBehavior.Strict);
        clientMock.Setup(c => c.CreateSender("email")).Returns(senderMock.Object);
        var factoryMock = new Mock<IAzureClientFactory<ServiceBusClient>>(MockBehavior.Strict);
        factoryMock.Setup(f => f.CreateClient("crgolden")).Returns(clientMock.Object);
        return factoryMock.Object;
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
