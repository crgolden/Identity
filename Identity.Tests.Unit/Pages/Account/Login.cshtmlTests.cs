namespace Identity.Tests.Unit.Pages.Account;

using CAPTCHA;
using Identity.Pages.Account;
using Infrastructure;
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
public class LoginModelTests
{
    [Fact]
    public async Task OnGetAsync_WithErrorMessage_AddsModelError()
    {
        // Arrange
        var model = CreateModelWithContext();
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
        await model.OnGetAsync("/dashboard");

        // Assert
        Assert.Equal("/dashboard", model.ReturnUrl);
    }

    [Fact]
    public async Task OnGetAsync_WithoutReturnUrl_DefaultsToRoot()
    {
        // Arrange
        var model = CreateModelWithContext();

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
        var model = CreateModelWithContext(scheme);

        // Act
        await model.OnGetAsync();

        // Assert
        var onlyExternalLogin = Assert.Single(model.ExternalLogins);
        Assert.Equal("Google", onlyExternalLogin.Name);
    }

    [Fact]
    public async Task OnPostAsync_PasswordSignIn_Succeeded_ReturnsLocalRedirect()
    {
        // Arrange
        var signInEmail = TestValues.NewEmailAddress();
        var signInPassword = TestValues.NewPassword();
        var signInManagerMock = CreateSignInManagerMock();
        signInManagerMock
            .Setup(s => s.PasswordSignInAsync(signInEmail, signInPassword, false, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        var urlHelperMock = new Mock<IUrlHelper>(MockBehavior.Strict);
        urlHelperMock.Setup(u => u.Content("~/")).Returns("/");
        urlHelperMock.Setup(u => u.IsLocalUrl("/")).Returns(true);

        var model = new LoginModel(signInManagerMock.Object, CreateRecaptchaServiceMock().Object)
        {
            Url = urlHelperMock.Object,
            PageContext = new PageContext(new ActionContext(new DefaultHttpContext(), new RouteData(), new PageActionDescriptor())),
            Input = new LoginModel.InputModel { Email = signInEmail, Password = signInPassword }
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

        var urlHelperMock = new Mock<IUrlHelper>(MockBehavior.Strict);
        urlHelperMock.Setup(u => u.Content("~/")).Returns("/");

        var model = new LoginModel(signInManagerMock.Object, CreateRecaptchaServiceMock().Object)
        {
            Url = urlHelperMock.Object,
            PageContext = new PageContext(new ActionContext(new DefaultHttpContext(), new RouteData(), new PageActionDescriptor())),
            Input = new LoginModel.InputModel { Email = TestValues.NewEmailAddress(), Password = TestValues.NewPassword() }
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

        var urlHelperMock = new Mock<IUrlHelper>(MockBehavior.Strict);
        urlHelperMock.Setup(u => u.Content("~/")).Returns("/");

        var model = new LoginModel(signInManagerMock.Object, CreateRecaptchaServiceMock().Object)
        {
            Url = urlHelperMock.Object,
            PageContext = new PageContext(new ActionContext(new DefaultHttpContext(), new RouteData(), new PageActionDescriptor())),
            Input = new LoginModel.InputModel { Email = TestValues.NewEmailAddress(), Password = TestValues.NewPassword() }
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

        var urlHelperMock = new Mock<IUrlHelper>(MockBehavior.Strict);
        urlHelperMock.Setup(u => u.Content("~/")).Returns("/");

        var model = new LoginModel(signInManagerMock.Object, CreateRecaptchaServiceMock().Object)
        {
            Url = urlHelperMock.Object,
            PageContext = new PageContext(new ActionContext(new DefaultHttpContext(), new RouteData(), new PageActionDescriptor())),
            Input = new LoginModel.InputModel { Email = TestValues.NewEmailAddress(), Password = TestValues.NewPassword() }
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

        var urlHelperMock = new Mock<IUrlHelper>(MockBehavior.Strict);
        urlHelperMock.Setup(u => u.Content("~/")).Returns("/");
        urlHelperMock.Setup(u => u.IsLocalUrl(It.IsAny<string?>())).Returns(true);

        var model = new LoginModel(signInManagerMock.Object, CreateRecaptchaServiceMock().Object)
        {
            Url = urlHelperMock.Object,
            PageContext = new PageContext(new ActionContext(new DefaultHttpContext(), new RouteData(), new PageActionDescriptor())),
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

    [Fact]
    public async Task OnPostAsync_PasskeySignIn_Failed_TurnsAutofillOff()
    {
        // Arrange
        var credentialJson = TestValues.NewPasskeyCredentialJson();
        var signInManagerMock = CreateSignInManagerMock();
        signInManagerMock
            .Setup(s => s.PasskeySignInAsync(credentialJson))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        var urlHelperMock = new Mock<IUrlHelper>(MockBehavior.Strict);
        urlHelperMock.Setup(u => u.Content("~/")).Returns("/");

        var model = new LoginModel(signInManagerMock.Object, CreateRecaptchaServiceMock().Object)
        {
            Url = urlHelperMock.Object,
            PageContext = new PageContext(new ActionContext(new DefaultHttpContext(), new RouteData(), new PageActionDescriptor())),
            Input = new LoginModel.InputModel
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
        urlHelperMock.Setup(u => u.Content("~/")).Returns("/");

        var model = new LoginModel(signInManagerMock.Object, CreateRecaptchaServiceMock().Object)
        {
            Url = urlHelperMock.Object,
            PageContext = new PageContext(new ActionContext(new DefaultHttpContext(), new RouteData(), new PageActionDescriptor())),
            Input = new LoginModel.InputModel
            {
                Email = TestValues.NewEmailAddress(),
                Password = TestValues.NewPassword()
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
        urlHelperMock.Setup(u => u.Content("~/")).Returns("/");

        var model = new LoginModel(signInManagerMock.Object, CreateRecaptchaServiceMock().Object)
        {
            Url = urlHelperMock.Object,
            Input = new LoginModel.InputModel
            {
                Passkey = null,
                Email = TestValues.NewEmailAddress(),
                Password = TestValues.NewPassword(),
                RememberMe = false
            }
        };

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
        var recaptchaServiceMock = CreateRecaptchaServiceMock(passed: false);

        var urlHelperMock = new Mock<IUrlHelper>(MockBehavior.Strict);
        urlHelperMock.Setup(u => u.Content("~/")).Returns("/");

        var model = new LoginModel(signInManagerMock.Object, recaptchaServiceMock.Object)
        {
            Url = urlHelperMock.Object,
            PageContext = new PageContext(new ActionContext(new DefaultHttpContext(), new RouteData(), new PageActionDescriptor())),
            Input = new LoginModel.InputModel { Email = TestValues.NewEmailAddress(), Password = TestValues.NewPassword() }
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

        var recaptchaServiceMock = CreateRecaptchaServiceMock(passed: false);

        var urlHelperMock = new Mock<IUrlHelper>(MockBehavior.Strict);
        urlHelperMock.Setup(u => u.Content("~/")).Returns("/");
        urlHelperMock.Setup(u => u.IsLocalUrl("/")).Returns(true);

        var model = new LoginModel(signInManagerMock.Object, recaptchaServiceMock.Object)
        {
            Url = urlHelperMock.Object,
            PageContext = new PageContext(new ActionContext(new DefaultHttpContext(), new RouteData(), new PageActionDescriptor())),
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
    public async Task OnPostAsync_FailingVerdict_NeverAttemptsSignIn()
    {
        var signInManagerMock = CreateSignInManagerMock();
        var recaptchaServiceMock = CreateRecaptchaServiceMock(passed: false);

        var urlHelperMock = new Mock<IUrlHelper>(MockBehavior.Strict);
        urlHelperMock.Setup(u => u.Content("~/")).Returns("/");

        var model = new LoginModel(signInManagerMock.Object, recaptchaServiceMock.Object)
        {
            Url = urlHelperMock.Object,
            PageContext = new PageContext(new ActionContext(new DefaultHttpContext(), new RouteData(), new PageActionDescriptor())),
            Input = new LoginModel.InputModel { Email = TestValues.NewEmailAddress(), Password = TestValues.NewPassword() }
        };

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        recaptchaServiceMock.Verify(s => s.VerifyAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        signInManagerMock.Verify(s => s.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    private static LoginModel CreateModelWithContext(params AuthenticationScheme[] schemes)
    {
        var userManagerMock = MockHelpers.MockUserManager();
        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);
        signInManagerMock
            .Setup(s => s.GetExternalAuthenticationSchemesAsync())
            .ReturnsAsync(schemes);

        var authServiceMock = new Mock<IAuthenticationService>(MockBehavior.Strict);
        authServiceMock
            .Setup(a => a.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties?>()))
            .Returns(Task.CompletedTask);

        var serviceProvider = new ServiceCollection()
            .AddSingleton(authServiceMock.Object)
            .BuildServiceProvider();

        var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };
        var model = new LoginModel(signInManagerMock.Object, CreateRecaptchaServiceMock().Object);
        model.PageContext = new PageContext(new ActionContext(httpContext, new RouteData(), new PageActionDescriptor()));

        var urlHelperMock = new Mock<IUrlHelper>(MockBehavior.Strict);
        urlHelperMock.Setup(u => u.Content("~/")).Returns("/");
        model.Url = urlHelperMock.Object;

        return model;
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
            ? TestValues.NewScoreAtOrAboveDefaultThreshold()
            : TestValues.NewScoreBelowDefaultThreshold();
        mock.Setup(s => s.VerifyAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CAPTCHAVerdict(passed, score));
        mock.Setup(s => s.SiteKey).Returns(TestValues.NewRecaptchaSiteKey());
        return mock;
    }
}