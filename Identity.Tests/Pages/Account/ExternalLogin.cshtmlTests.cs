#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
namespace Identity.Tests.Pages.Account;

using System.Security.Claims;
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
using Microsoft.Extensions.Options;
using Moq;

/// <summary>
/// Tests for ExternalLoginModel.OnGetCallbackAsync covering success, error and edge cases
/// involving external login info, sign-in results and email claim presence.
/// </summary>
[Trait("Category", "Unit")]
public class ExternalLoginModelTests
{
    /// <summary>
    /// Helper to create a configured ExternalLoginModel instance along with commonly needed mocks.
    /// Ensures UserManager.SupportsUserEmail returns true and the provided external sign-in manager is used.
    /// </summary>
    private static (ExternalLoginModel model, Mock<SignInManager<IdentityUser<Guid>>> signInManagerMock, Mock<UserManager<IdentityUser<Guid>>> userManagerMock) CreateModelWithMocks()
    {
        // IUserEmailStore needed as the constructor calls GetEmailStore which casts userStore to IUserEmailStore
        var userEmailStoreMock = new Mock<IUserEmailStore<IdentityUser<Guid>>>();
        var userStore = userEmailStoreMock.As<IUserStore<IdentityUser<Guid>>>();

        // UserManager dependencies
        var userValidators = new List<IUserValidator<IdentityUser<Guid>>>();
        var pwdValidators = new List<IPasswordValidator<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(
            userStore.Object,
            new Mock<IOptions<IdentityOptions>>().Object,
            new Mock<IPasswordHasher<IdentityUser<Guid>>>().Object,
            userValidators,
            pwdValidators,
            new Mock<ILookupNormalizer>().Object,
            new IdentityErrorDescriber(),
            new Mock<IServiceProvider>().Object,
            new Mock<ILogger<UserManager<IdentityUser<Guid>>>>().Object);

        userManagerMock.SetupGet(um => um.SupportsUserEmail).Returns(true);

        // SignInManager dependencies
        var httpContextAccessor = new Mock<IHttpContextAccessor>().Object;
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>().Object;
        var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(
            userManagerMock.Object,
            httpContextAccessor,
            claimsFactory,
            new Mock<IOptions<IdentityOptions>>().Object,
            new Mock<ILogger<SignInManager<IdentityUser<Guid>>>>().Object,
            new Mock<IAuthenticationSchemeProvider>().Object,
            new Mock<IUserConfirmation<IdentityUser<Guid>>>().Object);

        // ExternalLoginModel dependencies
        var loggerMock = new Mock<ILogger<ExternalLoginModel>>();
        var emailSenderMock = new Mock<IEmailSender>();

        var model = new ExternalLoginModel(
            signInManagerMock.Object,
            userManagerMock.Object,
            userStore.Object,
            loggerMock.Object,
            emailSenderMock.Object);

        // Provide Url helper so Url.Content("~/") works
        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock.Setup(u => u.Content("~/")).Returns("/");
        model.Url = urlHelperMock.Object;

        return (model, signInManagerMock, userManagerMock);
    }

    /// <summary>
    /// Test that when remoteError is non-null the method sets ErrorMessage appropriately and redirects to the Login page.
    /// Inputs: returnUrl can be null or a non-empty string. remoteError is non-null.
    /// Expectation: ErrorMessage contains the remoteError and RedirectToPage("./Login") is returned with correct ReturnUrl value.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("/some/return")]
    public async Task OnGetCallbackAsync_RemoteErrorProvided_SetsErrorMessageAndRedirectsToLogin(string? returnUrl)
    {
        // Arrange
        var (model, signInManagerMock, _) = CreateModelWithMocks();
        var remoteError = "provider failure";

        // Act
        var result = await model.OnGetCallbackAsync(returnUrl, remoteError);

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./Login", redirect.PageName);
        // Check route values contains ReturnUrl as resolved by Url.Content when returnUrl is null, otherwise same string
        var expectedReturn = returnUrl ?? "/";
        Assert.NotNull(redirect.RouteValues);
        Assert.True(redirect.RouteValues.ContainsKey("ReturnUrl"));
        Assert.Equal(expectedReturn, redirect.RouteValues["ReturnUrl"]);
        Assert.NotNull(model.ErrorMessage);
        Assert.Contains(remoteError, model.ErrorMessage);
        // Ensure no external login info was requested in this branch
        signInManagerMock.Verify(s => s.GetExternalLoginInfoAsync(), Times.Never);
    }

