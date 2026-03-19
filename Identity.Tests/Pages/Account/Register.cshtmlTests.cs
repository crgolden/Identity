#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
namespace Identity.Tests.Pages.Account;

using Identity.Pages.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Moq;

/// <summary>
/// Tests for Identity.Pages.Account.RegisterModel.OnGetAsync.
/// Focus: ensure ReturnUrl assignment and ExternalLogins population behavior.
/// </summary>
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

    /// <summary>
    /// Tests that OnGetAsync assigns the provided returnUrl value to the ReturnUrl property.
    /// Input conditions: various returnUrl values including null, empty, whitespace-only, long, and special characters.
    /// Expected result: ReturnUrl equals the input returnUrl and no exception is thrown.
    /// </summary>
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
        var userEmailStoreMock = new Mock<IUserEmailStore<IdentityUser<Guid>>>();
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

        var logger = Mock.Of<ILogger<RegisterModel>>();
        var emailSender = Mock.Of<IEmailSender>();

        var model = new RegisterModel(
            userManagerMock.Object,
            userEmailStoreMock.Object,
            signInManagerMock.Object,
            Mock.Of<IAvatarService>(),
            logger,
            emailSender);

        // Act & Assert: ensure no exception and ReturnUrl set as expected
        var ex = await Record.ExceptionAsync(() => model.OnGetAsync(returnUrl));
        Assert.Null(ex);
        Assert.Equal(returnUrl, model.ReturnUrl);

        // ExternalLogins should be an empty list as set up
        Assert.NotNull(model.ExternalLogins);
        Assert.Empty(model.ExternalLogins);
    }

    /// <summary>
    /// Tests that OnGetAsync populates ExternalLogins from SignInManager's GetExternalAuthenticationSchemesAsync.
    /// Input conditions: various collections of AuthenticationScheme (empty, single, multiple).
    /// Expected result: ExternalLogins contains the same schemes (order preserved) and count matches.
    /// </summary>
#pragma warning disable xUnit1045
    [Theory]
    [MemberData(nameof(ExternalSchemesData))]
    public async Task OnGetAsync_ExternalSchemesReturned_PopulatesExternalLogins(IEnumerable<AuthenticationScheme> schemes)
    {
        // Arrange
        var userEmailStoreMock = new Mock<IUserEmailStore<IdentityUser<Guid>>>();
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

        var logger = Mock.Of<ILogger<RegisterModel>>();
        var emailSender = Mock.Of<IEmailSender>();

        var model = new RegisterModel(
            userManagerMock.Object,
            userEmailStoreMock.Object,
            signInManagerMock.Object,
            Mock.Of<IAvatarService>(),
            logger,
            emailSender);

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

    /// <summary>
    /// Tests that when the PageModel's ModelState is invalid OnPostAsync returns PageResult
    /// and does not attempt to create a user. This ensures validation short-circuits creation logic.
    /// Input conditions: ModelState contains an error.
    /// Expected result: IActionResult is PageResult and GetExternalAuthenticationSchemesAsync was still invoked.
    /// </summary>
    [Fact(DisplayName = "OnPostAsync_ModelStateInvalid_ReturnsPage")]
    public async Task OnPostAsync_ModelStateInvalid_ReturnsPage()
    {
        // Arrange
        var userStoreMock = new Mock<IUserEmailStore<IdentityUser<Guid>>>();
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

        var loggerMock = new Mock<ILogger<RegisterModel>>();
        var emailSenderMock = new Mock<IEmailSender>();

        var model = new RegisterModel(
            userManagerMock.Object,
            userStoreMock.Object,
            signInManagerMock.Object,
            Mock.Of<IAvatarService>(),
            loggerMock.Object,
            emailSenderMock.Object);

        // Configure PageContext/Url/Request
        var ctx = new DefaultHttpContext();
        ctx.Request.Scheme = "https";
        model.PageContext = new PageContext { HttpContext = ctx };

        var urlHelperMock = new Mock<IUrlHelper>();
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

    /// <summary>
    /// Parameterized test covering both branches of RequireConfirmedAccount.
    /// Input conditions: Valid ModelState, CreateAsync succeeds.
    /// - When requireConfirmed is true: expect RedirectToPageResult for RegisterConfirmation.
    /// - When requireConfirmed is false: expect LocalRedirectResult to the provided returnUrl and SignInAsync invoked.
    /// </summary>
    [Theory(DisplayName = "OnPostAsync_CreateSucceeds_RespectsRequireConfirmedAccount")]
    [InlineData(true, "/confirmed-redirect")]
    [InlineData(false, "/local-redirect")]
    public async Task OnPostAsync_CreateSucceeds_RespectsRequireConfirmedAccount(bool requireConfirmed, string returnUrl)
    {
        // Arrange
        var userEmailStoreMock = new Mock<IUserEmailStore<IdentityUser<Guid>>>();

        // Pass IdentityOptions with RequireConfirmedAccount through the constructor
        // (Options property is non-virtual and cannot be set up via Moq)
        var identityOptions = new IdentityOptions();
        identityOptions.SignIn.RequireConfirmedAccount = requireConfirmed;
        var identityOptionsMock = new Mock<Microsoft.Extensions.Options.IOptions<IdentityOptions>>();
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

        var loggerMock = new Mock<ILogger<RegisterModel>>();
        var emailSenderMock = new Mock<IEmailSender>();
        emailSenderMock.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var model = new RegisterModel(
            userManagerMock.Object,
            userEmailStoreMock.Object,
            signInManagerMock.Object,
            Mock.Of<IAvatarService>(),
            loggerMock.Object,
            emailSenderMock.Object);

        // Configure PageContext/Url/Request
        var ctx = new DefaultHttpContext();
        ctx.Request.Scheme = "https";
        model.PageContext = new PageContext { HttpContext = ctx };

        var urlHelperMock = new Mock<IUrlHelper>();

        // ActionContext must be non-null because UrlHelperExtensions.Page always accesses it
        var urlRouteData = new RouteData();
        urlRouteData.Values["page"] = "/Account/Register";
        urlHelperMock.SetupGet(u => u.ActionContext).Returns(
            new ActionContext(new DefaultHttpContext(), urlRouteData, new ActionDescriptor()));

        // SetReturnsDefault covers all string?-returning methods including RouteUrl called by Url.Page
        urlHelperMock.SetReturnsDefault<string?>("https://example/confirm");

        // Url.Content explicit setup takes precedence over SetReturnsDefault
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
        emailSenderMock.Verify(e => e.SendEmailAsync(model.Input.Email, It.IsAny<string>(), It.IsAny<string>()), Times.Once);

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