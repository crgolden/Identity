#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
namespace Identity.Tests.Pages.Account;

using Identity;
using Identity.Pages.Account;
using Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Threading.Channels;

/// <summary>
/// Tests for Identity.Pages.Account.LoginModel constructor behavior.
/// </summary>
[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class LoginModelTests
{
    /// <summary>
    /// Verifies that the LoginModel constructor does not throw and produces a usable PageModel instance
    /// when signInManager is null and logger is either provided or null.
    /// Inputs:
    ///  - signInManager: null (SignInManager{IdentityUser{Guid}}?).
    ///  - logger: nullable; tested both null and a mocked ILogger instance.
    /// Expected:
    ///  - No exception is thrown.
    ///  - The constructed instance is not null and is assignable to PageModel.
    ///  - Public properties that are not initialized by the constructor (e.g., Input) remain null.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Constructor_NullSignInManager_DoesNotThrow(bool provideLogger)
    {
        // Arrange
        SignInManager<IdentityUser<Guid>>? signInManager = null;
        var logger = provideLogger
            ? new Mock<ILogger<LoginModel>>().Object
            : null;

        // Act
        LoginModel model = null!;
        var ex = Record.Exception(() => model = new LoginModel(signInManager, null, logger, null, null));

        // Assert
        Assert.Null(ex);
        Assert.NotNull(model);
        Assert.IsType<PageModel>(model, exactMatch: false);

        // Constructor initializes Input with a default InputModel instance.
        Assert.NotNull(model.Input);

        // ReturnUrl is not set by constructor; expect null.
        Assert.Null(model.ReturnUrl);

        // ExternalLogins is initialized to an empty list by the field initializer.
        Assert.NotNull(model.ExternalLogins);
        Assert.Empty(model.ExternalLogins);
    }

    /// <summary>
    /// Verifies that the LoginModel constructor accepts both parameters as null without throwing.
    /// Inputs:
    ///  - signInManager: null
    ///  - logger: null
    /// Expected:
    ///  - No exception is thrown and instance is constructible.
    /// </summary>
    [Fact]
    public void Constructor_BothParametersNull_DoesNotThrow()
    {
        // Arrange
        SignInManager<IdentityUser<Guid>>? signInManager = null;
        ILogger<LoginModel>? logger = null;

        // Act & Assert
        var ex = Record.Exception(() => new LoginModel(signInManager, null, logger, null, null));
        Assert.Null(ex);
    }

    [Fact]
    public async Task OnGetAsync_WithErrorMessage_AddsModelError()
    {
        // Arrange
        var (model, _) = CreateModelWithContext();
        model.ErrorMessage = "Login failed.";

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.False(model.ModelState.IsValid);
        Assert.True(model.ModelState.ContainsKey(string.Empty));
    }

    [Fact]
    public async Task OnGetAsync_WithoutErrorMessage_DoesNotAddModelError()
    {
        // Arrange
        var (model, _) = CreateModelWithContext();

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.True(model.ModelState.IsValid);
    }

    [Fact]
    public async Task OnGetAsync_WithReturnUrl_SetsReturnUrl()
    {
        // Arrange
        var (model, _) = CreateModelWithContext();

        // Act
        await model.OnGetAsync("/dashboard");

        // Assert
        Assert.Equal("/dashboard", model.ReturnUrl);
    }

    [Fact]
    public async Task OnGetAsync_WithoutReturnUrl_DefaultsToRoot()
    {
        // Arrange
        var (model, _) = CreateModelWithContext();

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.Equal("/", model.ReturnUrl);
    }

    [Fact]
    public async Task OnGetAsync_ExternalSchemesAvailable_PopulatesExternalLogins()
    {
        // Arrange
        var scheme = new AuthenticationScheme("Google", "Google", typeof(IAuthenticationHandler));
        var (model, _) = CreateModelWithContext(schemes: [scheme]);

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.Single(model.ExternalLogins);
        Assert.Equal("Google", model.ExternalLogins[0].Name);
    }

    [Fact]
    public async Task OnPostAsync_PasswordSignIn_Succeeded_ReturnsLocalRedirect()
    {
        // Arrange
        var signInManagerMock = CreateSignInManagerMock();
        signInManagerMock
            .Setup(s => s.PasswordSignInAsync("user@example.com", "pass", false, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock.Setup(u => u.Content("~/")).Returns("/");
        urlHelperMock.Setup(u => u.IsLocalUrl("/")).Returns(true);

        var model = new LoginModel(signInManagerMock.Object, Channel.CreateUnbounded<Func<IServiceProvider, CancellationToken, Task>>().Writer, Mock.Of<ILogger<LoginModel>>(), CreateRecaptchaServiceMock().Object, CreateRecaptchaOptionsMock())
        {
            Url = urlHelperMock.Object,
            PageContext = new PageContext(new ActionContext(new DefaultHttpContext(), new RouteData(), new PageActionDescriptor())),
            Input = new LoginModel.InputModel { Email = "user@example.com", Password = "pass" }
        };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var redirect = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal("/", redirect.Url);
    }

    [Fact]
    public async Task OnPostAsync_PasswordSignIn_RequiresTwoFactor_RedirectsToLoginWith2fa()
    {
        // Arrange
        var signInManagerMock = CreateSignInManagerMock();
        signInManagerMock
            .Setup(s => s.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.TwoFactorRequired);

        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock.Setup(u => u.Content("~/")).Returns("/");

        var model = new LoginModel(signInManagerMock.Object, Channel.CreateUnbounded<Func<IServiceProvider, CancellationToken, Task>>().Writer, Mock.Of<ILogger<LoginModel>>(), CreateRecaptchaServiceMock().Object, CreateRecaptchaOptionsMock())
        {
            Url = urlHelperMock.Object,
            PageContext = new PageContext(new ActionContext(new DefaultHttpContext(), new RouteData(), new PageActionDescriptor())),
            Input = new LoginModel.InputModel { Email = "user@example.com", Password = "pass" }
        };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./LoginWith2fa", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_PasswordSignIn_IsLockedOut_RedirectsToLockout()
    {
        // Arrange
        var signInManagerMock = CreateSignInManagerMock();
        signInManagerMock
            .Setup(s => s.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);

        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock.Setup(u => u.Content("~/")).Returns("/");

        var model = new LoginModel(signInManagerMock.Object, Channel.CreateUnbounded<Func<IServiceProvider, CancellationToken, Task>>().Writer, Mock.Of<ILogger<LoginModel>>(), CreateRecaptchaServiceMock().Object, CreateRecaptchaOptionsMock())
        {
            Url = urlHelperMock.Object,
            PageContext = new PageContext(new ActionContext(new DefaultHttpContext(), new RouteData(), new PageActionDescriptor())),
            Input = new LoginModel.InputModel { Email = "user@example.com", Password = "pass" }
        };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./Lockout", redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_PasswordSignIn_Failed_ReturnsPageWithModelError()
    {
        // Arrange
        var signInManagerMock = CreateSignInManagerMock();
        signInManagerMock
            .Setup(s => s.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock.Setup(u => u.Content("~/")).Returns("/");

        var model = new LoginModel(signInManagerMock.Object, Channel.CreateUnbounded<Func<IServiceProvider, CancellationToken, Task>>().Writer, Mock.Of<ILogger<LoginModel>>(), CreateRecaptchaServiceMock().Object, CreateRecaptchaOptionsMock())
        {
            Url = urlHelperMock.Object,
            PageContext = new PageContext(new ActionContext(new DefaultHttpContext(), new RouteData(), new PageActionDescriptor())),
            Input = new LoginModel.InputModel { Email = "user@example.com", Password = "pass" }
        };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
    }

    [Fact]
    public async Task OnPostAsync_PasskeySignIn_Succeeded_ReturnsLocalRedirect()
    {
        // Arrange
        var signInManagerMock = CreateSignInManagerMock();
        signInManagerMock
            .Setup(s => s.PasskeySignInAsync("{\"credentialJson\":true}"))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock.Setup(u => u.Content("~/")).Returns("/");

        var model = new LoginModel(signInManagerMock.Object, Channel.CreateUnbounded<Func<IServiceProvider, CancellationToken, Task>>().Writer, Mock.Of<ILogger<LoginModel>>(), CreateRecaptchaServiceMock().Object, CreateRecaptchaOptionsMock())
        {
            Url = urlHelperMock.Object,
            Input = new LoginModel.InputModel
            {
                Passkey = new Identity.Pages.Account.Manage.PasskeyInputModel { CredentialJson = "{\"credentialJson\":true}" }
            }
        };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.IsType<LocalRedirectResult>(result);
    }

    /// <summary>
    /// Verifies that when the model state is invalid and no passkey credential is provided,
    /// the method returns PageResult without calling PasswordSignInAsync.
    /// Input conditions: Input.Passkey is null and ModelState contains an error.
    /// Expected result: returns PageResult and PasswordSignInAsync is not invoked.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task OnPostAsync_InvalidModelState_ReturnsPageWithoutSignIn()
    {
        // Arrange
        var storeMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(storeMock.Object, null, null, null, null, null, null, null, null);
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        var claimsFactoryMock = new Mock<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>();
        var optionsMock = new Mock<IOptions<IdentityOptions>>();
        optionsMock.Setup(o => o.Value).Returns(new IdentityOptions());
        var signInLoggerMock = new Mock<ILogger<SignInManager<IdentityUser<Guid>>>>();
        var schemesMock = new Mock<IAuthenticationSchemeProvider>();
        var confirmationMock = new Mock<IUserConfirmation<IdentityUser<Guid>>>();

        var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(
                userManagerMock.Object,
                httpContextAccessorMock.Object,
                claimsFactoryMock.Object,
                optionsMock.Object,
                signInLoggerMock.Object,
                schemesMock.Object,
                confirmationMock.Object)
        { CallBase = false };

        signInManagerMock.Setup(s => s.GetExternalAuthenticationSchemesAsync())
            .ReturnsAsync([]);

        var loggerMock = new Mock<ILogger<LoginModel>>();
        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock.Setup(u => u.Content("~/")).Returns("/");

        var model = new LoginModel(signInManagerMock.Object, Channel.CreateUnbounded<Func<IServiceProvider, CancellationToken, Task>>().Writer, loggerMock.Object, CreateRecaptchaServiceMock().Object, CreateRecaptchaOptionsMock())
        {
            Url = urlHelperMock.Object,
            Input = new LoginModel.InputModel
            {
                Passkey = null,
                Email = "user@example.com",
                Password = "pw",
                RememberMe = false
            }
        };

        // Make ModelState invalid
        model.ModelState.AddModelError("error", "invalid");

        // Act
        var result = await model.OnPostAsync(returnUrl: null);

        // Assert
        Assert.IsType<PageResult>(result);
        signInManagerMock.Verify(s => s.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_RecaptchaScoreBelowThreshold_ReturnsPageWithModelError()
    {
        var signInManagerMock = CreateSignInManagerMock();
        var recaptchaServiceMock = CreateRecaptchaServiceMock(score: 0.0m);

        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock.Setup(u => u.Content("~/")).Returns("/");

        var model = new LoginModel(signInManagerMock.Object, Channel.CreateUnbounded<Func<IServiceProvider, CancellationToken, Task>>().Writer, Mock.Of<ILogger<LoginModel>>(), recaptchaServiceMock.Object, CreateRecaptchaOptionsMock())
        {
            Url = urlHelperMock.Object,
            PageContext = new PageContext(new ActionContext(new DefaultHttpContext(), new RouteData(), new PageActionDescriptor())),
            Input = new LoginModel.InputModel { Email = "user@example.com", Password = "pass" }
        };

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        Assert.True(model.ModelState.ContainsKey(string.Empty));
        signInManagerMock.Verify(s => s.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_PasskeyPath_SkipsRecaptcha()
    {
        var signInManagerMock = CreateSignInManagerMock();
        signInManagerMock
            .Setup(s => s.PasskeySignInAsync(It.IsAny<string>()))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        var recaptchaServiceMock = CreateRecaptchaServiceMock(score: 0.0m);

        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock.Setup(u => u.Content("~/")).Returns("/");
        urlHelperMock.Setup(u => u.IsLocalUrl("/")).Returns(true);

        var model = new LoginModel(signInManagerMock.Object, Channel.CreateUnbounded<Func<IServiceProvider, CancellationToken, Task>>().Writer, Mock.Of<ILogger<LoginModel>>(), recaptchaServiceMock.Object, CreateRecaptchaOptionsMock())
        {
            Url = urlHelperMock.Object,
            Input = new LoginModel.InputModel
            {
                Passkey = new Identity.Pages.Account.Manage.PasskeyInputModel { CredentialJson = "{\"credentialJson\":true}" }
            }
        };

        var result = await model.OnPostAsync();

        Assert.IsType<LocalRedirectResult>(result);
        recaptchaServiceMock.Verify(s => s.VerifyAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_SmokeTestEmail_SkipsRecaptchaAndSucceeds()
    {
        var signInManagerMock = CreateSignInManagerMock();
        signInManagerMock
            .Setup(s => s.PasswordSignInAsync("smoke@example.com", "pass", false, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        var recaptchaServiceMock = CreateRecaptchaServiceMock(score: 0.0m);

        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock.Setup(u => u.Content("~/")).Returns("/");
        urlHelperMock.Setup(u => u.IsLocalUrl("/")).Returns(true);

        var model = new LoginModel(signInManagerMock.Object, Channel.CreateUnbounded<Func<IServiceProvider, CancellationToken, Task>>().Writer, Mock.Of<ILogger<LoginModel>>(), recaptchaServiceMock.Object, CreateRecaptchaOptionsMock(smokeTestEmail: "smoke@example.com"))
        {
            Url = urlHelperMock.Object,
            PageContext = new PageContext(new ActionContext(new DefaultHttpContext(), new RouteData(), new PageActionDescriptor())),
            Input = new LoginModel.InputModel { Email = "smoke@example.com", Password = "pass" }
        };

        var result = await model.OnPostAsync();

        Assert.IsType<LocalRedirectResult>(result);
        recaptchaServiceMock.Verify(s => s.VerifyAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_EmailDoesNotMatchSmokeTestEmail_RunsRecaptchaAndRejects()
    {
        var signInManagerMock = CreateSignInManagerMock();
        var recaptchaServiceMock = CreateRecaptchaServiceMock(score: 0.0m);

        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock.Setup(u => u.Content("~/")).Returns("/");

        var model = new LoginModel(signInManagerMock.Object, Channel.CreateUnbounded<Func<IServiceProvider, CancellationToken, Task>>().Writer, Mock.Of<ILogger<LoginModel>>(), recaptchaServiceMock.Object, CreateRecaptchaOptionsMock(smokeTestEmail: "smoke@example.com"))
        {
            Url = urlHelperMock.Object,
            PageContext = new PageContext(new ActionContext(new DefaultHttpContext(), new RouteData(), new PageActionDescriptor())),
            Input = new LoginModel.InputModel { Email = "other@example.com", Password = "pass" }
        };

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        recaptchaServiceMock.Verify(s => s.VerifyAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_SmokeTestEmailNull_RunsRecaptchaAndRejects()
    {
        var signInManagerMock = CreateSignInManagerMock();
        var recaptchaServiceMock = CreateRecaptchaServiceMock(score: 0.0m);

        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock.Setup(u => u.Content("~/")).Returns("/");

        var model = new LoginModel(signInManagerMock.Object, Channel.CreateUnbounded<Func<IServiceProvider, CancellationToken, Task>>().Writer, Mock.Of<ILogger<LoginModel>>(), recaptchaServiceMock.Object, CreateRecaptchaOptionsMock(smokeTestEmail: null))
        {
            Url = urlHelperMock.Object,
            PageContext = new PageContext(new ActionContext(new DefaultHttpContext(), new RouteData(), new PageActionDescriptor())),
            Input = new LoginModel.InputModel { Email = "user@example.com", Password = "pass" }
        };

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        recaptchaServiceMock.Verify(s => s.VerifyAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static (LoginModel model, Mock<SignInManager<IdentityUser<Guid>>> signInManagerMock) CreateModelWithContext(
        IList<AuthenticationScheme>? schemes = null)
    {
        var storeMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(storeMock.Object, null, null, null, null, null, null, null, null);
        var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(
            userManagerMock.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(),
            null,
            null,
            null,
            null);
        signInManagerMock
            .Setup(s => s.GetExternalAuthenticationSchemesAsync())
            .ReturnsAsync(schemes ?? []);

        var authServiceMock = new Mock<IAuthenticationService>();
        authServiceMock
            .Setup(a => a.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties?>()))
            .Returns(Task.CompletedTask);

        var serviceProvider = new ServiceCollection()
            .AddSingleton(authServiceMock.Object)
            .BuildServiceProvider();

        var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };
        var model = new LoginModel(signInManagerMock.Object, Channel.CreateUnbounded<Func<IServiceProvider, CancellationToken, Task>>().Writer, Mock.Of<ILogger<LoginModel>>(), CreateRecaptchaServiceMock().Object, CreateRecaptchaOptionsMock());
        model.PageContext = new PageContext(new ActionContext(httpContext, new RouteData(), new PageActionDescriptor()));

        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock.Setup(u => u.Content("~/")).Returns("/");
        model.Url = urlHelperMock.Object;

        return (model, signInManagerMock);
    }

    private static Mock<SignInManager<IdentityUser<Guid>>> CreateSignInManagerMock()
    {
        var storeMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(storeMock.Object, null, null, null, null, null, null, null, null);
        var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(
            userManagerMock.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(),
            null,
            null,
            null,
            null);
        signInManagerMock
            .Setup(s => s.GetExternalAuthenticationSchemesAsync())
            .ReturnsAsync([]);
        return signInManagerMock;
    }

    private static Mock<ICAPTCHAService> CreateRecaptchaServiceMock(decimal score = 1.0m)
    {
        var mock = new Mock<ICAPTCHAService>();
        mock.Setup(s => s.VerifyAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(score);
        return mock;
    }

    private static IOptions<ReCAPTCHAOptions> CreateRecaptchaOptionsMock(decimal threshold = 0.5m, string? smokeTestEmail = null)
    {
        var mock = new Mock<IOptions<ReCAPTCHAOptions>>();
        mock.Setup(o => o.Value).Returns(new ReCAPTCHAOptions { ScoreThreshold = threshold, TestEmail = smokeTestEmail });
        return mock.Object;
    }
}