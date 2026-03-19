#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
namespace Identity.Tests.Pages.Account.Manage;

using System.Security.Claims;
using Identity.Pages.Account.Manage;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

[Trait("Category", "Unit")]
public class ExternalLoginsModelTests
{
    public static TheoryData<string?> Providers() => new()
    {
        "Google",
        string.Empty, // empty provider
        "   ", // whitespace-only provider
        "prov!der@#%", // special chars
    };

    public static TheoryData<int, string?, bool> ShowRemoveData() => new()
    {
        // loginCount, passwordHash, expectedShowRemoveButton
        { 0, null, false },          // no logins, no password -> cannot remove
        { 0, "hash", true },         // no logins, but has password -> can remove
        { 1, null, false },          // single external login, no password -> cannot remove
        { 2, null, true },           // multiple external logins -> can remove
    };

    /// <summary>
    /// Verifies that the ExternalLoginsModel constructor succeeds (does not throw) and that
    /// public properties remain at their default values when constructed with null or a mocked IUserStore.
    /// Tests two input conditions for the userStore parameter: null and non-null (mocked).
    /// Expected result: instance is created; CurrentLogins and OtherLogins are null; ShowRemoveButton is false; StatusMessage is null.
    /// </summary>
    /// <param name="userStoreIsNull">If true, pass null for userStore; otherwise pass a mocked IUserStore instance.</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ExternalLoginsModel_Constructor_NullAndMockedUserStore_ObjectConstructedAndDefaults(bool userStoreIsNull)
    {
        // Arrange
        UserManager<IdentityUser<Guid>>? userManager = null;
        SignInManager<IdentityUser<Guid>>? signInManager = null;
        IUserStore<IdentityUser<Guid>>? userStore = null;

        if (!userStoreIsNull)
        {
            var userStoreMock = new Mock<IUserStore<IdentityUser<Guid>>>(MockBehavior.Strict);
            userStore = userStoreMock.Object;
        }

        // Act
        var model = new ExternalLoginsModel(userManager, signInManager, userStore);

        // Assert
        Assert.NotNull(model);

        // Public auto-properties are initialized to empty collections by the class
        Assert.NotNull(model.CurrentLogins);
        Assert.Empty(model.CurrentLogins);
        Assert.NotNull(model.OtherLogins);
        Assert.Empty(model.OtherLogins);
        Assert.False(model.ShowRemoveButton);
        Assert.Null(model.StatusMessage);
    }

    /// <summary>
    /// Verifies that the ExternalLoginsModel constructor allows all three parameters to be null and does not throw.
    /// Input conditions: all dependencies null.
    /// Expected result: instance is created successfully.
    /// </summary>
    [Fact]
    public void ExternalLoginsModel_Constructor_AllParametersNull_DoesNotThrowCreatesInstance()
    {
        // Arrange
        UserManager<IdentityUser<Guid>>? userManager = null;
        SignInManager<IdentityUser<Guid>>? signInManager = null;
        IUserStore<IdentityUser<Guid>>? userStore = null;

        // Act & Assert - constructor should not throw
        var exception = Record.Exception(() => new ExternalLoginsModel(userManager, signInManager, userStore));
        Assert.Null(exception);
    }

    /// <summary>
    /// The test verifies that when the current user cannot be loaded, the handler returns NotFoundObjectResult
    /// containing the user id retrieved from UserManager.GetUserId(ClaimsPrincipal).
    /// </summary>
    [Fact]
    public async Task OnGetLinkLoginCallbackAsync_UserNotFound_ReturnsNotFoundObjectResult()
    {
        // Arrange
        var expectedUserId = "missing-user-id";
        var userStore = Mock.Of<IUserStore<IdentityUser<Guid>>>();

        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(userStore, null, null, null, null, null, null, null, null);
        userManagerMock
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((IdentityUser<Guid>?)null);
        userManagerMock
            .Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(expectedUserId);

        var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(
            userManagerMock.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(),
            null,
            Mock.Of<ILogger<SignInManager<IdentityUser<Guid>>>>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<IdentityUser<Guid>>>());

        var model = new ExternalLoginsModel(userManagerMock.Object, signInManagerMock.Object, userStore);
        model.PageContext = new PageContext { HttpContext = new DefaultHttpContext() };

        // Act
        var result = await model.OnGetLinkLoginCallbackAsync();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var expectedMessage = $"Unable to load user with ID '{expectedUserId}'.";
        Assert.Equal(expectedMessage, notFound.Value);
    }

