#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
namespace Identity.Tests.Pages.Account;

using Azure.Messaging.ServiceBus;
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
using Microsoft.Extensions.Logging;
using Moq;
using System.Threading.Channels;

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
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(
            Mock.Of<IUserStore<IdentityUser<Guid>>>(), null, null, null, null, null, null, null, null);
        userManagerMock.SetupGet(u => u.SupportsUserEmail).Returns(true);

        var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(
            userManagerMock.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(),
            Mock.Of<Microsoft.Extensions.Options.IOptions<IdentityOptions>>(),
            Mock.Of<ILogger<SignInManager<IdentityUser<Guid>>>>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<IdentityUser<Guid>>>());

        // Provide an empty external schemes result to focus this test on ReturnUrl assignment.
        signInManagerMock
            .Setup(s => s.GetExternalAuthenticationSchemesAsync())
            .ReturnsAsync([]);

        var model = new RegisterModel(
            userManagerMock.Object,
            signInManagerMock.Object,
            Channel.CreateUnbounded<string>().Writer,
            CreateSenderFactory(),
            CreateRecaptchaServiceMock().Object);

        // Act & Assert: ensure no exception and ReturnUrl set as expected
        var ex = await Record.ExceptionAsync(() => model.OnGetAsync(returnUrl));
        Assert.Null(ex);
        Assert.Equal(returnUrl, model.ReturnUrl);

        // ExternalLogins should be an empty list as set up
        Assert.NotNull(model.ExternalLogins);
        Assert.Empty(model.ExternalLogins);
    }

#pragma warning disable xUnit1045
    [Theory]
    [MemberData(nameof(ExternalSchemesData))]
    public async Task OnGetAsync_ExternalSchemesReturned_PopulatesExternalLogins(IEnumerable<AuthenticationScheme> schemes)
    {
        // Arrange
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(
            Mock.Of<IUserStore<IdentityUser<Guid>>>(), null, null, null, null, null, null, null, null);
        userManagerMock.SetupGet(u => u.SupportsUserEmail).Returns(true);

        var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(
            userManagerMock.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(),
            Mock.Of<Microsoft.Extensions.Options.IOptions<IdentityOptions>>(),
            Mock.Of<ILogger<SignInManager<IdentityUser<Guid>>>>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<IdentityUser<Guid>>>());

        signInManagerMock
            .Setup(s => s.GetExternalAuthenticationSchemesAsync())
            .ReturnsAsync(schemes);

        var model = new RegisterModel(
            userManagerMock.Object,
            signInManagerMock.Object,
            Channel.CreateUnbounded<string>().Writer,
            CreateSenderFactory(),
            CreateRecaptchaServiceMock().Object);

        // Act
        await model.OnGetAsync("someReturn");

        // Assert
        Assert.Equal("someReturn", model.ReturnUrl);
        Assert.NotNull(model.ExternalLogins);
        Assert.Equal(schemes.Count(), model.ExternalLogins.Count);

        // Verify that items are present and names match in order
        var expectedNames = schemes.Select(s => s.Name).ToList();
        var actualNames = model.ExternalLogins.Select(s => s.Name).ToList();
        Assert.Equal(expectedNames, actualNames);
    }
