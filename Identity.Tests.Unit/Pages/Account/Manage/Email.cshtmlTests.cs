namespace Identity.Tests.Unit.Pages.Account.Manage;

using System.Security.Claims;
using System.Text.Encodings.Web;
using Azure.Messaging.ServiceBus;
using Identity.Pages.Account.Manage;
using Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Azure;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class EmailModelTests
{
    public static TheoryData<string?> ValidEmailCases() => new()
    {
        TestValues.NewEmailAddress(),
        TestValues.NewTaggedEmailAddress(),
    };

    [Fact]
    public async Task OnPostSendVerificationEmailAsync_UserNotFound_ReturnsNotFoundWithUserId()
    {
        // Arrange
        var expectedUserId = TestValues.NewUserId().ToString();
        var userManagerMock = MockHelpers.MockUserManager();
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, TestValues.NewUserId().ToString())]));
        userManagerMock
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((IdentityUser<Guid>?)null);
        userManagerMock
            .Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(expectedUserId);

        var model = new EmailModel(userManagerMock.Object, CreateSenderFactory())
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            }
        };

        // Act
        var result = await model.OnPostSendVerificationEmailAsync();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFound.Value);
        Assert.Contains(expectedUserId, notFound.Value.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task OnPostSendVerificationEmailAsync_InvalidModelState_ReturnsPage()
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();
        var user = new IdentityUser<Guid> { Id = TestValues.NewUserId() };
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        userManagerMock
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);

        var (factory, senderMock) = CreateSenderFactoryWithMock();
        var model = new EmailModel(userManagerMock.Object, factory)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            }
        };

        model.ModelState.AddModelError(TestValues.NewModelStateKey(), TestValues.NewValidationMessage());

        // Act
        var result = await model.OnPostSendVerificationEmailAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        senderMock.Verify(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [MemberData(nameof(ValidEmailCases))]
    public async Task OnPostSendVerificationEmailAsync_ValidUser_SendsEmailAndRedirects(string? returnedEmail)
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();
        var user = new IdentityUser<Guid> { Id = TestValues.NewUserId() };
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        userManagerMock
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);
        userManagerMock
            .Setup(um => um.GetUserIdAsync(It.IsAny<IdentityUser<Guid>>()))
            .ReturnsAsync(TestValues.NewUserId().ToString());
        userManagerMock
            .Setup(um => um.GetEmailAsync(It.IsAny<IdentityUser<Guid>>()))
            .ReturnsAsync(returnedEmail);
        userManagerMock
            .Setup(um => um.GenerateEmailConfirmationTokenAsync(It.IsAny<IdentityUser<Guid>>()))
            .ReturnsAsync(TestValues.NewEmailConfirmationToken());

        var fixedCallbackUrl = TestValues.NewCallbackUrl();
        var urlHelperMock = new Mock<IUrlHelper>(MockBehavior.Strict);
        var urlRouteData = new RouteData();
        urlHelperMock.SetupGet(u => u.ActionContext).Returns(
            new ActionContext(new DefaultHttpContext(), urlRouteData, new ActionDescriptor()));

        urlHelperMock.Setup(u => u.RouteUrl(It.IsAny<UrlRouteContext>())).Returns(fixedCallbackUrl);

        ServiceBusMessage? capturedMessage = null;
        var senderMock = new Mock<ServiceBusSender>(MockBehavior.Strict);
        senderMock
            .Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback<ServiceBusMessage, CancellationToken>((msg, _) => capturedMessage = msg);
        var clientMock = new Mock<ServiceBusClient>(MockBehavior.Strict);
        clientMock.Setup(c => c.CreateSender(ServiceBusNames.EmailQueueName)).Returns(senderMock.Object);
        var factoryMock = new Mock<IAzureClientFactory<ServiceBusClient>>(MockBehavior.Strict);
        factoryMock.Setup(f => f.CreateClient(ServiceBusNames.ClientName)).Returns(clientMock.Object);

        var model = new EmailModel(userManagerMock.Object, factoryMock.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal
                }
            },
            Url = urlHelperMock.Object
        };

        model.PageContext.HttpContext.Request.Scheme = Uri.UriSchemeHttps;

        // Act
        var result = await model.OnPostSendVerificationEmailAsync();

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(EmailModel.VerificationEmailSentMessage, model.StatusMessage);

        senderMock.Verify(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Once);

        Assert.NotNull(capturedMessage);
        Assert.Equal(UserMessages.ConfirmEmailSubject, capturedMessage.Subject);
        Assert.Equal(returnedEmail, capturedMessage.To);
        var capturedBody = capturedMessage.Body.ToString();
        var expectedEncodedUrl = HtmlEncoder.Default.Encode(fixedCallbackUrl);
        Assert.Contains(expectedEncodedUrl, capturedBody, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_ValidDependencies_InitializesDefaults()
    {
        // Arrange
        var userManager = CreateUserManager();

        // Act
        var model = new EmailModel(userManager, CreateSenderFactory());

        // Assert
        Assert.NotNull(model);
        Assert.Null(model.Email);
        Assert.False(model.IsEmailConfirmed);
        Assert.Null(model.StatusMessage);
        Assert.NotNull(model.Input);
    }

    [Fact]
    public void Constructor_MultipleInstances_AreIndependent()
    {
        // Arrange
        var userManager1 = CreateUserManager();
        var userManager2 = CreateUserManager();

        // Act
        var model1 = new EmailModel(userManager1, CreateSenderFactory());
        var model2 = new EmailModel(userManager2, CreateSenderFactory());

        // Assert
        Assert.NotNull(model1);
        Assert.NotNull(model2);
        Assert.Null(model1.Email);
        Assert.False(model1.IsEmailConfirmed);
        Assert.Null(model1.StatusMessage);
        Assert.NotNull(model1.Input);

        Assert.Null(model2.Email);
        Assert.False(model2.IsEmailConfirmed);
        Assert.Null(model2.StatusMessage);
        Assert.NotNull(model2.Input);
    }

    [Fact]
    public async Task OnPostChangeEmailAsync_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var missingUserId = TestValues.NewUserId().ToString();
        var userManagerMock = MockHelpers.MockUserManager();
        userManagerMock
            .Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((IdentityUser<Guid>?)null);
        userManagerMock
            .Setup(u => u.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(missingUserId);

        var model = new EmailModel(userManagerMock.Object, CreateSenderFactory())
        {
            PageContext = new PageContext { HttpContext = new DefaultHttpContext() }
        };

        // Act
        var result = await model.OnPostChangeEmailAsync();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var message = Assert.IsType<string>(notFound.Value);
        Assert.Equal(UserMessages.UnableToLoadUser(missingUserId), message);
    }

    private static IAzureClientFactory<ServiceBusClient> CreateSenderFactory()
    {
        var senderMock = new Mock<ServiceBusSender>(MockBehavior.Strict);
        senderMock.Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var clientMock = new Mock<ServiceBusClient>(MockBehavior.Strict);
        clientMock.Setup(c => c.CreateSender(ServiceBusNames.EmailQueueName)).Returns(senderMock.Object);
        var factoryMock = new Mock<IAzureClientFactory<ServiceBusClient>>(MockBehavior.Strict);
        factoryMock.Setup(f => f.CreateClient(ServiceBusNames.ClientName)).Returns(clientMock.Object);
        return factoryMock.Object;
    }

    private static (IAzureClientFactory<ServiceBusClient> factory, Mock<ServiceBusSender> senderMock) CreateSenderFactoryWithMock()
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

    private static UserManager<IdentityUser<Guid>> CreateUserManager() => MockHelpers.MockUserManager().Object;
}