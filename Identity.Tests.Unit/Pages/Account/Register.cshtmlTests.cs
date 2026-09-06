namespace Identity.Tests.Unit.Pages.Account;

using Azure.Messaging.ServiceBus;
using CAPTCHA;
using Identity.Pages.Account;
using Infrastructure;
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
public class RegisterModelTests
{
    public static TheoryData<IEnumerable<AuthenticationScheme>> ExternalSchemesData() => new()
    {
        Enumerable.Empty<AuthenticationScheme>(),
        new List<AuthenticationScheme>
        {
            new AuthenticationScheme("Provider1", "Provider One", typeof(DummyAuthHandler))
        },
        new List<AuthenticationScheme>
        {
            new AuthenticationScheme("ProviderA", "A", typeof(DummyAuthHandler)),
            new AuthenticationScheme("ProviderB", "B", typeof(DummyAuthHandler)),
            new AuthenticationScheme("ProviderC", "C", typeof(DummyAuthHandler))
        },
    };

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("a")]
    [InlineData("a/../?%#&")]
    [InlineData("LongString_" + "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public async Task OnGetAsync_VariousReturnUrlValues_AssignsReturnUrlAndDoesNotThrow(string? returnUrl)
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();
        userManagerMock.SetupGet(u => u.SupportsUserEmail).Returns(true);

        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);
        signInManagerMock
            .Setup(s => s.GetExternalAuthenticationSchemesAsync())
            .ReturnsAsync([]);

        var model = new RegisterModel(
            userManagerMock.Object,
            signInManagerMock.Object,
            CreateClientFactory(),
            CreateRecaptchaServiceMock().Object);

        // Act
        var ex = await Record.ExceptionAsync(() => model.OnGetAsync(returnUrl));

        // Assert
        Assert.Null(ex);
        Assert.Equal(returnUrl, model.ReturnUrl);
        Assert.NotNull(model.ExternalLogins);
        Assert.Empty(model.ExternalLogins);
    }

#pragma warning disable xUnit1045
    [Theory]
    [MemberData(nameof(ExternalSchemesData))]
    public async Task OnGetAsync_ExternalSchemesReturned_PopulatesExternalLogins(IEnumerable<AuthenticationScheme> schemes)
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();
        userManagerMock.SetupGet(u => u.SupportsUserEmail).Returns(true);

        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);

        signInManagerMock
            .Setup(s => s.GetExternalAuthenticationSchemesAsync())
            .ReturnsAsync(schemes);

        var model = new RegisterModel(
            userManagerMock.Object,
            signInManagerMock.Object,
            CreateClientFactory(),
            CreateRecaptchaServiceMock().Object);

        // Act
        await model.OnGetAsync("someReturn");

        // Assert
        Assert.Equal("someReturn", model.ReturnUrl);
        Assert.NotNull(model.ExternalLogins);
        Assert.Equal(schemes.Count(), model.ExternalLogins.Count);
        var expectedNames = schemes.Select(s => s.Name).ToList();
        var actualNames = model.ExternalLogins.Select(s => s.Name).ToList();
        Assert.Equal(expectedNames, actualNames);
    }