#pragma warning restore xUnit1045

    [Fact(DisplayName = "OnPostAsync_ModelStateInvalid_ReturnsPage")]
    public async Task OnPostAsync_ModelStateInvalid_ReturnsPage()
    {
        // Arrange
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(
            Mock.Of<IUserStore<IdentityUser<Guid>>>(), null, null, null, null, null, null, null, null);
        userManagerMock.SetupGet(u => u.SupportsUserEmail).Returns(true);
        var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(
            userManagerMock.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(),
            null,
            Mock.Of<ILogger<SignInManager<IdentityUser<Guid>>>>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<IdentityUser<Guid>>>());

        signInManagerMock
            .Setup(s => s.GetExternalAuthenticationSchemesAsync())
            .ReturnsAsync([]);

        var model = new RegisterModel(
            userManagerMock.Object,
            signInManagerMock.Object,
            Channel.CreateUnbounded<string>().Writer,
            CreateSenderFactory(),
            CreateRecaptchaServiceMock().Object);

        // Configure PageContext/Url/Request
        var ctx = new DefaultHttpContext();
        ctx.Request.Scheme = "https";
        model.PageContext = new PageContext { HttpContext = ctx };

        var urlHelperMock = new Mock<IUrlHelper>(MockBehavior.Strict);
        urlHelperMock.Setup(u => u.Content("~/")).Returns("/");
        model.Url = urlHelperMock.Object;

        // Invalidate model state
        model.ModelState.AddModelError("someKey", "some error");

        // Act
        var result = await model.OnPostAsync("/return");

        // Assert
        Assert.IsType<PageResult>(result);
        signInManagerMock.Verify(s => s.GetExternalAuthenticationSchemesAsync(), Times.Once);

        // Ensure no user creation call was attempted
        userManagerMock.Verify(u => u.CreateAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<string>()), Times.Never);
    }

    [Theory(DisplayName = "OnPostAsync_CreateSucceeds_RespectsRequireConfirmedAccount")]
    [InlineData(true, "/confirmed-redirect")]
    [InlineData(false, "/local-redirect")]
    public async Task OnPostAsync_CreateSucceeds_RespectsRequireConfirmedAccount(bool requireConfirmed, string returnUrl)
    {
        // Arrange

        // Pass IdentityOptions with RequireConfirmedAccount through the constructor
        // (Options property is non-virtual and cannot be set up via Moq)
        var identityOptions = new IdentityOptions();
        identityOptions.SignIn.RequireConfirmedAccount = requireConfirmed;
        var identityOptionsMock = new Mock<Microsoft.Extensions.Options.IOptions<IdentityOptions>>(MockBehavior.Strict);
        identityOptionsMock.Setup(o => o.Value).Returns(identityOptions);
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(
            Mock.Of<IUserStore<IdentityUser<Guid>>>(), identityOptionsMock.Object, null, null, null, null, null, null, null);
        userManagerMock.SetupGet(u => u.SupportsUserEmail).Returns(true);

        // Configure UserManager behaviors
        userManagerMock
            .Setup(u => u.CreateAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        userManagerMock
            .Setup(u => u.GetUserIdAsync(It.IsAny<IdentityUser<Guid>>()))
            .ReturnsAsync("test-user-id");

        userManagerMock
            .Setup(u => u.GenerateEmailConfirmationTokenAsync(It.IsAny<IdentityUser<Guid>>()))
            .ReturnsAsync("raw-token");

        var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(
            userManagerMock.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(),
            null,
            Mock.Of<ILogger<SignInManager<IdentityUser<Guid>>>>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<IdentityUser<Guid>>>());

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
            Channel.CreateUnbounded<string>().Writer,
            senderFactory,
            CreateRecaptchaServiceMock().Object);

        // Configure PageContext/Url/Request
        var ctx = new DefaultHttpContext();
        ctx.Request.Scheme = "https";
        model.PageContext = new PageContext { HttpContext = ctx };

        var urlHelperMock = new Mock<IUrlHelper>(MockBehavior.Strict);

        // ActionContext must be non-null because UrlHelperExtensions.Page always accesses it
        var urlRouteData = new RouteData();
        urlRouteData.Values["page"] = "/Account/Register";
        urlHelperMock.SetupGet(u => u.ActionContext).Returns(
            new ActionContext(new DefaultHttpContext(), urlRouteData, new ActionDescriptor()));

        urlHelperMock.Setup(u => u.RouteUrl(It.IsAny<UrlRouteContext>())).Returns("https://example/confirm");
        urlHelperMock.Setup(u => u.Content("~/")).Returns("/");
        model.Url = urlHelperMock.Object;

        // Provide Input
        model.Input = new RegisterModel.InputModel
        {
            Email = "user@example.com",
            Password = "P@ssw0rd!"
        };

        // Act
        var actionResult = await model.OnPostAsync(returnUrl);

        // Assert
        // Always should call CreateAsync
        userManagerMock.Verify(u => u.CreateAsync(It.IsAny<IdentityUser<Guid>>(), It.Is<string>(p => p == model.Input.Password)), Times.Once);
        senderMock.Verify(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Once);

        if (requireConfirmed)
        {
            var redirect = Assert.IsType<RedirectToPageResult>(actionResult);
            Assert.Equal("RegisterConfirmation", redirect.PageName);

            // RouteValues should contain email and returnUrl
            Assert.NotNull(redirect.RouteValues);
            Assert.Equal(model.Input.Email, redirect.RouteValues["email"]);
            Assert.Equal(returnUrl, redirect.RouteValues["returnUrl"]);

            // SignIn should not be called when RequireConfirmedAccount is true
            signInManagerMock.Verify(s => s.SignInAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<bool>(), It.IsAny<string>()), Times.Never);
        }
        else
        {
            var local = Assert.IsType<LocalRedirectResult>(actionResult);
            Assert.Equal(returnUrl, local.Url);
            signInManagerMock.Verify(s => s.SignInAsync(It.IsAny<IdentityUser<Guid>>(), false, null), Times.Once);
        }
    }

    [Fact]
    public async Task OnPostAsync_RecaptchaScoreBelowThreshold_ReturnsPageWithModelError()
    {
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(
            Mock.Of<IUserStore<IdentityUser<Guid>>>(), null, null, null, null, null, null, null, null);
        userManagerMock.SetupGet(u => u.SupportsUserEmail).Returns(true);
        var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(
            userManagerMock.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(),
            null,
            Mock.Of<ILogger<SignInManager<IdentityUser<Guid>>>>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<IdentityUser<Guid>>>());
        signInManagerMock.Setup(s => s.GetExternalAuthenticationSchemesAsync()).ReturnsAsync([]);

        var recaptchaServiceMock = CreateRecaptchaServiceMock(score: 0.0m);

        var model = new RegisterModel(
            userManagerMock.Object,
            signInManagerMock.Object,
            Channel.CreateUnbounded<string>().Writer,
            CreateSenderFactory(),
            recaptchaServiceMock.Object);

        var ctx = new DefaultHttpContext();
        ctx.Request.Scheme = "https";
        model.PageContext = new PageContext { HttpContext = ctx };

        var urlHelperMock = new Mock<IUrlHelper>(MockBehavior.Strict);
        urlHelperMock.Setup(u => u.Content("~/")).Returns("/");
        model.Url = urlHelperMock.Object;
        model.Input = new RegisterModel.InputModel { Email = "user@example.com", Password = "P@ssw0rd!" };

        var result = await model.OnPostAsync("/return");

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        Assert.True(model.ModelState.ContainsKey(string.Empty));
        userManagerMock.Verify(u => u.CreateAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_TestEmail_SkipsRecaptchaAndCreatesUser()
    {
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(
            Mock.Of<IUserStore<IdentityUser<Guid>>>(), null, null, null, null, null, null, null, null);
        userManagerMock.SetupGet(u => u.SupportsUserEmail).Returns(true);
        userManagerMock
            .Setup(u => u.CreateAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        userManagerMock
            .Setup(u => u.GetUserIdAsync(It.IsAny<IdentityUser<Guid>>()))
            .ReturnsAsync("test-user-id");
        userManagerMock
            .Setup(u => u.GenerateEmailConfirmationTokenAsync(It.IsAny<IdentityUser<Guid>>()))
            .ReturnsAsync("raw-token");

        var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(
            userManagerMock.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(),
            null,
            Mock.Of<ILogger<SignInManager<IdentityUser<Guid>>>>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<IdentityUser<Guid>>>());
        signInManagerMock.Setup(s => s.GetExternalAuthenticationSchemesAsync()).ReturnsAsync([]);

        var recaptchaServiceMock = CreateRecaptchaServiceMock(score: 0.0m);
        recaptchaServiceMock.Setup(s => s.IsExempt("smoke@example.com")).Returns(true);

        var model = new RegisterModel(
            userManagerMock.Object,
            signInManagerMock.Object,
            Channel.CreateUnbounded<string>().Writer,
            CreateSenderFactory(),
            recaptchaServiceMock.Object);

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
        model.Input = new RegisterModel.InputModel { Email = "smoke@example.com", Password = "P@ssw0rd!" };

        await model.OnPostAsync("/return");

        userManagerMock.Verify(u => u.CreateAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<string>()), Times.Once);
        recaptchaServiceMock.Verify(s => s.VerifyAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static IAzureClientFactory<ServiceBusSender> CreateSenderFactory()
    {
        var senderMock = new Mock<ServiceBusSender>(MockBehavior.Strict);
        senderMock.Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var factoryMock = new Mock<IAzureClientFactory<ServiceBusSender>>(MockBehavior.Strict);
        factoryMock.Setup(f => f.CreateClient("email")).Returns(senderMock.Object);
        return factoryMock.Object;
    }

    private static (IAzureClientFactory<ServiceBusSender> factory, Mock<ServiceBusSender> senderMock) CreateSenderFactoryWithMock()
    {
        var senderMock = new Mock<ServiceBusSender>(MockBehavior.Strict);
        senderMock.Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var factoryMock = new Mock<IAzureClientFactory<ServiceBusSender>>(MockBehavior.Strict);
        factoryMock.Setup(f => f.CreateClient("email")).Returns(senderMock.Object);
        return (factoryMock.Object, senderMock);
    }

    private static Mock<ICAPTCHAService> CreateRecaptchaServiceMock(decimal score = 1.0m, decimal threshold = 0.5m)
    {
        var mock = new Mock<ICAPTCHAService>(MockBehavior.Strict);
        mock.Setup(s => s.SiteKey).Returns((string?)null);
        mock.Setup(s => s.ScoreThreshold).Returns(threshold);
        mock.Setup(s => s.VerifyAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(score);
        mock.Setup(s => s.IsExempt(It.IsAny<string?>())).Returns(false);
        return mock;
    }

    // Minimal IAuthenticationHandler implementation for use in AuthenticationScheme construction
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