    /// <summary>
    /// The test verifies that when external login info cannot be loaded for an existing user,
    /// the handler throws InvalidOperationException.
    /// Input conditions:
    /// - A valid user exists.
    /// - SignInManager.GetExternalLoginInfoAsync returns null for that user id.
    /// Expected:
    /// - InvalidOperationException is thrown.
    /// </summary>
    [Fact]
    public async Task OnGetLinkLoginCallbackAsync_NoExternalLoginInfo_ThrowsInvalidOperationException()
    {
        // Arrange
        var user = new IdentityUser<Guid> { Id = Guid.NewGuid() };
        var userIdString = "user-uid-123";

        var userStore = Mock.Of<IUserStore<IdentityUser<Guid>>>();

        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(
            userStore, null, null, null, null, null, null, null, null);
        userManagerMock
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);
        userManagerMock
            .Setup(um => um.GetUserIdAsync(user))
            .ReturnsAsync(userIdString);

        var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(
            userManagerMock.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(),
            null,
            Mock.Of<ILogger<SignInManager<IdentityUser<Guid>>>>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<IdentityUser<Guid>>>());
        signInManagerMock
            .Setup(sm => sm.GetExternalLoginInfoAsync(userIdString))
            .ReturnsAsync((ExternalLoginInfo?)null);

