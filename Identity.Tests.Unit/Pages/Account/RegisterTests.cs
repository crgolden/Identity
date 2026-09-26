namespace Identity.Tests.Unit.Pages.Account;

using Azure.Messaging.ServiceBus;
using Identity.CAPTCHA;
using Identity.Pages.Account;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Authentication;
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
public sealed class RegisterTests : IDisposable
{
    private readonly TelemetryHarness _harness = new();

    public static TheoryData<int> ExternalSchemeCounts() => new() { 0, 1, Random.Shared.Next(2, 6) };

    public static TheoryData<string?> ReturnUrlValues() => new()
    {
        (string?)null,
        string.Empty,
        Generated.NewWhitespaceValue(),
        Generated.LowercaseToken(1),
        Generated.NewPunctuatedPageName(),
        Generated.NewOverlongPageName(),
    };

    [Theory]
    [MemberData(nameof(ReturnUrlValues))]
    public async Task OnGetAsync_VariousReturnUrlValues_AssignsReturnUrlAndDoesNotThrow(string? returnUrl)
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();
        userManagerMock.SetupGet(u => u.SupportsUserEmail).Returns(true);

        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);
        signInManagerMock
            .Setup(s => s.GetExternalAuthenticationSchemesAsync())
            .ReturnsAsync([]);

        var model = new Register(
            userManagerMock.Object,
            signInManagerMock.Object,
            CreateClientFactory(),
            CreateRecaptchaServiceMock().Object,
            TestValues.NewAccountEmailSettings(),
            _harness.Telemetry);

        // Act
        var ex = await Record.ExceptionAsync(() => model.OnGetAsync(returnUrl));

        // Assert
        Assert.Null(ex);
        Assert.Equal(returnUrl, model.ReturnUrl);
        Assert.NotNull(model.ExternalLogins);
        Assert.Empty(model.ExternalLogins);
    }

    [Theory]
    [MemberData(nameof(ExternalSchemeCounts))]
    public async Task OnGetAsync_ExternalSchemesReturned_PopulatesExternalLogins(int schemeCount)
    {
        // Arrange
        var schemes = NewSchemes(schemeCount);
        var userManagerMock = MockHelpers.MockUserManager();
        userManagerMock.SetupGet(u => u.SupportsUserEmail).Returns(true);

        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);

        signInManagerMock
            .Setup(s => s.GetExternalAuthenticationSchemesAsync())
            .ReturnsAsync(schemes);

        var model = new Register(
            userManagerMock.Object,
            signInManagerMock.Object,
            CreateClientFactory(),
            CreateRecaptchaServiceMock().Object,
            TestValues.NewAccountEmailSettings(),
            _harness.Telemetry);

        var returnUrl = Generated.NewLocalPath();

        // Act
        await model.OnGetAsync(returnUrl);

        // Assert
        Assert.Equal(returnUrl, model.ReturnUrl);
        Assert.NotNull(model.ExternalLogins);
        Assert.Equal(schemes.Length, model.ExternalLogins.Count);
        var expectedNames = schemes.Select(s => s.Name).ToList();
        var actualNames = model.ExternalLogins.Select(s => s.Name).ToList();
        Assert.Equal(expectedNames, actualNames);
    }

    [Fact(DisplayName = "OnPostAsync_ModelStateInvalid_ReturnsPage")]
    public async Task OnPostAsync_ModelStateInvalid_ReturnsPage()
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();
        userManagerMock.SetupGet(u => u.SupportsUserEmail).Returns(true);
        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);

        signInManagerMock
            .Setup(s => s.GetExternalAuthenticationSchemesAsync())
            .ReturnsAsync([]);

        var model = new Register(
            userManagerMock.Object,
            signInManagerMock.Object,
            CreateClientFactory(),
            CreateRecaptchaServiceMock().Object,
            TestValues.NewAccountEmailSettings(),
            _harness.Telemetry);

        var ctx = new DefaultHttpContext();
        ctx.Request.Scheme = Uri.UriSchemeHttps;
        model.PageContext = new PageContext { HttpContext = ctx };

        var urlHelperMock = new Mock<IUrlHelper>(MockBehavior.Strict);
        urlHelperMock.Setup(u => u.Content(PageRoutes.ContentRoot)).Returns(Generated.NewLocalPath());
        model.Url = urlHelperMock.Object;
        model.ModelState.AddModelError(Generated.NewClaimType(), Generated.NewFailureReason());

        // Act
        var result = await model.OnPostAsync(Generated.NewLocalPath());

        // Assert
        Assert.IsType<PageResult>(result);
        signInManagerMock.Verify(s => s.GetExternalAuthenticationSchemesAsync(), Times.Once);
        userManagerMock.Verify(u => u.CreateAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_CreateSucceedsAndConfirmationRequired_RedirectsToRegisterConfirmationWithoutSigningIn()
    {
        // Arrange
        var returnUrl = Generated.NewLocalPath();
        var (model, userManagerMock, signInManagerMock, senderMock) = BuildRegisterFixture(requireConfirmedAccount: true);

        // Act
        var actionResult = await model.OnPostAsync(returnUrl);

        // Assert
        userManagerMock.Verify(u => u.CreateAsync(It.IsAny<IdentityUser<Guid>>(), It.Is<string>(p => p == model.Input.Password)), Times.Once);
        senderMock.Verify(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        var redirect = Assert.IsType<RedirectToPageResult>(actionResult);
        Assert.Equal(Register.RegisterConfirmationPageName, redirect.PageName);
        Assert.NotNull(redirect.RouteValues);
        Assert.Equal(model.Input.Email, redirect.RouteValues[nameof(Register.InputModel.Email)]);
        Assert.Equal(returnUrl, redirect.RouteValues[nameof(Register.ReturnUrl)]);
        signInManagerMock.Verify(s => s.SignInAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<bool>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_CreateSucceedsAndConfirmationNotRequired_SignsInAndRedirectsLocally()
    {
        // Arrange
        var returnUrl = Generated.NewLocalPath();
        var (model, userManagerMock, signInManagerMock, senderMock) = BuildRegisterFixture(requireConfirmedAccount: false);

        // Act
        var actionResult = await model.OnPostAsync(returnUrl);

        // Assert
        userManagerMock.Verify(u => u.CreateAsync(It.IsAny<IdentityUser<Guid>>(), It.Is<string>(p => p == model.Input.Password)), Times.Once);
        senderMock.Verify(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        var local = Assert.IsType<LocalRedirectResult>(actionResult);
        Assert.Equal(returnUrl, local.Url);
        signInManagerMock.Verify(s => s.SignInAsync(It.IsAny<IdentityUser<Guid>>(), false, null), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_CreateSucceedsWithANonLocalReturnUrl_RedirectsToTheContentRoot()
    {
        // Arrange
        var (model, _, _, _) = BuildRegisterFixture(requireConfirmedAccount: false);

        // Act
        var actionResult = await model.OnPostAsync(Generated.NewOrigin());

        // Assert
        var local = Assert.IsType<LocalRedirectResult>(actionResult);
        Assert.Equal(PageRoutes.ContentRoot, local.Url);
    }

    [Fact]
    public async Task OnPostAsync_RecaptchaScoreBelowThreshold_ReturnsPageWithModelError()
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();
        userManagerMock.SetupGet(u => u.SupportsUserEmail).Returns(true);
        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);
        signInManagerMock.Setup(s => s.GetExternalAuthenticationSchemesAsync()).ReturnsAsync([]);

        var recaptchaServiceMock = CreateRecaptchaServiceMock(passed: false);

        var model = new Register(
            userManagerMock.Object,
            signInManagerMock.Object,
            CreateClientFactory(),
            recaptchaServiceMock.Object,
            TestValues.NewAccountEmailSettings(),
            _harness.Telemetry);

        var ctx = new DefaultHttpContext();
        ctx.Request.Scheme = Uri.UriSchemeHttps;
        model.PageContext = new PageContext { HttpContext = ctx };

        var urlHelperMock = new Mock<IUrlHelper>(MockBehavior.Strict);
        urlHelperMock.Setup(u => u.Content(PageRoutes.ContentRoot)).Returns(Generated.NewLocalPath());
        model.Url = urlHelperMock.Object;
        model.Input = new Register.InputModel { Email = Generated.NewEmailAddress(), Password = Generated.NewPassword() };

        // Act
        var result = await model.OnPostAsync(Generated.NewLocalPath());

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        Assert.True(model.ModelState.ContainsKey(string.Empty));
        userManagerMock.Verify(u => u.CreateAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<string>()), Times.Never);
    }

    public void Dispose() => _harness.Dispose();

    private static IAzureClientFactory<ServiceBusClient> CreateClientFactory()
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

    private static Mock<ICAPTCHAService> CreateRecaptchaServiceMock(bool passed = true)
    {
        var mock = new Mock<ICAPTCHAService>(MockBehavior.Strict);
        var score = passed
            ? Generated.NewScoreAtOrAbove(ReCAPTCHAOptions.DefaultScoreThreshold)
            : Generated.NewScoreBelow(ReCAPTCHAOptions.DefaultScoreThreshold);
        mock.Setup(s => s.SiteKey).Returns((string?)null);
        mock.Setup(s => s.ScriptEndpoint).Returns(Generated.NewProviderBaseAddress());
        mock.Setup(s => s.VerifyAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CAPTCHAVerdict(passed, score));
        return mock;
    }

    private static AuthenticationScheme[] NewSchemes(int count) =>
        [.. Enumerable.Range(0, count).Select(_ => new AuthenticationScheme(Generated.NewSchemeName(), Generated.NewDisplayName(), typeof(DummyAuthHandler)))];

    private (
        Register Model,
        Mock<UserManager<IdentityUser<Guid>>> UserManagerMock,
        Mock<SignInManager<IdentityUser<Guid>>> SignInManagerMock,
        Mock<ServiceBusSender> SenderMock) BuildRegisterFixture(bool requireConfirmedAccount)
    {
        var identityOptions = new IdentityOptions();
        identityOptions.SignIn.RequireConfirmedAccount = requireConfirmedAccount;
        var userManagerMock = MockHelpers.MockUserManager(identityOptions);
        userManagerMock.SetupGet(u => u.SupportsUserEmail).Returns(true);
        userManagerMock
            .Setup(u => u.CreateAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var createdUserId = Generated.NewUserId().ToString();
        userManagerMock
            .Setup(u => u.GetUserIdAsync(It.IsAny<IdentityUser<Guid>>()))
            .ReturnsAsync(createdUserId);

        var emailConfirmationToken = Generated.NewEmailConfirmationToken();
        userManagerMock
            .Setup(u => u.GenerateEmailConfirmationTokenAsync(It.IsAny<IdentityUser<Guid>>()))
            .ReturnsAsync(emailConfirmationToken);

        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);

        signInManagerMock
            .Setup(s => s.GetExternalAuthenticationSchemesAsync())
            .ReturnsAsync([]);

        signInManagerMock
            .Setup(s => s.SignInAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<bool>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var (senderFactory, senderMock) = CreateSenderFactoryWithMock();

        var model = new Register(
            userManagerMock.Object,
            signInManagerMock.Object,
            senderFactory,
            CreateRecaptchaServiceMock().Object,
            TestValues.NewAccountEmailSettings(),
            _harness.Telemetry);

        var ctx = new DefaultHttpContext();
        ctx.Request.Scheme = Uri.UriSchemeHttps;
        model.PageContext = new PageContext { HttpContext = ctx };

        var urlHelperMock = new Mock<IUrlHelper>(MockBehavior.Strict);
        var urlRouteData = new RouteData();
        urlHelperMock.SetupGet(u => u.ActionContext).Returns(
            new ActionContext(new DefaultHttpContext(), urlRouteData, new ActionDescriptor()));

        urlHelperMock.Setup(u => u.RouteUrl(It.IsAny<UrlRouteContext>())).Returns(Generated.NewCallbackAddress());
        urlHelperMock.Setup(u => u.Content(PageRoutes.ContentRoot)).Returns(Generated.NewLocalPath());
        urlHelperMock.Setup(u => u.IsLocalUrl(It.IsAny<string?>())).Returns<string?>(u => u is not null && u.StartsWith('/'));
        model.Url = urlHelperMock.Object;
        model.Input = new Register.InputModel
        {
            Email = Generated.NewEmailAddress(),
            Password = Generated.NewPassword()
        };

        return (model, userManagerMock, signInManagerMock, senderMock);
    }

    private class DummyAuthHandler : IAuthenticationHandler
    {
        public Task<AuthenticateResult> AuthenticateAsync()
            => Task.FromResult(AuthenticateResult.NoResult());

        public Task ChallengeAsync(AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task ForbidAsync(AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context)
            => Task.CompletedTask;
    }
}
