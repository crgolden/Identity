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

    private readonly AccountEmailSettings _emailSettings = TestValues.NewAccountEmailSettings();

    private readonly List<string> _routedPages = [];

    public static TheoryData<int> NoneOrOneExternalSchemeCounts() => new() { 0, 1 };

    public static TheoryData<string?> NullReturnUrls() => new()
    {
        (string?)null,
    };

    [Theory]
    [MemberData(nameof(NullReturnUrls))]
    public async Task OnGetAsync_NullReturnUrl_AssignsReturnUrlAndDoesNotThrow(string? returnUrl)
    {
        // Arrange
        var model = BuildModelReturningSchemes([]);

        // Act
        var ex = await Record.ExceptionAsync(() => model.OnGetAsync(returnUrl));

        // Assert
        AssertReturnUrlAssignedWithoutExternalLogins(ex, model, returnUrl);
    }

    [Fact]
    public async Task OnGetAsync_WhitespaceReturnUrl_AssignsReturnUrlAndDoesNotThrow()
    {
        // Arrange
        var whitespaceReturnUrl = Generated.NewWhitespaceValue();
        var model = BuildModelReturningSchemes([]);

        // Act
        var ex = await Record.ExceptionAsync(() => model.OnGetAsync(whitespaceReturnUrl));

        // Assert
        AssertReturnUrlAssignedWithoutExternalLogins(ex, model, whitespaceReturnUrl);
    }

    [Fact]
    public async Task OnGetAsync_SingleCharacterReturnUrl_AssignsReturnUrlAndDoesNotThrow()
    {
        // Arrange
        var singleCharacterReturnUrl = Generated.LowercaseToken(1);
        var model = BuildModelReturningSchemes([]);

        // Act
        var ex = await Record.ExceptionAsync(() => model.OnGetAsync(singleCharacterReturnUrl));

        // Assert
        AssertReturnUrlAssignedWithoutExternalLogins(ex, model, singleCharacterReturnUrl);
    }

    [Fact]
    public async Task OnGetAsync_PunctuatedReturnUrl_AssignsReturnUrlAndDoesNotThrow()
    {
        // Arrange
        var punctuatedReturnUrl = Generated.NewPunctuatedPageName();
        var model = BuildModelReturningSchemes([]);

        // Act
        var ex = await Record.ExceptionAsync(() => model.OnGetAsync(punctuatedReturnUrl));

        // Assert
        AssertReturnUrlAssignedWithoutExternalLogins(ex, model, punctuatedReturnUrl);
    }

    [Fact]
    public async Task OnGetAsync_OverlongReturnUrl_AssignsReturnUrlAndDoesNotThrow()
    {
        // Arrange
        var overlongReturnUrl = Generated.NewOverlongPageName();
        var model = BuildModelReturningSchemes([]);

        // Act
        var ex = await Record.ExceptionAsync(() => model.OnGetAsync(overlongReturnUrl));

        // Assert
        AssertReturnUrlAssignedWithoutExternalLogins(ex, model, overlongReturnUrl);
    }

    [Theory]
    [MemberData(nameof(NoneOrOneExternalSchemeCounts))]
    public async Task OnGetAsync_ExternalSchemesReturned_PopulatesExternalLogins(int schemeCount)
    {
        // Arrange
        var schemes = NewSchemes(schemeCount);
        var model = BuildModelReturningSchemes(schemes);
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

    [Fact]
    public async Task OnGetAsync_SeveralExternalSchemesReturned_PopulatesExternalLogins()
    {
        // Arrange
        var severalSchemeCount = Random.Shared.Next(2, 6);
        var schemes = NewSchemes(severalSchemeCount);
        var model = BuildModelReturningSchemes(schemes);
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
    public async Task OnPostAsync_CreateSucceeds_StoresTheTypedEmailAsUserNameAndEmail()
    {
        // Arrange
        var (model, userManagerMock, _, _) = BuildRegisterFixture(requireConfirmedAccount: false);

        // Act
        await model.OnPostAsync(Generated.NewLocalPath());

        // Assert
        userManagerMock.Verify(u => u.SetUserNameAsync(It.IsAny<IdentityUser<Guid>>(), model.Input.Email), Times.Once);
        userManagerMock.Verify(u => u.SetEmailAsync(It.IsAny<IdentityUser<Guid>>(), model.Input.Email), Times.Once);
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
        Assert.Contains(model.ModelState.Values.SelectMany(v => v.Errors), e => string.Equals(e.ErrorMessage, Login.CaptchaFailedMessage, StringComparison.Ordinal));
        userManagerMock.Verify(u => u.CreateAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_CreateSucceeds_SendsTheConfirmationToTheNewAddressFromTheConfiguredSender()
    {
        // Arrange
        var (model, _, _, senderMock) = BuildRegisterFixture(requireConfirmedAccount: true);
        var sent = new List<ServiceBusMessage>();
        senderMock.Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Callback<ServiceBusMessage, CancellationToken>((message, _) => sent.Add(message))
            .Returns(Task.CompletedTask);

        // Act
        await model.OnPostAsync(Generated.NewLocalPath());

        // Assert
        var message = Assert.Single(sent);
        Assert.Equal(model.Input.Email, message.To);
        Assert.Equal(_emailSettings.Sender, message.ReplyTo);
        Assert.Equal(UserMessages.ConfirmEmailSubject, message.Subject);
    }

    [Fact]
    public async Task OnPostAsync_CreateSucceeds_BuildsTheCallbackForTheConfirmEmailPage()
    {
        // Arrange
        var (model, _, _, _) = BuildRegisterFixture(requireConfirmedAccount: true);

        // Act
        await model.OnPostAsync(Generated.NewLocalPath());

        // Assert
        Assert.Equal(PageRoutes.ConfirmEmail, Assert.Single(_routedPages));
    }

    [Fact]
    public async Task OnPostAsync_CreateSucceeds_TagsTheRegisterSpanSucceededAndSent()
    {
        // Arrange
        var (model, _, _, _) = BuildRegisterFixture(requireConfirmedAccount: true);

        // Act
        await model.OnPostAsync(Generated.NewLocalPath());

        // Assert
        var span = Assert.Single(_harness.StoppedActivities, a => string.Equals(a.OperationName, Register.RegisterActivityName, StringComparison.Ordinal));
        Assert.True(Assert.IsType<bool>(span.GetTagItem(Telemetry.Metrics.SucceededTagName)));
        Assert.True(Assert.IsType<bool>(span.GetTagItem(Telemetry.Metrics.EmailConfirmationSentTagName)));
    }

    [Fact]
    public async Task OnPostAsync_NoCallbackUrl_SendsNoEmailAndTagsItUnsent()
    {
        // Arrange
        var (model, _, _, senderMock) = BuildRegisterFixture(requireConfirmedAccount: true, callbackResolves: false);

        // Act
        await model.OnPostAsync(Generated.NewLocalPath());

        // Assert
        senderMock.Verify(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        var span = Assert.Single(_harness.StoppedActivities, a => string.Equals(a.OperationName, Register.RegisterActivityName, StringComparison.Ordinal));
        Assert.False(Assert.IsType<bool>(span.GetTagItem(Telemetry.Metrics.EmailConfirmationSentTagName)));
    }

    [Fact]
    public async Task OnPostAsync_CreateFails_ReRendersWithEveryErrorAndTagsTheSpanFailed()
    {
        // Arrange
        var (model, userManagerMock, signInManagerMock, _) = BuildRegisterFixture(requireConfirmedAccount: false);
        var firstError = Generated.NewFailureReason();
        var secondError = Generated.NewFailureReason();
        userManagerMock
            .Setup(u => u.CreateAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = firstError }, new IdentityError { Description = secondError }));

        // Act
        var result = await model.OnPostAsync(Generated.NewLocalPath());

        // Assert
        Assert.IsType<PageResult>(result);
        var messages = model.ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
        Assert.Equal(new[] { firstError, secondError }, messages);
        signInManagerMock.Verify(s => s.SignInAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<bool>(), It.IsAny<string>()), Times.Never);
        var span = Assert.Single(_harness.StoppedActivities, a => string.Equals(a.OperationName, Register.RegisterActivityName, StringComparison.Ordinal));
        Assert.False(Assert.IsType<bool>(span.GetTagItem(Telemetry.Metrics.SucceededTagName)));
    }

    [Fact]
    public async Task OnPostAsync_BlankEmail_ReturnsPageWithoutVerifyingOrCreating()
    {
        // Arrange
        var captchaMock = CreateRecaptchaServiceMock();
        var (model, userManagerMock, _, _) = BuildRegisterFixture(requireConfirmedAccount: false, captchaMock);
        model.Input = new Register.InputModel { Email = Generated.NewBlank(), Password = Generated.NewPassword() };

        // Act
        var result = await model.OnPostAsync(Generated.NewLocalPath());

        // Assert
        Assert.IsType<PageResult>(result);
        captchaMock.Verify(c => c.VerifyAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        userManagerMock.Verify(u => u.CreateAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_BlankPassword_ReturnsPageWithoutVerifyingOrCreating()
    {
        // Arrange
        var captchaMock = CreateRecaptchaServiceMock();
        var (model, userManagerMock, _, _) = BuildRegisterFixture(requireConfirmedAccount: false, captchaMock);
        model.Input = new Register.InputModel { Email = Generated.NewEmailAddress(), Password = Generated.NewBlank() };

        // Act
        var result = await model.OnPostAsync(Generated.NewLocalPath());

        // Assert
        Assert.IsType<PageResult>(result);
        captchaMock.Verify(c => c.VerifyAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
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

    private static void AssertReturnUrlAssignedWithoutExternalLogins(Exception? ex, Register model, string? returnUrl)
    {
        Assert.Null(ex);
        Assert.Equal(returnUrl, model.ReturnUrl);
        Assert.NotNull(model.ExternalLogins);
        Assert.Empty(model.ExternalLogins);
    }

    private Register BuildModelReturningSchemes(AuthenticationScheme[] schemes)
    {
        var userManagerMock = MockHelpers.MockUserManager();
        userManagerMock.SetupGet(u => u.SupportsUserEmail).Returns(true);

        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);
        signInManagerMock
            .Setup(s => s.GetExternalAuthenticationSchemesAsync())
            .ReturnsAsync(schemes);

        return new Register(
            userManagerMock.Object,
            signInManagerMock.Object,
            CreateClientFactory(),
            CreateRecaptchaServiceMock().Object,
            TestValues.NewAccountEmailSettings(),
            _harness.Telemetry);
    }

    private (
        Register Model,
        Mock<UserManager<IdentityUser<Guid>>> UserManagerMock,
        Mock<SignInManager<IdentityUser<Guid>>> SignInManagerMock,
        Mock<ServiceBusSender> SenderMock) BuildRegisterFixture(bool requireConfirmedAccount, Mock<ICAPTCHAService>? captchaMock = null, bool callbackResolves = true)
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
            (captchaMock ?? CreateRecaptchaServiceMock()).Object,
            _emailSettings,
            _harness.Telemetry);

        var ctx = new DefaultHttpContext();
        ctx.Request.Scheme = Uri.UriSchemeHttps;
        model.PageContext = new PageContext { HttpContext = ctx };

        var urlHelperMock = new Mock<IUrlHelper>(MockBehavior.Strict);
        var urlRouteData = new RouteData();
        urlHelperMock.SetupGet(u => u.ActionContext).Returns(
            new ActionContext(new DefaultHttpContext(), urlRouteData, new ActionDescriptor()));

        urlHelperMock.Setup(u => u.RouteUrl(It.IsAny<UrlRouteContext>()))
            .Callback<UrlRouteContext>(context => RoutedPages.Record(context, _routedPages))
            .Returns(callbackResolves ? Generated.NewCallbackAddress() : null);
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