        var model = new ExternalLoginsModel(userManagerMock.Object, signInManagerMock.Object, userStore);
        model.PageContext = new PageContext { HttpContext = new DefaultHttpContext() };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => model.OnGetLinkLoginCallbackAsync());
    }

    /// <summary>
    /// Parameterized test covering both AddLoginAsync failure and success cases.
    /// Conditions:
    /// - A valid user exists.
    /// - ExternalLoginInfo is available.
    /// - AddLoginAsync returns success or failure based on the parameter.
    /// Expected:
    /// - For success: StatusMessage set to success text and RedirectToPageResult returned.
    /// - For failure: StatusMessage set to failure text and RedirectToPageResult returned.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task OnGetLinkLoginCallbackAsync_AddLoginResult_UpdatesStatusMessageAndRedirects(bool addSucceeded)
    {
        // Arrange
        var user = new IdentityUser<Guid> { Id = Guid.NewGuid() };
        var userIdString = "user-uid-456";
        var provider = "TestProvider";
        var providerKey = "prov-key";

        var userStore = Mock.Of<IUserStore<IdentityUser<Guid>>>();

        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(
            userStore, null, null, null, null, null, null, null, null);
        userManagerMock
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);
        userManagerMock
            .Setup(um => um.GetUserIdAsync(user))
            .ReturnsAsync(userIdString);

        var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(
            userManagerMock.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(),
            null,
            Mock.Of<ILogger<SignInManager<IdentityUser<Guid>>>>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<IdentityUser<Guid>>>());

        var externalPrincipal = new ClaimsPrincipal(new ClaimsIdentity());
        var info = new ExternalLoginInfo(externalPrincipal, provider, providerKey, displayName: provider);

        signInManagerMock
            .Setup(sm => sm.GetExternalLoginInfoAsync(userIdString))
            .ReturnsAsync(info);

        var result = addSucceeded
            ? IdentityResult.Success
            : IdentityResult.Failed(new IdentityError { Description = "fail" });

        userManagerMock
            .Setup(um => um.AddLoginAsync(user, info))
            .ReturnsAsync(result);

        var mockAuthService = new Mock<IAuthenticationService>();
        mockAuthService
            .Setup(a => a.SignOutAsync(It.IsAny<HttpContext>(), IdentityConstants.ExternalScheme, It.IsAny<AuthenticationProperties>()))
            .Returns(Task.CompletedTask);
        var services = new Mock<IServiceProvider>();
        services.Setup(s => s.GetService(typeof(IAuthenticationService))).Returns(mockAuthService.Object);

        var model = new ExternalLoginsModel(userManagerMock.Object, signInManagerMock.Object, userStore);
        var httpContext = new DefaultHttpContext { RequestServices = services.Object };
        model.PageContext = new PageContext { HttpContext = httpContext };

        // Act
        var actionResult = await model.OnGetLinkLoginCallbackAsync();

        // Assert
        Assert.IsType<RedirectToPageResult>(actionResult);
        if (addSucceeded)
        {
            Assert.Equal("The external login was added.", model.StatusMessage);
        }
        else
        {
            Assert.Equal("The external login was not added. External logins can only be associated with one account.", model.StatusMessage);
        }
    }

    /// <summary>
    /// Tests that when the current user cannot be loaded (UserManager.GetUserAsync returns null),
    /// OnPostRemoveLoginAsync returns a NotFoundObjectResult containing the user id returned by UserManager.GetUserId.
    /// Input conditions: UserManager.GetUserAsync returns null and UserManager.GetUserId returns a known id.
    /// Expected: NotFoundObjectResult with message including the id, and no call to RemoveLoginAsync or RefreshSignInAsync.
    /// </summary>
    [Fact]
    public async Task OnPostRemoveLoginAsync_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var userStoreMockForCtor = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(
            userStoreMockForCtor,
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<IPasswordHasher<IdentityUser<Guid>>>(),
            new List<IUserValidator<IdentityUser<Guid>>>(),
            new List<IPasswordValidator<IdentityUser<Guid>>>(),
            Mock.Of<ILookupNormalizer>(),
            Mock.Of<IdentityErrorDescriber>(),
            Mock.Of<IServiceProvider>(),
            Mock.Of<ILogger<UserManager<IdentityUser<Guid>>>>());

        // Setup GetUserAsync to return null and GetUserId to return a known id string
        const string expectedUserId = "known-user-id";
        userManagerMock
            .Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((IdentityUser<Guid>?)null);
        userManagerMock
            .Setup(u => u.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(expectedUserId);

        var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(
            userManagerMock.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(),
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<ILogger<SignInManager<IdentityUser<Guid>>>>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<IdentityUser<Guid>>>());

        var model = new ExternalLoginsModel(userManagerMock.Object, signInManagerMock.Object, Mock.Of<IUserStore<IdentityUser<Guid>>>());

        // Act
        var result = await model.OnPostRemoveLoginAsync("provider", "key");

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFound.Value);
        var asString = notFound.Value.ToString() ?? string.Empty;
        Assert.Contains(expectedUserId, asString);

        // Ensure RemoveLoginAsync and RefreshSignInAsync were not invoked
        userManagerMock.Verify(u => u.RemoveLoginAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        signInManagerMock.Verify(s => s.RefreshSignInAsync(It.IsAny<IdentityUser<Guid>>()), Times.Never);
    }

    /// <summary>
    /// Tests that when RemoveLoginAsync fails, the method sets StatusMessage accordingly and returns a RedirectToPageResult.
    /// Input conditions: UserManager.GetUserAsync returns a valid user; RemoveLoginAsync returns a failed IdentityResult.
    /// Expected: StatusMessage == "The external login was not removed.", RedirectToPageResult returned, and RefreshSignInAsync not called.
    /// This test is parameterized to cover several string edge cases for loginProvider and providerKey (empty, whitespace, long).
    /// </summary>
    [Theory]
    [InlineData("", "key")]
    [InlineData("   ", " ")]
    [InlineData("provider", "")]
    [InlineData("provider", "very-long-key-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx")]
    public async Task OnPostRemoveLoginAsync_RemoveLoginFails_SetsFailureMessageAndRedirects(string loginProvider, string providerKey)
    {
        // Arrange
        var user = new IdentityUser<Guid> { Id = Guid.NewGuid() };
        var userStoreMockForCtor = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(
            userStoreMockForCtor,
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<IPasswordHasher<IdentityUser<Guid>>>(),
            new List<IUserValidator<IdentityUser<Guid>>>(),
            new List<IPasswordValidator<IdentityUser<Guid>>>(),
            Mock.Of<ILookupNormalizer>(),
            Mock.Of<IdentityErrorDescriber>(),
            Mock.Of<IServiceProvider>(),
            Mock.Of<ILogger<UserManager<IdentityUser<Guid>>>>());

        userManagerMock
            .Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);

        var failedResult = IdentityResult.Failed(new IdentityError { Description = "remove failed" });
        userManagerMock
            .Setup(u => u.RemoveLoginAsync(It.Is<IdentityUser<Guid>>(x => x == user), loginProvider, providerKey))
            .ReturnsAsync(failedResult);

        var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(
            userManagerMock.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(),
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<ILogger<SignInManager<IdentityUser<Guid>>>>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<IdentityUser<Guid>>>());

        // Ensure RefreshSignInAsync would be observable if called
        signInManagerMock
            .Setup(s => s.RefreshSignInAsync(It.IsAny<IdentityUser<Guid>>()))
            .Returns(Task.CompletedTask);

        var model = new ExternalLoginsModel(userManagerMock.Object, signInManagerMock.Object, Mock.Of<IUserStore<IdentityUser<Guid>>>());

        // Pre-condition check: StatusMessage should be null or empty
        Assert.Null(model.StatusMessage);

        // Act
        var result = await model.OnPostRemoveLoginAsync(loginProvider, providerKey);

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("The external login was not removed.", model.StatusMessage);

        // Verify RemoveLoginAsync called with expected parameters
        userManagerMock.Verify(u => u.RemoveLoginAsync(It.Is<IdentityUser<Guid>>(x => x == user), loginProvider, providerKey), Times.Once);

        // Ensure sign-in refresh was NOT called
        signInManagerMock.Verify(s => s.RefreshSignInAsync(It.IsAny<IdentityUser<Guid>>()), Times.Never);
    }

    /// <summary>
    /// Tests that when RemoveLoginAsync succeeds, the method refreshes the sign-in, sets a success StatusMessage, and redirects.
    /// Input conditions: UserManager.GetUserAsync returns a valid user; RemoveLoginAsync returns IdentityResult.Success.
    /// Expected: RefreshSignInAsync called once with the user; StatusMessage == "The external login was removed."; RedirectToPageResult returned.
    /// This test is parameterized to exercise different provider/providerKey inputs.
    /// </summary>
    [Theory]
    [InlineData("Google", "google-key")]
    [InlineData("LocalProvider", "local-key")]
    [InlineData("P", "K")]
    public async Task OnPostRemoveLoginAsync_RemoveLoginSucceeds_RefreshesSignInAndSetsSuccessMessage(string loginProvider, string providerKey)
    {
        // Arrange
        var user = new IdentityUser<Guid> { Id = Guid.NewGuid() };
        var userStoreMockForCtor = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(
            userStoreMockForCtor,
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<IPasswordHasher<IdentityUser<Guid>>>(),
            new List<IUserValidator<IdentityUser<Guid>>>(),
            new List<IPasswordValidator<IdentityUser<Guid>>>(),
            Mock.Of<ILookupNormalizer>(),
            Mock.Of<IdentityErrorDescriber>(),
            Mock.Of<IServiceProvider>(),
            Mock.Of<ILogger<UserManager<IdentityUser<Guid>>>>());

        userManagerMock
            .Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);

        userManagerMock
            .Setup(u => u.RemoveLoginAsync(It.Is<IdentityUser<Guid>>(x => x == user), loginProvider, providerKey))
            .ReturnsAsync(IdentityResult.Success);

        var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(
            userManagerMock.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(),
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<ILogger<SignInManager<IdentityUser<Guid>>>>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<IdentityUser<Guid>>>());

        signInManagerMock
            .Setup(s => s.RefreshSignInAsync(It.Is<IdentityUser<Guid>>(x => x == user)))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var model = new ExternalLoginsModel(userManagerMock.Object, signInManagerMock.Object, Mock.Of<IUserStore<IdentityUser<Guid>>>());

        // Act
        var result = await model.OnPostRemoveLoginAsync(loginProvider, providerKey);

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("The external login was removed.", model.StatusMessage);

        userManagerMock.Verify(u => u.RemoveLoginAsync(It.Is<IdentityUser<Guid>>(x => x == user), loginProvider, providerKey), Times.Once);
        signInManagerMock.Verify(s => s.RefreshSignInAsync(It.Is<IdentityUser<Guid>>(x => x == user)), Times.Once);
    }

    /// <summary>
    /// Verifies that OnPostLinkLoginAsync signs out the external cookie, configures external authentication properties,
    /// and returns a ChallengeResult with the same provider and the configured AuthenticationProperties.
    /// Input conditions: various provider string values (including empty and special characters).
    /// Expected result: SignOutAsync called once with IdentityConstants.ExternalScheme; ConfigureExternalAuthenticationProperties invoked;
    /// returned IActionResult is ChallengeResult with expected provider and properties.
    /// </summary>
    [Theory]
    [MemberData(nameof(Providers))]
    public async Task OnPostLinkLoginAsync_Provider_ReturnsChallengeAndSignsOut(string? provider)
    {
        // Arrange
        // Mock UserManager
        var mockUserStoreForUserManager = new Mock<IUserStore<IdentityUser<Guid>>>().Object;
        var mockUserManager = new Mock<UserManager<IdentityUser<Guid>>>(
            mockUserStoreForUserManager,
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<IPasswordHasher<IdentityUser<Guid>>>(),
            Array.Empty<IUserValidator<IdentityUser<Guid>>>(),
            Array.Empty<IPasswordValidator<IdentityUser<Guid>>>(),
            Mock.Of<ILookupNormalizer>(),
            Mock.Of<IdentityErrorDescriber>(),
            Mock.Of<IServiceProvider>(),
            Mock.Of<ILogger<UserManager<IdentityUser<Guid>>>>());

        // Return a predictable user id from GetUserId
        const string expectedUserId = "user-id-123";
        mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(expectedUserId);

        // Mock SignInManager with required dependencies
        var mockSignInManager = new Mock<SignInManager<IdentityUser<Guid>>>(
            mockUserManager.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(),
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<ILogger<SignInManager<IdentityUser<Guid>>>>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<IdentityUser<Guid>>>());

        // Prepare AuthenticationProperties that the SignInManager should return
        var expectedProperties = new AuthenticationProperties(new Dictionary<string, string?> { { "k", "v" } });

        // We'll set the UrlHelper to return this redirect
        const string expectedRedirect = "/ExternalLogins?handler=LinkLoginCallback";
        var mockUrlHelper = new Mock<IUrlHelper>();

        // ActionContext must be non-null because UrlHelperExtensions.Page always accesses it
        var urlRouteData = new RouteData();
        urlRouteData.Values["page"] = "/Account/Manage/ExternalLogins";
        mockUrlHelper.SetupGet(u => u.ActionContext).Returns(
            new ActionContext(new DefaultHttpContext(), urlRouteData, new ActionDescriptor()));

        // SetReturnsDefault covers all string?-returning methods including RouteUrl called by Url.Page
        mockUrlHelper.SetReturnsDefault<string?>(expectedRedirect);

        // Configure SignInManager to return expected properties when called with the given inputs
        mockSignInManager
            .Setup(s => s.ConfigureExternalAuthenticationProperties(
                It.Is<string>(p => p == provider),
                It.Is<string>(r => r == expectedRedirect),
                It.Is<string>(id => id == expectedUserId)))
            .Returns(expectedProperties);

        // Mock IUserStore for constructor (not used in this test but required by SUT ctor)
        var mockUserStore = new Mock<IUserStore<IdentityUser<Guid>>>();

        // Prepare IAuthenticationService mock and service provider so HttpContext.SignOutAsync resolves to it
        var mockAuthService = new Mock<IAuthenticationService>();
        mockAuthService
            .Setup(a => a.SignOutAsync(It.IsAny<HttpContext>(), IdentityConstants.ExternalScheme, It.IsAny<AuthenticationProperties>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var services = new Mock<IServiceProvider>();
        services
            .Setup(s => s.GetService(typeof(IAuthenticationService)))
            .Returns(mockAuthService.Object);

        // HttpContext for PageModel
        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.Object
        };

        // Provide a non-null User to PageModel (DefaultHttpContext has an empty ClaimsPrincipal)
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, expectedUserId)
        ]));

        // Construct model and inject Url and PageContext (to set HttpContext and Url)
        var model = new ExternalLoginsModel(mockUserManager.Object, mockSignInManager.Object, mockUserStore.Object)
        {
            Url = mockUrlHelper.Object,
            PageContext = new PageContext { HttpContext = httpContext }
        };

        // Act
        var result = await model.OnPostLinkLoginAsync(provider);

        // Assert
        // Verify result is a ChallengeResult with expected provider and properties
        var challenge = Assert.IsType<ChallengeResult>(result);
        Assert.Contains(provider, challenge.AuthenticationSchemes);
        Assert.Same(expectedProperties, challenge.Properties);

        // Verify SignOutAsync was invoked on the authentication service with the external scheme
        mockAuthService.Verify(
            a =>
            a.SignOutAsync(httpContext, IdentityConstants.ExternalScheme, It.IsAny<AuthenticationProperties>()), Times.Once);

        // Verify ConfigureExternalAuthenticationProperties called once with expected arguments
        mockSignInManager.Verify(
            s => s.ConfigureExternalAuthenticationProperties(
            It.Is<string>(p => p == provider),
            It.Is<string>(r => r == expectedRedirect),
            It.Is<string>(id => id == expectedUserId)), Times.Once);

        // Verify GetUserId was called with the PageModel's User
        mockUserManager.Verify(u => u.GetUserId(httpContext.User), Times.Once);
    }
}