    /// <summary>
    /// Test that when external login info cannot be loaded (info == null) the method sets an error and redirects to Login.
    /// Input: returnUrl tested for null and non-null.
    /// Expectation: ErrorMessage is set to indicate loading error and RedirectToPage("./Login") is returned.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("/return/here")]
    public async Task OnGetCallbackAsync_InfoIsNull_SetsErrorMessageAndRedirectsToLogin(string? returnUrl)
    {
        // Arrange
        var (model, signInManagerMock, _) = CreateModelWithMocks();
        signInManagerMock.Setup(s => s.GetExternalLoginInfoAsync()).ReturnsAsync((ExternalLoginInfo?)null);

        // Act
        var result = await model.OnGetCallbackAsync(returnUrl, null);

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./Login", redirect.PageName);
        var expectedReturn = returnUrl ?? "/";
        Assert.True(redirect.RouteValues?.ContainsKey("ReturnUrl"));
        Assert.Equal(expectedReturn, redirect.RouteValues!["ReturnUrl"]);
        Assert.Equal("Error loading external login information.", model.ErrorMessage);
        signInManagerMock.Verify(s => s.GetExternalLoginInfoAsync(), Times.Once);
    }

    /// <summary>
    /// Verifies that OnGet returns a RedirectToPageResult pointing to the expected Login page.
    /// Input conditions: an ExternalLoginModel constructed with mocked dependencies that satisfy constructor expectations.
    /// Expected result: a RedirectToPageResult whose PageName equals "./Login".
    /// </summary>
    [Fact]
    public void OnGet_NoParameters_ReturnsRedirectToLoginPage()
    {
        // Arrange
        // Create an IUserEmailStore mock and pass it as the IUserStore to ensure any cast in GetEmailStore succeeds.
        var userEmailStoreMock = new Mock<IUserEmailStore<IdentityUser<Guid>>>();
        var userStoreAsUserStore = (IUserStore<IdentityUser<Guid>>)userEmailStoreMock.Object;

        // UserManager requires an IUserStore and other constructor args; pass null for optional services.
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(userStoreAsUserStore, null, null, null, null, null, null, null, null);
        userManagerMock.Setup(m => m.SupportsUserEmail).Returns(true);

        // Create supporting mocks for SignInManager constructor arguments.
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        var claimsFactoryMock = new Mock<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>();
        var loggerForSignInMock = new Mock<ILogger<SignInManager<IdentityUser<Guid>>>>();
        var schemesMock = new Mock<IAuthenticationSchemeProvider>();
        var userConfirmationMock = new Mock<IUserConfirmation<IdentityUser<Guid>>>();

        var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(
            userManagerMock.Object,
            httpContextAccessorMock.Object,
            claimsFactoryMock.Object,
            null,
            loggerForSignInMock.Object,
            schemesMock.Object,
            userConfirmationMock.Object);

        var loggerMock = new Mock<ILogger<ExternalLoginModel>>();
        var emailSenderMock = new Mock<IEmailSender>();

        var model = new ExternalLoginModel(
            signInManagerMock.Object,
            userManagerMock.Object,
            userStoreAsUserStore,
            loggerMock.Object,
            emailSenderMock.Object);

        // Act
        var result = model.OnGet();

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./Login", redirect.PageName);
    }