#pragma warning restore xUnit1045

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

        var model = new RegisterModel(
            userManagerMock.Object,
            signInManagerMock.Object,
            CreateClientFactory(),
            CreateRecaptchaServiceMock().Object);

        var ctx = new DefaultHttpContext();
        ctx.Request.Scheme = "https";
        model.PageContext = new PageContext { HttpContext = ctx };

        var urlHelperMock = new Mock<IUrlHelper>(MockBehavior.Strict);
        urlHelperMock.Setup(u => u.Content("~/")).Returns("/");
        model.Url = urlHelperMock.Object;
        model.ModelState.AddModelError(TestValues.NewClaimType(), TestValues.NewFailureReason());

        // Act
        var result = await model.OnPostAsync("/return");

        // Assert
        Assert.IsType<PageResult>(result);
        signInManagerMock.Verify(s => s.GetExternalAuthenticationSchemesAsync(), Times.Once);
        userManagerMock.Verify(u => u.CreateAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_CreateSucceedsAndConfirmationRequired_RedirectsToRegisterConfirmationWithoutSigningIn()
    {
        // Arrange
        var returnUrl = $"/confirmed-redirect-{Guid.NewGuid():N}";
        var (model, userManagerMock, signInManagerMock, senderMock) = BuildRegisterFixture(requireConfirmedAccount: true);

        // Act
        var actionResult = await model.OnPostAsync(returnUrl);

        // Assert
        userManagerMock.Verify(u => u.CreateAsync(It.IsAny<IdentityUser<Guid>>(), It.Is<string>(p => p == model.Input.Password)), Times.Once);
        senderMock.Verify(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        var redirect = Assert.IsType<RedirectToPageResult>(actionResult);
        Assert.Equal("RegisterConfirmation", redirect.PageName);
        Assert.NotNull(redirect.RouteValues);
        Assert.Equal(model.Input.Email, redirect.RouteValues["email"]);
        Assert.Equal(returnUrl, redirect.RouteValues["returnUrl"]);
        signInManagerMock.Verify(s => s.SignInAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<bool>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_CreateSucceedsAndConfirmationNotRequired_SignsInAndRedirectsLocally()
    {
        // Arrange
        var returnUrl = $"/local-redirect-{Guid.NewGuid():N}";
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
    public async Task OnPostAsync_RecaptchaScoreBelowThreshold_ReturnsPageWithModelError()
    {
        var userManagerMock = MockHelpers.MockUserManager();
        userManagerMock.SetupGet(u => u.SupportsUserEmail).Returns(true);
        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);
        signInManagerMock.Setup(s => s.GetExternalAuthenticationSchemesAsync()).ReturnsAsync([]);

        var recaptchaServiceMock = CreateRecaptchaServiceMock(passed: false);

        var model = new RegisterModel(
            userManagerMock.Object,
            signInManagerMock.Object,
            CreateClientFactory(),
            recaptchaServiceMock.Object);

        var ctx = new DefaultHttpContext();
        ctx.Request.Scheme = "https";
        model.PageContext = new PageContext { HttpContext = ctx };

        var urlHelperMock = new Mock<IUrlHelper>(MockBehavior.Strict);
        urlHelperMock.Setup(u => u.Content("~/")).Returns("/");
        model.Url = urlHelperMock.Object;
        model.Input = new RegisterModel.InputModel { Email = TestValues.NewEmailAddress(), Password = TestValues.NewPassword() };

        var result = await model.OnPostAsync("/return");

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        Assert.True(model.ModelState.ContainsKey(string.Empty));
        userManagerMock.Verify(u => u.CreateAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<string>()), Times.Never);
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

    private static Mock<ICAPTCHAService> CreateRecaptchaServiceMock(bool passed = true)
    {
        var mock = new Mock<ICAPTCHAService>(MockBehavior.Strict);
        var score = passed
            ? TestValues.NewScoreAtOrAboveDefaultThreshold()
            : TestValues.NewScoreBelowDefaultThreshold();
        mock.Setup(s => s.SiteKey).Returns((string?)null);
        mock.Setup(s => s.VerifyAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CAPTCHAVerdict(passed, score));
        return mock;
    }

    private static (
        RegisterModel Model,
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

        userManagerMock
            .Setup(u => u.GetUserIdAsync(It.IsAny<IdentityUser<Guid>>()))
            .ReturnsAsync(Guid.NewGuid().ToString());

        userManagerMock
            .Setup(u => u.GenerateEmailConfirmationTokenAsync(It.IsAny<IdentityUser<Guid>>()))
            .ReturnsAsync(Guid.NewGuid().ToString("N"));

        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);

        signInManagerMock
            .Setup(s => s.GetExternalAuthenticationSchemesAsync())
            .ReturnsAsync([]);

        signInManagerMock
            .Setup(s => s.SignInAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<bool>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var (senderFactory, senderMock) = CreateSenderFactoryWithMock();

        var model = new RegisterModel(
            userManagerMock.Object,
            signInManagerMock.Object,
            senderFactory,
            CreateRecaptchaServiceMock().Object);

        var ctx = new DefaultHttpContext();
        ctx.Request.Scheme = "https";
        model.PageContext = new PageContext { HttpContext = ctx };

        var urlHelperMock = new Mock<IUrlHelper>(MockBehavior.Strict);
        var urlRouteData = new RouteData();
        urlRouteData.Values["page"] = "/Account/Register";
        urlHelperMock.SetupGet(u => u.ActionContext).Returns(
            new ActionContext(new DefaultHttpContext(), urlRouteData, new ActionDescriptor()));

        urlHelperMock.Setup(u => u.RouteUrl(It.IsAny<UrlRouteContext>())).Returns("https://example/confirm");
        urlHelperMock.Setup(u => u.Content("~/")).Returns("/");
        model.Url = urlHelperMock.Object;
        model.Input = new RegisterModel.InputModel
        {
            Email = TestValues.NewEmailAddress(),
            Password = $"P@ss{Guid.NewGuid():N}!"
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
