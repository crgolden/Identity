namespace Identity.Tests.Unit.Pages.Account.Manage;

using System.Security.Claims;
using System.Text.Encodings.Web;
using Azure.Messaging.ServiceBus;
using Identity.Pages.Account.Manage;
using Identity.Tests.Unit.Infrastructure;
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
public class EmailTests
{
    public static TheoryData<string?> ValidEmailCases() => new()
    {
        Generated.NewEmailAddress(),
        Generated.NewTaggedEmailAddress(),
    };

    [Fact]
    public async Task OnPostSendVerificationEmailAsync_UserNotFound_ReturnsNotFoundWithUserId()
    {
        // Arrange
        var expectedUserId = Generated.NewUserId().ToString();
        var userManagerMock = MockHelpers.MockUserManager();
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, Generated.NewUserId().ToString())]));
        userManagerMock
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((IdentityUser<Guid>?)null);
        userManagerMock
            .Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(expectedUserId);

        var model = new Email(userManagerMock.Object, CreateSenderFactory(), TestValues.NewAccountEmailSettings())
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
        var user = new IdentityUser<Guid> { Id = Generated.NewUserId() };
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        userManagerMock
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);

        var (factory, senderMock) = CreateSenderFactoryWithMock();
        var model = new Email(userManagerMock.Object, factory, TestValues.NewAccountEmailSettings())
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            }
        };

        model.ModelState.AddModelError(Generated.NewModelStateKey(), Generated.NewValidationMessage());

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
        var user = new IdentityUser<Guid> { Id = Generated.NewUserId() };
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        userManagerMock
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);
        userManagerMock
            .Setup(um => um.GetUserIdAsync(It.IsAny<IdentityUser<Guid>>()))
            .ReturnsAsync(Generated.NewUserId().ToString());
        userManagerMock
            .Setup(um => um.GetEmailAsync(It.IsAny<IdentityUser<Guid>>()))
            .ReturnsAsync(returnedEmail);
        userManagerMock
            .Setup(um => um.GenerateEmailConfirmationTokenAsync(It.IsAny<IdentityUser<Guid>>()))
            .ReturnsAsync(Generated.NewEmailConfirmationToken());

        var fixedCallbackUrl = Generated.NewCallbackAddress();
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

        var model = new Email(userManagerMock.Object, factoryMock.Object, TestValues.NewAccountEmailSettings())
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
        Assert.Equal(Email.VerificationEmailSentMessage, model.StatusMessage);

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
        var model = new Email(userManager, CreateSenderFactory(), TestValues.NewAccountEmailSettings());

        // Assert
        Assert.NotNull(model);
        Assert.Null(model.CurrentEmail);
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
        var model1 = new Email(userManager1, CreateSenderFactory(), TestValues.NewAccountEmailSettings());
        var model2 = new Email(userManager2, CreateSenderFactory(), TestValues.NewAccountEmailSettings());

        // Assert
        Assert.NotNull(model1);
        Assert.NotNull(model2);
        Assert.Null(model1.CurrentEmail);
        Assert.False(model1.IsEmailConfirmed);
        Assert.Null(model1.StatusMessage);
        Assert.NotNull(model1.Input);

        Assert.Null(model2.CurrentEmail);
        Assert.False(model2.IsEmailConfirmed);
        Assert.Null(model2.StatusMessage);
        Assert.NotNull(model2.Input);
    }

    [Fact]
    public async Task OnPostChangeEmailAsync_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var missingUserId = Generated.NewUserId().ToString();
        var userManagerMock = MockHelpers.MockUserManager();
        userManagerMock
            .Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((IdentityUser<Guid>?)null);
        userManagerMock
            .Setup(u => u.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(missingUserId);

        var model = new Email(userManagerMock.Object, CreateSenderFactory(), TestValues.NewAccountEmailSettings())
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

    [Fact]
    public async Task OnGetAsync_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var missingUserId = Generated.NewUserId().ToString();
        var userManagerMock = MockHelpers.MockUserManager();
        userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((IdentityUser<Guid>?)null);
        userManagerMock.Setup(u => u.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(missingUserId);
        var model = Create(userManagerMock, CreateSenderFactory(), UrlHelperReturning(Generated.NewCallbackAddress()));

        // Act
        var result = await model.OnGetAsync();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(UserMessages.UnableToLoadUser(missingUserId), notFound.Value);
    }

    [Fact]
    public async Task OnGetAsync_UserFound_PopulatesTheEmailInputAndConfirmationState()
    {
        // Arrange
        var currentEmail = Generated.NewEmailAddress();
        var userManagerMock = UserManagerFor(currentEmail);
        userManagerMock.Setup(u => u.IsEmailConfirmedAsync(It.IsAny<IdentityUser<Guid>>())).ReturnsAsync(true);
        var model = Create(userManagerMock, CreateSenderFactory(), UrlHelperReturning(Generated.NewCallbackAddress()));

        // Act
        var result = await model.OnGetAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal(currentEmail, model.CurrentEmail);
        Assert.Equal(currentEmail, model.Input.NewEmail);
        Assert.True(model.IsEmailConfirmed);
    }

    [Fact]
    public async Task OnPostChangeEmailAsync_InvalidModelState_RepopulatesTheCurrentEmailAndSendsNothing()
    {
        // Arrange
        var currentEmail = Generated.NewEmailAddress();
        var userManagerMock = UserManagerFor(currentEmail);
        var (factory, senderMock) = CreateSenderFactoryWithMock();
        var model = Create(userManagerMock, factory, UrlHelperReturning(Generated.NewCallbackAddress()));
        model.Input = new Email.InputModel { NewEmail = Generated.NewEmailAddress() };
        model.ModelState.AddModelError(Generated.NewModelStateKey(), Generated.NewValidationMessage());

        // Act
        var result = await model.OnPostChangeEmailAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal(currentEmail, model.Input.NewEmail);
        senderMock.Verify(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostChangeEmailAsync_NewEmailDiffers_SendsTheChangeLinkToTheNewAddress()
    {
        // Arrange
        var newEmail = Generated.NewEmailAddress();
        var callbackUrl = Generated.NewCallbackAddress();
        var userManagerMock = UserManagerFor(Generated.NewEmailAddress());
        userManagerMock.Setup(u => u.GenerateChangeEmailTokenAsync(It.IsAny<IdentityUser<Guid>>(), newEmail)).ReturnsAsync(Generated.NewEmailConfirmationToken());
        var (factory, senderMock) = CreateSenderFactoryWithMock();
        var model = Create(userManagerMock, factory, UrlHelperReturning(callbackUrl));
        model.Input = new Email.InputModel { NewEmail = newEmail };

        // Act
        var result = await model.OnPostChangeEmailAsync();

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(Email.EmailChangeLinkSentMessage, model.StatusMessage);
        senderMock.Verify(
            s => s.SendMessageAsync(
                It.Is<ServiceBusMessage>(m =>
                    string.Equals(m.To, newEmail, StringComparison.Ordinal)
                    && m.Body.ToString().Contains(HtmlEncoder.Default.Encode(callbackUrl), StringComparison.Ordinal)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task OnPostChangeEmailAsync_CallbackUrlNotBuilt_ThrowsAndReportsNothingSent()
    {
        // Arrange
        var newEmail = Generated.NewEmailAddress();
        var userManagerMock = UserManagerFor(Generated.NewEmailAddress());
        userManagerMock.Setup(u => u.GenerateChangeEmailTokenAsync(It.IsAny<IdentityUser<Guid>>(), newEmail)).ReturnsAsync(Generated.NewEmailConfirmationToken());
        var (factory, senderMock) = CreateSenderFactoryWithMock();
        var model = Create(userManagerMock, factory, UrlHelperReturning(null));
        model.Input = new Email.InputModel { NewEmail = newEmail };

        // Act
        var exception = await Record.ExceptionAsync(() => model.OnPostChangeEmailAsync());

        // Assert
        Assert.IsType<InvalidOperationException>(exception);
        Assert.Null(model.StatusMessage);
        senderMock.Verify(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostChangeEmailAsync_NewEmailBlank_ReportsTheEmailUnchanged()
    {
        // Arrange
        var userManagerMock = UserManagerFor(Generated.NewEmailAddress());
        var (factory, senderMock) = CreateSenderFactoryWithMock();
        var model = Create(userManagerMock, factory, UrlHelperReturning(Generated.NewCallbackAddress()));
        model.Input = new Email.InputModel { NewEmail = Generated.NewBlank() };

        // Act
        var result = await model.OnPostChangeEmailAsync();

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(Email.EmailUnchangedMessage, model.StatusMessage);
        senderMock.Verify(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostChangeEmailAsync_NewEmailEqualsCurrent_ReportsTheEmailUnchanged()
    {
        // Arrange
        var currentEmail = Generated.NewEmailAddress();
        var userManagerMock = UserManagerFor(currentEmail);
        var (factory, senderMock) = CreateSenderFactoryWithMock();
        var model = Create(userManagerMock, factory, UrlHelperReturning(Generated.NewCallbackAddress()));
        model.Input = new Email.InputModel { NewEmail = currentEmail };

        // Act
        var result = await model.OnPostChangeEmailAsync();

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(Email.EmailUnchangedMessage, model.StatusMessage);
        senderMock.Verify(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostSendVerificationEmailAsync_EmailBlank_ReportsNoEmailAndSendsNothing()
    {
        // Arrange
        var userManagerMock = UserManagerFor(Generated.NewBlank());
        var (factory, senderMock) = CreateSenderFactoryWithMock();
        var model = Create(userManagerMock, factory, UrlHelperReturning(Generated.NewCallbackAddress()));

        // Act
        var result = await model.OnPostSendVerificationEmailAsync();

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(Email.NoEmailToVerifyMessage, model.StatusMessage);
        userManagerMock.Verify(u => u.GenerateEmailConfirmationTokenAsync(It.IsAny<IdentityUser<Guid>>()), Times.Never);
        senderMock.Verify(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostSendVerificationEmailAsync_CallbackUrlNotBuilt_ThrowsAndReportsNothingSent()
    {
        // Arrange
        var userManagerMock = UserManagerFor(Generated.NewEmailAddress());
        userManagerMock.Setup(u => u.GenerateEmailConfirmationTokenAsync(It.IsAny<IdentityUser<Guid>>())).ReturnsAsync(Generated.NewEmailConfirmationToken());
        var (factory, senderMock) = CreateSenderFactoryWithMock();
        var model = Create(userManagerMock, factory, UrlHelperReturning(null));

        // Act
        var exception = await Record.ExceptionAsync(() => model.OnPostSendVerificationEmailAsync());

        // Assert
        Assert.IsType<InvalidOperationException>(exception);
        Assert.Null(model.StatusMessage);
        senderMock.Verify(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Mock<UserManager<IdentityUser<Guid>>> UserManagerFor(string currentEmail)
    {
        var userManagerMock = MockHelpers.MockUserManager();
        userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(new IdentityUser<Guid> { Id = Generated.NewUserId() });
        userManagerMock.Setup(u => u.GetUserIdAsync(It.IsAny<IdentityUser<Guid>>())).ReturnsAsync(Generated.NewUserId().ToString());
        userManagerMock.Setup(u => u.GetEmailAsync(It.IsAny<IdentityUser<Guid>>())).ReturnsAsync(currentEmail);
        return userManagerMock;
    }

    private static IUrlHelper UrlHelperReturning(string? callbackUrl)
    {
        var urlHelperMock = new Mock<IUrlHelper>(MockBehavior.Strict);
        urlHelperMock.SetupGet(u => u.ActionContext).Returns(new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()));
        urlHelperMock.Setup(u => u.RouteUrl(It.IsAny<UrlRouteContext>())).Returns(callbackUrl);
        return urlHelperMock.Object;
    }

    private static Email Create(
        Mock<UserManager<IdentityUser<Guid>>> userManagerMock,
        IAzureClientFactory<ServiceBusClient> factory,
        IUrlHelper urlHelper)
    {
        var model = new Email(userManagerMock.Object, factory, TestValues.NewAccountEmailSettings())
        {
            PageContext = new PageContext { HttpContext = new DefaultHttpContext() },
            Url = urlHelper,
        };
        model.PageContext.HttpContext.Request.Scheme = Uri.UriSchemeHttps;
        return model;
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

    private static UserManager<IdentityUser<Guid>> CreateUserManager() => MockHelpers.MockUserManager().Object;
}