    /// <summary>
    /// Test that when ExternalLoginInfo is null, the method sets ErrorMessage and redirects to the Login page.
    /// Input conditions: returnUrl is null (causing Url.Content("~/") to be used) and SignInManager.GetExternalLoginInfoAsync returns null.
    /// Expected result: RedirectToPageResult to "./Login" with route value ReturnUrl equal to Url.Content("~/"), and ErrorMessage set.
    /// </summary>
    [Fact]
    public async Task OnPostConfirmationAsync_InfoNull_ReturnsRedirectToLoginAndSetsErrorMessage()
    {
        // Arrange
        var userStoreMock = new Mock<IUserEmailStore<IdentityUser<Guid>>>(MockBehavior.Strict);
        var options = Options.Create(new IdentityOptions());
        var pwdHasherMock = new Mock<IPasswordHasher<IdentityUser<Guid>>>();
        var userValidators = Enumerable.Empty<IUserValidator<IdentityUser<Guid>>>();
        var pwdValidators = Enumerable.Empty<IPasswordValidator<IdentityUser<Guid>>>();
        var keyNormalizerMock = new Mock<ILookupNormalizer>();
        var errors = new IdentityErrorDescriber();
        var serviceProviderMock = new Mock<IServiceProvider>();
        var loggerUserManagerMock = new Mock<ILogger<UserManager<IdentityUser<Guid>>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(
            userStoreMock.Object, options, pwdHasherMock.Object, userValidators,
            pwdValidators, keyNormalizerMock.Object, errors, serviceProviderMock.Object, loggerUserManagerMock.Object);

        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        var claimsFactoryMock = new Mock<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>();
        var loggerSignInMock = new Mock<ILogger<SignInManager<IdentityUser<Guid>>>>();
        var authSchemeProviderMock = new Mock<IAuthenticationSchemeProvider>();
        var userConfirmationMock = new Mock<IUserConfirmation<IdentityUser<Guid>>>();
        var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(
            userManagerMock.Object, httpContextAccessorMock.Object, claimsFactoryMock.Object,
            options, loggerSignInMock.Object, authSchemeProviderMock.Object, userConfirmationMock.Object);

        // Ensure GetExternalLoginInfoAsync returns null
        signInManagerMock.Setup(s => s.GetExternalLoginInfoAsync(It.IsAny<string?>()))
            .ReturnsAsync((ExternalLoginInfo?)null);

        var emailSenderMock = new Mock<IEmailSender>();
        var loggerMock = new Mock<ILogger<ExternalLoginModel>>();

        userManagerMock.SetupGet(um => um.SupportsUserEmail).Returns(true);

        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock.Setup(u => u.Content("~/")).Returns("/");

        var model = new ExternalLoginModel(signInManagerMock.Object, userManagerMock.Object, userStoreMock.Object, loggerMock.Object, emailSenderMock.Object)
        {
            Url = urlHelperMock.Object,
            PageContext = new PageContext { HttpContext = new DefaultHttpContext() }
        };

        // Act
        var result = await model.OnPostConfirmationAsync(returnUrl: null);

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./Login", redirect.PageName);
        Assert.True(redirect.RouteValues?.ContainsKey("ReturnUrl"));
        Assert.Equal("/", redirect.RouteValues!["ReturnUrl"]);
        Assert.Equal("Error loading external login information during confirmation.", model.ErrorMessage);
    }

