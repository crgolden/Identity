#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
namespace Identity.Tests.Pages.Account.Manage;
using Infrastructure;

using System.Security.Claims;
using System.Text.Encodings.Web;
using Azure.Messaging.ServiceBus;
using Identity.Pages.Account.Manage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class EmailModelTests
{
    public static TheoryData<string?> ValidEmailCases() => new()
    {
        "user@example.com",
        "user+tag@exa-mple.co.uk",
    };

    [Fact]
    public async Task OnPostSendVerificationEmailAsync_UserNotFound_ReturnsNotFoundWithUserId()
    {
        // Arrange
        var storeMock = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(storeMock, null, null, null, null, null, null, null, null);
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "ignored")]));
        userManagerMock
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((IdentityUser<Guid>?)null);
        userManagerMock
            .Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns("expected-user-id");

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
        Assert.Contains("expected-user-id", notFound.Value.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task OnPostSendVerificationEmailAsync_InvalidModelState_ReturnsPage()
    {
        // Arrange
        var storeMock = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(storeMock, null, null, null, null, null, null, null, null);
        var user = new IdentityUser<Guid> { Id = Guid.NewGuid() };
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

        // Make ModelState invalid
        model.ModelState.AddModelError("Input.NewEmail", "Required");

        // Act
        var result = await model.OnPostSendVerificationEmailAsync();

        // Assert
        Assert.IsType<PageResult>(result);

        // Ensure no email was attempted to be sent
        senderMock.Verify(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [MemberData(nameof(ValidEmailCases))]
    public async Task OnPostSendVerificationEmailAsync_ValidUser_SendsEmailAndRedirects(string? returnedEmail)
    {
        // Arrange
        var storeMock = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(storeMock, null, null, null, null, null, null, null, null);
        var user = new IdentityUser<Guid> { Id = Guid.NewGuid() };
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        userManagerMock
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);
        userManagerMock
            .Setup(um => um.GetUserIdAsync(It.IsAny<IdentityUser<Guid>>()))
            .ReturnsAsync("the-user-id");
        userManagerMock
            .Setup(um => um.GetEmailAsync(It.IsAny<IdentityUser<Guid>>()))
            .ReturnsAsync(returnedEmail);
        userManagerMock
            .Setup(um => um.GenerateEmailConfirmationTokenAsync(It.IsAny<IdentityUser<Guid>>()))
            .ReturnsAsync("raw-token");

        const string fixedCallbackUrl = "https://example.test/Account/ConfirmEmail?userId=the-user-id&code=abc";
        var urlHelperMock = new Mock<IUrlHelper>();

        // ActionContext must be non-null because UrlHelperExtensions.Page always accesses it
        var urlRouteData = new RouteData();
        urlRouteData.Values["page"] = "/Account/Manage/Email";
        urlHelperMock.SetupGet(u => u.ActionContext).Returns(
            new ActionContext(new DefaultHttpContext(), urlRouteData, new ActionDescriptor()));

        // SetReturnsDefault covers all string?-returning methods including RouteUrl called by Url.Page
        urlHelperMock.SetReturnsDefault<string?>(fixedCallbackUrl);

        ServiceBusMessage? capturedMessage = null;
        var senderMock = new Mock<ServiceBusSender>();
        senderMock
            .Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback<ServiceBusMessage, CancellationToken>((msg, _) => capturedMessage = msg);
        var factoryMock = new Mock<IAzureClientFactory<ServiceBusSender>>();
        factoryMock.Setup(f => f.CreateClient("email")).Returns(senderMock.Object);

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

        // Ensure request scheme is set for Url.Page usage
        model.PageContext.HttpContext.Request.Scheme = "https";

        // Act
        var result = await model.OnPostSendVerificationEmailAsync();

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("Verification email sent. Please check your email.", model.StatusMessage);

        senderMock.Verify(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Once);

        Assert.NotNull(capturedMessage);
        Assert.Equal("Confirm your email", capturedMessage.Subject);
        Assert.Equal(returnedEmail, capturedMessage.To);

        // The body should contain the encoded callback url
        var capturedBody = capturedMessage.Body.ToString();
        var expectedEncodedUrl = HtmlEncoder.Default.Encode(fixedCallbackUrl);
        Assert.Contains(expectedEncodedUrl, capturedBody);
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
        // Arrange - first instance
        var userManager1 = CreateUserManager();

        // Arrange - second instance
        var userManager2 = CreateUserManager();

        // Act
        var model1 = new EmailModel(userManager1, CreateSenderFactory());
        var model2 = new EmailModel(userManager2, CreateSenderFactory());

        // Assert
        Assert.NotNull(model1);
        Assert.NotNull(model2);

        // Default public state assertions for both instances
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
        var storeMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var optionsMock = new Mock<IOptions<IdentityOptions>>();
        optionsMock.Setup(o => o.Value).Returns(new IdentityOptions());
        var hasherMock = new Mock<IPasswordHasher<IdentityUser<Guid>>>();
        var lookupNormalizerMock = new Mock<ILookupNormalizer>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(
                storeMock.Object,
                optionsMock.Object,
                hasherMock.Object,
                Array.Empty<IUserValidator<IdentityUser<Guid>>>(),
                Array.Empty<IPasswordValidator<IdentityUser<Guid>>>(),
                lookupNormalizerMock.Object,
                new IdentityErrorDescriber(),
                new Mock<IServiceProvider>().Object,
                new Mock<ILogger<UserManager<IdentityUser<Guid>>>>().Object)
        { CallBase = false };

        // Make GetUserAsync return null to simulate missing user.
        userManagerMock
            .Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((IdentityUser<Guid>?)null);

        // Ensure GetUserId returns a specific id used in the NotFound message.
        userManagerMock
            .Setup(u => u.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns("missing-user-id");

        var model = new EmailModel(userManagerMock.Object, CreateSenderFactory())
        {
            // Ensure PageContext is available (PageModel may access Request, Url etc. but not needed here)
            PageContext = new PageContext { HttpContext = new DefaultHttpContext() }
        };

        // Act
        var result = await model.OnPostChangeEmailAsync();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("Unable to load user with ID 'missing-user-id'", notFound.Value?.ToString() ?? string.Empty);
    }

    // Helper methods to create minimal mocks/instances needed for constructor invocation.
    private static IAzureClientFactory<ServiceBusSender> CreateSenderFactory()
    {
        var senderMock = new Mock<ServiceBusSender>();
        senderMock.Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var factoryMock = new Mock<IAzureClientFactory<ServiceBusSender>>();
        factoryMock.Setup(f => f.CreateClient("email")).Returns(senderMock.Object);
        return factoryMock.Object;
    }

    private static (IAzureClientFactory<ServiceBusSender> factory, Mock<ServiceBusSender> senderMock) CreateSenderFactoryWithMock()
    {
        var senderMock = new Mock<ServiceBusSender>();
        senderMock.Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var factoryMock = new Mock<IAzureClientFactory<ServiceBusSender>>();
        factoryMock.Setup(f => f.CreateClient("email")).Returns(senderMock.Object);
        return (factoryMock.Object, senderMock);
    }

    private static UserManager<IdentityUser<Guid>> CreateUserManager()
    {
        // Minimal IUserStore needed for UserManager constructor
        var storeMock = new Mock<IUserStore<IdentityUser<Guid>>>();

        // Provide concrete/simple implementations for other parameters where feasible
        var options = Options.Create(new IdentityOptions());
        var passwordHasher = new PasswordHasher<IdentityUser<Guid>>();
        var userValidators = new List<IUserValidator<IdentityUser<Guid>>>();
        var passwordValidators = new List<IPasswordValidator<IdentityUser<Guid>>>();
        var lookupNormalizer = Mock.Of<ILookupNormalizer>();
        var errors = new IdentityErrorDescriber();
        IServiceProvider? services = null;
        var logger = Mock.Of<ILogger<UserManager<IdentityUser<Guid>>>>();

        // Create a mock of UserManager using the required constructor arguments.
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(
            storeMock.Object,
            options,
            passwordHasher,
            userValidators,
            passwordValidators,
            lookupNormalizer,
            errors,
            services,
            logger);

        return userManagerMock.Object;
    }
}
