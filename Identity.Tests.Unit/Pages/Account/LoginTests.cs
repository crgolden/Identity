namespace Identity.Tests.Unit.Pages.Account;

using Identity.CAPTCHA;
using Identity.Pages.Account;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public sealed class LoginTests : IDisposable
{
    private static readonly string RequestedReturnUrl = Generated.NewLocalPath();

    private static readonly string ContentRootUrl = Generated.NewLocalPath();

    private static readonly string ExternalSchemeName = Generated.NewSchemeName();

    private static readonly string PasskeyCredentialJson = Generated.NewPasskeyCredentialJson();

    private readonly TelemetryHarness _harness = new();

    [Fact]
    public async Task OnGetAsync_WithErrorMessage_AddsModelError()
    {
        // Arrange
        var model = CreateModelWithContext();
        model.ErrorMessage = Generated.NewFailureReason();

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
        var model = CreateModelWithContext();

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.True(model.ModelState.IsValid);
    }

    [Fact]
    public async Task OnGetAsync_WithReturnUrl_SetsReturnUrl()
    {
        // Arrange
        var model = CreateModelWithContext();

        // Act
        await model.OnGetAsync(RequestedReturnUrl);

        // Assert
        Assert.Equal(RequestedReturnUrl, model.ReturnUrl);
    }

    [Fact]
    public async Task OnGetAsync_WithoutReturnUrl_DefaultsToRoot()
    {
        // Arrange
        var model = CreateModelWithContext();

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.Equal(ContentRootUrl, model.ReturnUrl);
    }

    [Fact]
    public async Task OnGetAsync_SignsOutOfTheExternalScheme()
    {
        // Arrange
        var authServiceMock = CreateSignOutAcceptingAuthService();
        var model = CreateModelWithContext(authServiceMock, CreateRecaptchaServiceMock());

        // Act
        await model.OnGetAsync();

        // Assert
        authServiceMock.Verify(a => a.SignOutAsync(It.IsAny<HttpContext>(), IdentityConstants.ExternalScheme, It.IsAny<AuthenticationProperties?>()), Times.Once);
    }

    [Fact]
    public async Task OnGetAsync_ExposesTheCaptchaSiteKeyAndScriptEndpoint()
    {
        // Arrange
        var captchaMock = CreateRecaptchaServiceMock();
        var siteKey = Generated.NewRecaptchaSiteKey();
        var scriptEndpoint = Generated.NewProviderBaseAddress();
        captchaMock.Setup(s => s.SiteKey).Returns(siteKey);
        captchaMock.Setup(s => s.ScriptEndpoint).Returns(scriptEndpoint);
        var model = CreateModelWithContext(CreateSignOutAcceptingAuthService(), captchaMock);

        // Act
        await model.OnGetAsync();

        // Assert
        Assert.Equal(siteKey, model.RecaptchaSiteKey);
        Assert.Equal(scriptEndpoint, model.RecaptchaScriptEndpoint);
    }

    [Fact]
    public async Task OnGetAsync_ExternalSchemesAvailable_PopulatesExternalLogins()
    {
        // Arrange
        var scheme = new AuthenticationScheme(ExternalSchemeName, Generated.NewDisplayName(), typeof(IAuthenticationHandler));
        var model = CreateModelWithContext(scheme);

        // Act
        await model.OnGetAsync();

        // Assert
        var onlyExternalLogin = Assert.Single(model.ExternalLogins);
        Assert.Equal(ExternalSchemeName, onlyExternalLogin.Name);
    }

    [Fact]
    public async Task OnPostAsync_PasswordSignIn_Succeeded_ReturnsLocalRedirect()
    {
        // Arrange
        var signInEmail = Generated.NewEmailAddress();
        var signInPassword = Generated.NewPassword();
        var signInManagerMock = CreateSignInManagerMock();
        signInManagerMock
            .Setup(s => s.PasswordSignInAsync(signInEmail, signInPassword, false, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        var urlHelperMock = new Mock<IUrlHelper>(MockBehavior.Strict);
        urlHelperMock.Setup(u => u.Content(PageRoutes.ContentRoot)).Returns(ContentRootUrl);
        urlHelperMock.Setup(u => u.IsLocalUrl(ContentRootUrl)).Returns(true);

        var model = new Login(signInManagerMock.Object, CreateRecaptchaServiceMock().Object, _harness.Telemetry)
        {
            Url = urlHelperMock.Object,
            PageContext = new PageContext(new ActionContext(new DefaultHttpContext(), new RouteData(), new PageActionDescriptor())),
            Input = new Login.InputModel { Email = signInEmail, Password = signInPassword }
        };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var redirect = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal(ContentRootUrl, redirect.Url);
    }

    [Fact]
    public async Task OnPostAsync_PasswordSignIn_RequiresTwoFactor_RedirectsToLoginWith2fa()
    {
        // Arrange
        var signInManagerMock = CreateSignInManagerMock();
        signInManagerMock
            .Setup(s => s.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.TwoFactorRequired);

        var urlHelperMock = new Mock<IUrlHelper>(MockBehavior.Strict);
        urlHelperMock.Setup(u => u.Content(PageRoutes.ContentRoot)).Returns(ContentRootUrl);

        var model = new Login(signInManagerMock.Object, CreateRecaptchaServiceMock().Object, _harness.Telemetry)
        {
            Url = urlHelperMock.Object,
            PageContext = new PageContext(new ActionContext(new DefaultHttpContext(), new RouteData(), new PageActionDescriptor())),
            Input = new Login.InputModel { Email = Generated.NewEmailAddress(), Password = Generated.NewPassword() }
        };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(Login.LoginWith2faPageName, redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_PasswordSignIn_IsLockedOut_RedirectsToLockout()
    {
        // Arrange
        var signInManagerMock = CreateSignInManagerMock();
        signInManagerMock
            .Setup(s => s.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);

        var urlHelperMock = new Mock<IUrlHelper>(MockBehavior.Strict);
        urlHelperMock.Setup(u => u.Content(PageRoutes.ContentRoot)).Returns(ContentRootUrl);

        var model = new Login(signInManagerMock.Object, CreateRecaptchaServiceMock().Object, _harness.Telemetry)
        {
            Url = urlHelperMock.Object,
            PageContext = new PageContext(new ActionContext(new DefaultHttpContext(), new RouteData(), new PageActionDescriptor())),
            Input = new Login.InputModel { Email = Generated.NewEmailAddress(), Password = Generated.NewPassword() }
        };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(PageRoutes.SiblingLockout, redirect.PageName);
    }

    [Fact]
    public async Task OnPostAsync_PasswordSignIn_Failed_ReturnsPageWithModelError()
    {
        // Arrange
        var signInManagerMock = CreateSignInManagerMock();
        signInManagerMock
            .Setup(s => s.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        var urlHelperMock = new Mock<IUrlHelper>(MockBehavior.Strict);
        urlHelperMock.Setup(u => u.Content(PageRoutes.ContentRoot)).Returns(ContentRootUrl);

        var model = new Login(signInManagerMock.Object, CreateRecaptchaServiceMock().Object, _harness.Telemetry)
        {
            Url = urlHelperMock.Object,
            PageContext = new PageContext(new ActionContext(new DefaultHttpContext(), new RouteData(), new PageActionDescriptor())),
            Input = new Login.InputModel { Email = Generated.NewEmailAddress(), Password = Generated.NewPassword() }
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
            .Setup(s => s.PasskeySignInAsync(PasskeyCredentialJson))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        var urlHelperMock = new Mock<IUrlHelper>(MockBehavior.Strict);
        urlHelperMock.Setup(u => u.Content(PageRoutes.ContentRoot)).Returns(ContentRootUrl);
        urlHelperMock.Setup(u => u.IsLocalUrl(It.IsAny<string?>())).Returns(true);

        var model = new Login(signInManagerMock.Object, CreateRecaptchaServiceMock().Object, _harness.Telemetry)
        {
            Url = urlHelperMock.Object,
            PageContext = new PageContext(new ActionContext(new DefaultHttpContext(), new RouteData(), new PageActionDescriptor())),
            Input = new Login.InputModel
            {
                Passkey = new Identity.Pages.Account.Manage.PasskeyInputModel { CredentialJson = PasskeyCredentialJson }
            }
        };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.IsType<LocalRedirectResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_PasskeySignIn_Failed_TurnsAutofillOff()
    {
        // Arrange
        var credentialJson = Generated.NewPasskeyCredentialJson();
        var signInManagerMock = CreateSignInManagerMock();
        signInManagerMock
            .Setup(s => s.PasskeySignInAsync(credentialJson))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        var urlHelperMock = new Mock<IUrlHelper>(MockBehavior.Strict);
        urlHelperMock.Setup(u => u.Content(PageRoutes.ContentRoot)).Returns(ContentRootUrl);

        var model = new Login(signInManagerMock.Object, CreateRecaptchaServiceMock().Object, _harness.Telemetry)
        {
            Url = urlHelperMock.Object,
            PageContext = new PageContext(new ActionContext(new DefaultHttpContext(), new RouteData(), new PageActionDescriptor())),
            Input = new Login.InputModel
            {
                Passkey = new Identity.Pages.Account.Manage.PasskeyInputModel { CredentialJson = credentialJson }
            }
        };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.False(model.PasskeyAutofillAllowed);
    }

    [Fact]
    public async Task OnPostAsync_PasswordSignIn_Failed_LeavesAutofillOn()
    {
        // Arrange
        var signInManagerMock = CreateSignInManagerMock();
        signInManagerMock
            .Setup(s => s.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        var urlHelperMock = new Mock<IUrlHelper>(MockBehavior.Strict);
        urlHelperMock.Setup(u => u.Content(PageRoutes.ContentRoot)).Returns(ContentRootUrl);

        var model = new Login(signInManagerMock.Object, CreateRecaptchaServiceMock().Object, _harness.Telemetry)
        {
            Url = urlHelperMock.Object,
            PageContext = new PageContext(new ActionContext(new DefaultHttpContext(), new RouteData(), new PageActionDescriptor())),
            Input = new Login.InputModel
            {
                Email = Generated.NewEmailAddress(),
                Password = Generated.NewPassword()
            }
        };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.True(model.PasskeyAutofillAllowed);
    }

    [Fact]
    public async Task OnPostAsync_InvalidModelState_ReturnsPageWithoutSignIn()
    {
        // Arrange
        var userManagerMock = MockHelpers.MockUserManager();
        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);

        signInManagerMock.Setup(s => s.GetExternalAuthenticationSchemesAsync())
            .ReturnsAsync([]);

        var urlHelperMock = new Mock<IUrlHelper>(MockBehavior.Strict);
        urlHelperMock.Setup(u => u.Content(PageRoutes.ContentRoot)).Returns(ContentRootUrl);

        var model = new Login(signInManagerMock.Object, CreateRecaptchaServiceMock().Object, _harness.Telemetry)
        {
            Url = urlHelperMock.Object,
            Input = new Login.InputModel
            {
                Email = Generated.NewEmailAddress(),
                Password = Generated.NewPassword()
            }
        };

        model.ModelState.AddModelError(Generated.NewModelStateKey(), Generated.NewValidationMessage());

        // Act
        var result = await model.OnPostAsync(returnUrl: null);

        // Assert
        Assert.IsType<PageResult>(result);
        signInManagerMock.Verify(s => s.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_RecaptchaScoreBelowThreshold_ReturnsPageWithModelError()
    {
        // Arrange
        var signInManagerMock = CreateSignInManagerMock();
        var recaptchaServiceMock = CreateRecaptchaServiceMock(passed: false);

        var urlHelperMock = new Mock<IUrlHelper>(MockBehavior.Strict);
        urlHelperMock.Setup(u => u.Content(PageRoutes.ContentRoot)).Returns(ContentRootUrl);

        var model = new Login(signInManagerMock.Object, recaptchaServiceMock.Object, _harness.Telemetry)
        {
            Url = urlHelperMock.Object,
            PageContext = new PageContext(new ActionContext(new DefaultHttpContext(), new RouteData(), new PageActionDescriptor())),
            Input = new Login.InputModel { Email = Generated.NewEmailAddress(), Password = Generated.NewPassword() }
        };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        Assert.True(model.ModelState.ContainsKey(string.Empty));
        signInManagerMock.Verify(s => s.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_PasskeyPath_SkipsRecaptcha()
    {
        // Arrange
        var signInManagerMock = CreateSignInManagerMock();
        signInManagerMock
            .Setup(s => s.PasskeySignInAsync(It.IsAny<string>()))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        var recaptchaServiceMock = CreateRecaptchaServiceMock(passed: false);

        var urlHelperMock = new Mock<IUrlHelper>(MockBehavior.Strict);
        urlHelperMock.Setup(u => u.Content(PageRoutes.ContentRoot)).Returns(ContentRootUrl);
        urlHelperMock.Setup(u => u.IsLocalUrl(ContentRootUrl)).Returns(true);

        var model = new Login(signInManagerMock.Object, recaptchaServiceMock.Object, _harness.Telemetry)
        {
            Url = urlHelperMock.Object,
            PageContext = new PageContext(new ActionContext(new DefaultHttpContext(), new RouteData(), new PageActionDescriptor())),
            Input = new Login.InputModel
            {
                Passkey = new Identity.Pages.Account.Manage.PasskeyInputModel { CredentialJson = PasskeyCredentialJson }
            }
        };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.IsType<LocalRedirectResult>(result);
        recaptchaServiceMock.Verify(s => s.VerifyAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_FailingVerdict_NeverAttemptsSignIn()
    {
        // Arrange
        var signInManagerMock = CreateSignInManagerMock();
        var recaptchaServiceMock = CreateRecaptchaServiceMock(passed: false);

        var urlHelperMock = new Mock<IUrlHelper>(MockBehavior.Strict);
        urlHelperMock.Setup(u => u.Content(PageRoutes.ContentRoot)).Returns(ContentRootUrl);

        var model = new Login(signInManagerMock.Object, recaptchaServiceMock.Object, _harness.Telemetry)
        {
            Url = urlHelperMock.Object,
            PageContext = new PageContext(new ActionContext(new DefaultHttpContext(), new RouteData(), new PageActionDescriptor())),
            Input = new Login.InputModel { Email = Generated.NewEmailAddress(), Password = Generated.NewPassword() }
        };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        recaptchaServiceMock.Verify(s => s.VerifyAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        signInManagerMock.Verify(s => s.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_PasswordSignIn_Succeeded_RedirectsToTheRequestedLocalReturnUrl()
    {
        // Arrange
        var signInManagerMock = SignInManagerWithPasswordResult(Microsoft.AspNetCore.Identity.SignInResult.Success);
        var urlHelperMock = new Mock<IUrlHelper>(MockBehavior.Strict);
        urlHelperMock.Setup(u => u.IsLocalUrl(RequestedReturnUrl)).Returns(true);
        var model = CreatePostModel(signInManagerMock, urlHelperMock, PasswordInput());

        // Act
        var result = await model.OnPostAsync(RequestedReturnUrl);

        // Assert
        Assert.Equal(RequestedReturnUrl, Assert.IsType<LocalRedirectResult>(result).Url);
    }

    [Fact]
    public async Task OnPostAsync_PasswordSignIn_Succeeded_NonLocalReturnUrl_RedirectsToTheContentRoot()
    {
        // Arrange
        var nonLocalReturnUrl = Generated.NewOrigin();
        var signInManagerMock = SignInManagerWithPasswordResult(Microsoft.AspNetCore.Identity.SignInResult.Success);
        var urlHelperMock = new Mock<IUrlHelper>(MockBehavior.Strict);
        urlHelperMock.Setup(u => u.IsLocalUrl(nonLocalReturnUrl)).Returns(false);
        var model = CreatePostModel(signInManagerMock, urlHelperMock, PasswordInput());

        // Act
        var result = await model.OnPostAsync(nonLocalReturnUrl);

        // Assert
        Assert.Equal(PageRoutes.ContentRoot, Assert.IsType<LocalRedirectResult>(result).Url);
    }

    [Fact]
    public async Task OnPostAsync_PasswordSignIn_TagsTheSpanWithLockoutAndTwoFactor()
    {
        // Arrange
        var signInManagerMock = SignInManagerWithPasswordResult(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);
        var urlHelperMock = new Mock<IUrlHelper>(MockBehavior.Strict);
        urlHelperMock.Setup(u => u.Content(PageRoutes.ContentRoot)).Returns(ContentRootUrl);
        var model = CreatePostModel(signInManagerMock, urlHelperMock, PasswordInput());

        // Act
        await model.OnPostAsync();

        // Assert
        var span = Assert.Single(_harness.StoppedActivities, a => string.Equals(a.OperationName, Login.PasswordActivityName, StringComparison.Ordinal));
        Assert.True(Assert.IsType<bool>(span.GetTagItem(Login.LockedOutTagName)));
        Assert.False(Assert.IsType<bool>(span.GetTagItem(Login.RequiresTwoFactorTagName)));
    }

    [Fact]
    public async Task OnPostAsync_PasswordSignIn_RequiresTwoFactor_TagsTheSpan()
    {
        // Arrange
        var signInManagerMock = SignInManagerWithPasswordResult(Microsoft.AspNetCore.Identity.SignInResult.TwoFactorRequired);
        var urlHelperMock = new Mock<IUrlHelper>(MockBehavior.Strict);
        urlHelperMock.Setup(u => u.Content(PageRoutes.ContentRoot)).Returns(ContentRootUrl);
        var model = CreatePostModel(signInManagerMock, urlHelperMock, PasswordInput());

        // Act
        await model.OnPostAsync();

        // Assert
        var span = Assert.Single(_harness.StoppedActivities, a => string.Equals(a.OperationName, Login.PasswordActivityName, StringComparison.Ordinal));
        Assert.True(Assert.IsType<bool>(span.GetTagItem(Login.RequiresTwoFactorTagName)));
        Assert.False(Assert.IsType<bool>(span.GetTagItem(Login.LockedOutTagName)));
    }

    [Fact]
    public async Task OnPostAsync_PasskeySignIn_ClearsTheFormErrorsOfTheUnusedPasswordFields()
    {
        // Arrange
        var signInManagerMock = CreateSignInManagerMock();
        signInManagerMock.Setup(s => s.PasskeySignInAsync(PasskeyCredentialJson)).ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);
        var urlHelperMock = new Mock<IUrlHelper>(MockBehavior.Strict);
        urlHelperMock.Setup(u => u.Content(PageRoutes.ContentRoot)).Returns(ContentRootUrl);
        var model = CreatePostModel(signInManagerMock, urlHelperMock, PasskeyInput());
        var staleKey = Generated.NewModelStateKey();
        model.ModelState.AddModelError(staleKey, Generated.NewValidationMessage());

        // Act
        await model.OnPostAsync();

        // Assert
        Assert.False(model.ModelState.ContainsKey(staleKey));
    }

    [Fact]
    public async Task OnPostAsync_PasskeySignIn_TagsTheSpanAndCountsTheAttempt()
    {
        // Arrange
        var signInManagerMock = CreateSignInManagerMock();
        signInManagerMock.Setup(s => s.PasskeySignInAsync(PasskeyCredentialJson)).ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
        var urlHelperMock = new Mock<IUrlHelper>(MockBehavior.Strict);
        urlHelperMock.Setup(u => u.Content(PageRoutes.ContentRoot)).Returns(ContentRootUrl);
        urlHelperMock.Setup(u => u.IsLocalUrl(ContentRootUrl)).Returns(true);
        var model = CreatePostModel(signInManagerMock, urlHelperMock, PasskeyInput());
        var measurements = new List<long>();
        using var listener = _harness.ListenTo(Telemetry.Metrics.PasskeySignInCounterName, (value, _) => measurements.Add(value));

        // Act
        await model.OnPostAsync();

        // Assert
        var span = Assert.Single(_harness.StoppedActivities, a => string.Equals(a.OperationName, Login.PasskeyActivityName, StringComparison.Ordinal));
        Assert.True(Assert.IsType<bool>(span.GetTagItem(Telemetry.Metrics.SucceededTagName)));
        Assert.Single(measurements);
    }

    public void Dispose() => _harness.Dispose();

    private static Login.InputModel PasswordInput() =>
        new() { Email = Generated.NewEmailAddress(), Password = Generated.NewPassword() };

    private static Login.InputModel PasskeyInput() =>
        new() { Passkey = new Identity.Pages.Account.Manage.PasskeyInputModel { CredentialJson = PasskeyCredentialJson } };

    private static Mock<SignInManager<IdentityUser<Guid>>> SignInManagerWithPasswordResult(Microsoft.AspNetCore.Identity.SignInResult result)
    {
        var signInManagerMock = CreateSignInManagerMock();
        signInManagerMock
            .Setup(s => s.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(result);
        return signInManagerMock;
    }

    private static Mock<SignInManager<IdentityUser<Guid>>> CreateSignInManagerMock()
    {
        var userManagerMock = MockHelpers.MockUserManager();
        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);
        signInManagerMock
            .Setup(s => s.GetExternalAuthenticationSchemesAsync())
            .ReturnsAsync([]);
        return signInManagerMock;
    }

    private static Mock<ICAPTCHAService> CreateRecaptchaServiceMock(bool passed = true)
    {
        var mock = new Mock<ICAPTCHAService>(MockBehavior.Strict);
        var score = passed
            ? Generated.NewScoreAtOrAbove(ReCAPTCHAOptions.DefaultScoreThreshold)
            : Generated.NewScoreBelow(ReCAPTCHAOptions.DefaultScoreThreshold);
        mock.Setup(s => s.VerifyAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CAPTCHAVerdict(passed, score));
        mock.Setup(s => s.SiteKey).Returns(Generated.NewRecaptchaSiteKey());
        mock.Setup(s => s.ScriptEndpoint).Returns(Generated.NewProviderBaseAddress());
        return mock;
    }

    private static Mock<IAuthenticationService> CreateSignOutAcceptingAuthService()
    {
        var authServiceMock = new Mock<IAuthenticationService>(MockBehavior.Strict);
        authServiceMock
            .Setup(a => a.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties?>()))
            .Returns(Task.CompletedTask);
        return authServiceMock;
    }

    private Login CreatePostModel(Mock<SignInManager<IdentityUser<Guid>>> signInManagerMock, Mock<IUrlHelper> urlHelperMock, Login.InputModel input) =>
        new(signInManagerMock.Object, CreateRecaptchaServiceMock().Object, _harness.Telemetry)
        {
            Url = urlHelperMock.Object,
            PageContext = new PageContext(new ActionContext(new DefaultHttpContext(), new RouteData(), new PageActionDescriptor())),
            Input = input,
        };

    private Login CreateModelWithContext(params AuthenticationScheme[] schemes) =>
        CreateModelWithContext(CreateSignOutAcceptingAuthService(), CreateRecaptchaServiceMock(), schemes);

    private Login CreateModelWithContext(
        Mock<IAuthenticationService> authServiceMock,
        Mock<ICAPTCHAService> captchaMock,
        params AuthenticationScheme[] schemes)
    {
        var userManagerMock = MockHelpers.MockUserManager();
        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);
        signInManagerMock
            .Setup(s => s.GetExternalAuthenticationSchemesAsync())
            .ReturnsAsync(schemes);

        var serviceProvider = new ServiceCollection()
            .AddSingleton(authServiceMock.Object)
            .BuildServiceProvider();

        var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };
        var model = new Login(signInManagerMock.Object, captchaMock.Object, _harness.Telemetry);
        model.PageContext = new PageContext(new ActionContext(httpContext, new RouteData(), new PageActionDescriptor()));

        var urlHelperMock = new Mock<IUrlHelper>(MockBehavior.Strict);
        urlHelperMock.Setup(u => u.Content(PageRoutes.ContentRoot)).Returns(ContentRootUrl);
        model.Url = urlHelperMock.Object;

        return model;
    }
}