    /// <summary>
    /// When ModelState is invalid, but external login info is present, provider display name and return url should be set and the page returned.
    /// Input conditions: ModelState invalid, ExternalLoginInfo available with ProviderDisplayName.
    /// Expected result: PageResult returned, ProviderDisplayName and ReturnUrl set from info and parameter.
    /// </summary>
    [Fact]
    public async Task OnPostConfirmationAsync_ModelStateInvalid_ReturnsPageAndSetsProviderDisplayNameAndReturnUrl()
    {
        // Arrange
        var userStoreMock = new Mock<IUserEmailStore<IdentityUser<Guid>>>(MockBehavior.Strict);
        var options = Options.Create(new IdentityOptions());
        var pwdHasherMock = new Mock<IPasswordHasher<IdentityUser<Guid>>>();
        var userValidators = Enumerable.Empty<IUserValidator<IdentityUser<Guid>>>();
        var pwdValidators = Enumerable.Empty<IPasswordValidator<IdentityUser<Guid>>>();
        var keyNormalizerMock = new Mock<ILookupNormalizer>();
        var errors = new IdentityErrorDescriber();
        var serviceProviderMock = new Mock<IServiceProvider>();
        var loggerUserManagerMock = new Mock<ILogger<UserManager<IdentityUser<Guid>>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(
            userStoreMock.Object, options, pwdHasherMock.Object, userValidators,
            pwdValidators, keyNormalizerMock.Object, errors, serviceProviderMock.Object, loggerUserManagerMock.Object);

        // Ensure the mocked UserManager reports that it supports email, so GetEmailStore() succeeds.
        userManagerMock.Setup(u => u.SupportsUserEmail).Returns(true);

        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        var claimsFactoryMock = new Mock<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>();
        var loggerSignInMock = new Mock<ILogger<SignInManager<IdentityUser<Guid>>>>();
        var authSchemeProviderMock = new Mock<IAuthenticationSchemeProvider>();
        var userConfirmationMock = new Mock<IUserConfirmation<IdentityUser<Guid>>>();
        var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(
            userManagerMock.Object, httpContextAccessorMock.Object, claimsFactoryMock.Object,
            options, loggerSignInMock.Object, authSchemeProviderMock.Object, userConfirmationMock.Object);

        // Create an ExternalLoginInfo with a principal and provider display name
        var claims = new[] { new Claim(ClaimTypes.Email, "x@y.com") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        var info = new ExternalLoginInfo(principal, "TestProvider", "provkey", "TestProviderDisplay");

        signInManagerMock.Setup(s => s.GetExternalLoginInfoAsync(It.IsAny<string?>()))
            .ReturnsAsync(info);

        var emailSenderMock = new Mock<IEmailSender>();
        var loggerMock = new Mock<ILogger<ExternalLoginModel>>();

        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock.Setup(u => u.Content("~/")).Returns("/");

        var model = new ExternalLoginModel(signInManagerMock.Object, userManagerMock.Object, userStoreMock.Object, loggerMock.Object, emailSenderMock.Object)
        {
            Url = urlHelperMock.Object,
            PageContext = new PageContext { HttpContext = new DefaultHttpContext() }
        };

        // Make model state invalid
        model.ModelState.AddModelError("Test", "error");

        // Act
        var result = await model.OnPostConfirmationAsync(returnUrl: "/return");

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal("TestProviderDisplay", model.ProviderDisplayName);
        Assert.Equal("/return", model.ReturnUrl);
    }

    /// <summary>
    /// Parameterized test for successful CreateAsync + AddLoginAsync flows.
    /// Input conditions: CreateAsync and AddLoginAsync succeed; tests both cases where RequireConfirmedAccount is true and false.
    /// Expected result:
    /// - If RequireConfirmedAccount is true: RedirectToPageResult to "./RegisterConfirmation" with route value Email.
    /// - If false: LocalRedirectResult to the provided returnUrl.
    /// Also verifies that an email is sent and SignInAsync is invoked only when RequireConfirmedAccount is false.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task OnPostConfirmationAsync_CreateAndAddLogin_Succeeds_ConditionalRedirect(bool requireConfirmedAccount)
    {
        // Arrange
        var userStoreMock = new Mock<IUserEmailStore<IdentityUser<Guid>>>();
        userStoreMock.Setup(u => u.SetUserNameAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        userStoreMock.Setup(u => u.SetEmailAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var identityOptions = new IdentityOptions();
        identityOptions.SignIn.RequireConfirmedAccount = requireConfirmedAccount;
        var options = Options.Create(identityOptions);

        var pwdHasherMock = new Mock<IPasswordHasher<IdentityUser<Guid>>>();
        var userValidators = Enumerable.Empty<IUserValidator<IdentityUser<Guid>>>();
        var pwdValidators = Enumerable.Empty<IPasswordValidator<IdentityUser<Guid>>>();
        var keyNormalizerMock = new Mock<ILookupNormalizer>();
        var errors = new IdentityErrorDescriber();
        var serviceProviderMock = new Mock<IServiceProvider>();
        var loggerUserManagerMock = new Mock<ILogger<UserManager<IdentityUser<Guid>>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(
            userStoreMock.Object, options, pwdHasherMock.Object, userValidators,
            pwdValidators, keyNormalizerMock.Object, errors, serviceProviderMock.Object, loggerUserManagerMock.Object);

        userManagerMock.SetupGet(um => um.SupportsUserEmail).Returns(true);

        // Setup user manager behaviors for successful create/login flow
        userManagerMock.Setup(u => u.CreateAsync(It.IsAny<IdentityUser<Guid>>()))
            .ReturnsAsync(IdentityResult.Success);
        userManagerMock.Setup(u => u.AddLoginAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<ExternalLoginInfo>()))
            .ReturnsAsync(IdentityResult.Success);
        userManagerMock.Setup(u => u.GetUserIdAsync(It.IsAny<IdentityUser<Guid>>()))
            .ReturnsAsync("the-user-id");
        userManagerMock.Setup(u => u.GenerateEmailConfirmationTokenAsync(It.IsAny<IdentityUser<Guid>>()))
            .ReturnsAsync("email-token");

        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        var claimsFactoryMock = new Mock<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>();
        var loggerSignInMock = new Mock<ILogger<SignInManager<IdentityUser<Guid>>>>();
        var authSchemeProviderMock = new Mock<IAuthenticationSchemeProvider>();
        var userConfirmationMock = new Mock<IUserConfirmation<IdentityUser<Guid>>>();
        var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(
            userManagerMock.Object, httpContextAccessorMock.Object, claimsFactoryMock.Object,
            options, loggerSignInMock.Object, authSchemeProviderMock.Object, userConfirmationMock.Object);

        // Prepare ExternalLoginInfo with provider and principal
        var claims = new[] { new Claim(ClaimTypes.Email, "user@example.com") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        var info = new ExternalLoginInfo(principal, "TestProvider", "provkey", "Display");

        signInManagerMock.Setup(s => s.GetExternalLoginInfoAsync(It.IsAny<string?>()))
            .ReturnsAsync(info);

        // Mock SignInAsync to be verifiable
        signInManagerMock.Setup(s => s.SignInAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<bool>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var emailSenderMock = new Mock<IEmailSender>();
        emailSenderMock.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var loggerMock = new Mock<ILogger<ExternalLoginModel>>();

        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock.Setup(u => u.Content("~/")).Returns("/");
        // ActionContext must be non-null because UrlHelperExtensions.Page always accesses it
        var urlRouteData = new RouteData();
        urlRouteData.Values["page"] = "/Account/ExternalLogin";
        urlHelperMock.SetupGet(u => u.ActionContext).Returns(
            new ActionContext(new DefaultHttpContext(), urlRouteData, new ActionDescriptor()));
        urlHelperMock.SetReturnsDefault<string?>("https://example/confirm");

        var model = new ExternalLoginModel(signInManagerMock.Object, userManagerMock.Object, userStoreMock.Object, loggerMock.Object, emailSenderMock.Object)
        {
            Url = urlHelperMock.Object,
            PageContext = new PageContext { HttpContext = new DefaultHttpContext() },
            Input = new ExternalLoginModel.InputModel { Email = "user@example.com" }
        };

        var returnUrl = "/localReturn";

        // Act
        var result = await model.OnPostConfirmationAsync(returnUrl);

        // Assert
        if (requireConfirmedAccount)
        {
            var redirect = Assert.IsType<RedirectToPageResult>(result);
            Assert.Equal("./RegisterConfirmation", redirect.PageName);
            Assert.True(redirect.RouteValues?.ContainsKey("Email"));
            Assert.Equal("user@example.com", redirect.RouteValues!["Email"]);
            // SignIn should NOT be invoked when confirmation is required
            signInManagerMock.Verify(s => s.SignInAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<bool>(), It.IsAny<string?>()), Times.Never);
        }
        else
        {
            var localRedirect = Assert.IsType<LocalRedirectResult>(result);
            Assert.Equal(returnUrl, localRedirect.Url);
            // SignIn should be invoked
            signInManagerMock.Verify(s => s.SignInAsync(It.IsAny<IdentityUser<Guid>>(), false, info.LoginProvider), Times.Once);
        }

        // Email should be sent in both cases (the UI sends confirmation email)
        emailSenderMock.Verify(e => e.SendEmailAsync("user@example.com", "Confirm your email", It.Is<string>(s => s.Contains("clicking here") || s.Contains("clicking here"))), Times.Once);
    }

    /// <summary>
    /// Provides combinations of UserManager.SupportsUserEmail and whether the provided user store implements IUserEmailStore.
    /// The tuple values are:
    /// - supportsEmail: bool indicating UserManager.SupportsUserEmail
    /// - storeIsEmailStore: bool indicating whether the provided user store implements IUserEmailStore
    /// - expectedExceptionType: Type? expected exception type (null if construction should succeed)
    /// </summary>
    public static IEnumerable<object?[]> ConstructorTestData()
    {
        yield return [false, true, typeof(NotSupportedException)];
        yield return [true, false, typeof(InvalidCastException)];
        yield return [true, true, null];
    }

}
