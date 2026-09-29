namespace Identity.Tests.Unit.Pages.Account.Manage;

using System.Security.Claims;
using Identity.Pages.Account.Manage;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class ExternalLoginsTests
{
    private const int SingleCharacterLength = 1;

    [Fact]
    public async Task OnGetLinkLoginCallbackAsync_UserNotFound_ReturnsNotFoundObjectResult()
    {
        // Arrange
        var expectedUserId = Generated.NewUserId().ToString();
        var userStore = Mock.Of<IUserStore<IdentityUser<Guid>>>();

        var userManagerMock = MockHelpers.MockUserManager();
        userManagerMock
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((IdentityUser<Guid>?)null);
        userManagerMock
            .Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(expectedUserId);

        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);

        var model = new ExternalLogins(userManagerMock.Object, signInManagerMock.Object, userStore);
        model.PageContext = new PageContext { HttpContext = new DefaultHttpContext() };

        // Act
        var result = await model.OnGetLinkLoginCallbackAsync();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var expectedMessage = UserMessages.UnableToLoadUser(expectedUserId);
        Assert.Equal(expectedMessage, notFound.Value);
    }

    [Fact]
    public async Task OnGetLinkLoginCallbackAsync_NoExternalLoginInfo_ThrowsInvalidOperationException()
    {
        // Arrange
        var user = new IdentityUser<Guid> { Id = Generated.NewUserId() };
        var userIdString = Generated.NewUserId().ToString();

        var userStore = Mock.Of<IUserStore<IdentityUser<Guid>>>();

        var userManagerMock = MockHelpers.MockUserManager();
        userManagerMock
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);
        userManagerMock
            .Setup(um => um.GetUserIdAsync(user))
            .ReturnsAsync(userIdString);

        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);
        signInManagerMock
            .Setup(sm => sm.GetExternalLoginInfoAsync(userIdString))
            .ReturnsAsync((ExternalLoginInfo?)null);

        var model = new ExternalLogins(userManagerMock.Object, signInManagerMock.Object, userStore);
        model.PageContext = new PageContext { HttpContext = new DefaultHttpContext() };

        // Act
        var exception = await Record.ExceptionAsync(() => model.OnGetLinkLoginCallbackAsync());

        // Assert
        Assert.IsType<InvalidOperationException>(exception);
    }

    [Fact]
    public async Task OnGetLinkLoginCallbackAsync_AddLoginSucceeds_RedirectsWithAddedStatusMessage()
    {
        // Arrange
        var model = BuildModelForLinkLoginCallback(IdentityResult.Success);

        // Act
        var actionResult = await model.OnGetLinkLoginCallbackAsync();

        // Assert
        Assert.IsType<RedirectToPageResult>(actionResult);
        Assert.Equal(ExternalLogins.LoginAddedMessage, model.StatusMessage);
    }

    [Fact]
    public async Task OnGetLinkLoginCallbackAsync_AddLoginFails_RedirectsWithNotAddedStatusMessage()
    {
        // Arrange
        var addLoginFailure = IdentityResult.Failed(new IdentityError { Description = Generated.NewFailureReason() });
        var model = BuildModelForLinkLoginCallback(addLoginFailure);

        // Act
        var actionResult = await model.OnGetLinkLoginCallbackAsync();

        // Assert
        Assert.IsType<RedirectToPageResult>(actionResult);
        Assert.Equal(ExternalLogins.LoginNotAddedMessage, model.StatusMessage);
    }

    [Fact]
    public async Task OnPostRemoveLoginAsync_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var userStoreMockForCtor = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(
            userStoreMockForCtor,
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<IPasswordHasher<IdentityUser<Guid>>>(),
            Array.Empty<IUserValidator<IdentityUser<Guid>>>(),
            Array.Empty<IPasswordValidator<IdentityUser<Guid>>>(),
            Mock.Of<ILookupNormalizer>(),
            Mock.Of<IdentityErrorDescriber>(),
            Mock.Of<IServiceProvider>(),
            NullLogger<UserManager<IdentityUser<Guid>>>.Instance);

        var expectedUserId = Generated.NewUserId().ToString();
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
            NullLogger<SignInManager<IdentityUser<Guid>>>.Instance,
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<IdentityUser<Guid>>>());

        var model = new ExternalLogins(userManagerMock.Object, signInManagerMock.Object, Mock.Of<IUserStore<IdentityUser<Guid>>>());

        // Act
        var result = await model.OnPostRemoveLoginAsync(Generated.NewSchemeName(), Generated.NewProviderKey());

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var message = Assert.IsType<string>(notFound.Value);
        Assert.Contains(expectedUserId, message, StringComparison.Ordinal);
        userManagerMock.Verify(u => u.RemoveLoginAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        signInManagerMock.Verify(s => s.RefreshSignInAsync(It.IsAny<IdentityUser<Guid>>()), Times.Never);
    }

    [Fact]
    public async Task OnPostRemoveLoginAsync_RemoveLoginFailsForBlankLoginProvider_SetsFailureMessageAndRedirects()
    {
        // Arrange
        var blankLoginProvider = Generated.NewBlank();
        var providerKey = Generated.NewProviderKey();
        var removal = BuildModelForRemoveLogin(blankLoginProvider, providerKey, RemoveLoginFailure());
        Assert.Null(removal.Model.StatusMessage);

        // Act
        var result = await removal.Model.OnPostRemoveLoginAsync(blankLoginProvider, providerKey);

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(ExternalLogins.LoginNotRemovedMessage, removal.Model.StatusMessage);
        removal.UserManagerMock.Verify(u => u.RemoveLoginAsync(It.Is<IdentityUser<Guid>>(x => x == removal.User), blankLoginProvider, providerKey), Times.Once);
        removal.SignInManagerMock.Verify(s => s.RefreshSignInAsync(It.IsAny<IdentityUser<Guid>>()), Times.Never);
    }

    [Fact]
    public async Task OnPostRemoveLoginAsync_RemoveLoginFailsForWhitespaceLoginProviderAndProviderKey_SetsFailureMessageAndRedirects()
    {
        // Arrange
        var whitespaceLoginProvider = Generated.NewWhitespaceValue();
        var whitespaceProviderKey = Generated.NewWhitespaceValue();
        var removal = BuildModelForRemoveLogin(whitespaceLoginProvider, whitespaceProviderKey, RemoveLoginFailure());
        Assert.Null(removal.Model.StatusMessage);

        // Act
        var result = await removal.Model.OnPostRemoveLoginAsync(whitespaceLoginProvider, whitespaceProviderKey);

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(ExternalLogins.LoginNotRemovedMessage, removal.Model.StatusMessage);
        removal.UserManagerMock.Verify(u => u.RemoveLoginAsync(It.Is<IdentityUser<Guid>>(x => x == removal.User), whitespaceLoginProvider, whitespaceProviderKey), Times.Once);
        removal.SignInManagerMock.Verify(s => s.RefreshSignInAsync(It.IsAny<IdentityUser<Guid>>()), Times.Never);
    }

    [Fact]
    public async Task OnPostRemoveLoginAsync_RemoveLoginFailsForBlankProviderKey_SetsFailureMessageAndRedirects()
    {
        // Arrange
        var loginProvider = Generated.NewSchemeName();
        var blankProviderKey = Generated.NewBlank();
        var removal = BuildModelForRemoveLogin(loginProvider, blankProviderKey, RemoveLoginFailure());
        Assert.Null(removal.Model.StatusMessage);

        // Act
        var result = await removal.Model.OnPostRemoveLoginAsync(loginProvider, blankProviderKey);

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(ExternalLogins.LoginNotRemovedMessage, removal.Model.StatusMessage);
        removal.UserManagerMock.Verify(u => u.RemoveLoginAsync(It.Is<IdentityUser<Guid>>(x => x == removal.User), loginProvider, blankProviderKey), Times.Once);
        removal.SignInManagerMock.Verify(s => s.RefreshSignInAsync(It.IsAny<IdentityUser<Guid>>()), Times.Never);
    }

    [Fact]
    public async Task OnPostRemoveLoginAsync_RemoveLoginFailsForOverlongProviderKey_SetsFailureMessageAndRedirects()
    {
        // Arrange
        var loginProvider = Generated.NewSchemeName();
        var overlongProviderKey = Generated.NewOverlongDisplayName();
        var removal = BuildModelForRemoveLogin(loginProvider, overlongProviderKey, RemoveLoginFailure());
        Assert.Null(removal.Model.StatusMessage);

        // Act
        var result = await removal.Model.OnPostRemoveLoginAsync(loginProvider, overlongProviderKey);

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(ExternalLogins.LoginNotRemovedMessage, removal.Model.StatusMessage);
        removal.UserManagerMock.Verify(u => u.RemoveLoginAsync(It.Is<IdentityUser<Guid>>(x => x == removal.User), loginProvider, overlongProviderKey), Times.Once);
        removal.SignInManagerMock.Verify(s => s.RefreshSignInAsync(It.IsAny<IdentityUser<Guid>>()), Times.Never);
    }

    [Fact]
    public async Task OnPostRemoveLoginAsync_RemoveLoginSucceeds_RefreshesSignInAndSetsSuccessMessage()
    {
        // Arrange
        var loginProvider = Generated.NewSchemeName();
        var providerKey = Generated.NewProviderKey();
        var removal = BuildModelForRemoveLogin(loginProvider, providerKey, IdentityResult.Success);

        // Act
        var result = await removal.Model.OnPostRemoveLoginAsync(loginProvider, providerKey);

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(ExternalLogins.LoginRemovedMessage, removal.Model.StatusMessage);
        removal.UserManagerMock.Verify(u => u.RemoveLoginAsync(It.Is<IdentityUser<Guid>>(x => x == removal.User), loginProvider, providerKey), Times.Once);
        removal.SignInManagerMock.Verify(s => s.RefreshSignInAsync(It.Is<IdentityUser<Guid>>(x => x == removal.User)), Times.Once);
    }

    [Fact]
    public async Task OnPostRemoveLoginAsync_RemoveLoginSucceedsForSingleCharacterLoginProviderAndProviderKey_RefreshesSignInAndSetsSuccessMessage()
    {
        // Arrange
        var singleCharacterLoginProvider = Generated.LowercaseToken(SingleCharacterLength);
        var singleCharacterProviderKey = Generated.LowercaseToken(SingleCharacterLength);
        var removal = BuildModelForRemoveLogin(singleCharacterLoginProvider, singleCharacterProviderKey, IdentityResult.Success);

        // Act
        var result = await removal.Model.OnPostRemoveLoginAsync(singleCharacterLoginProvider, singleCharacterProviderKey);

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(ExternalLogins.LoginRemovedMessage, removal.Model.StatusMessage);
        removal.UserManagerMock.Verify(u => u.RemoveLoginAsync(It.Is<IdentityUser<Guid>>(x => x == removal.User), singleCharacterLoginProvider, singleCharacterProviderKey), Times.Once);
        removal.SignInManagerMock.Verify(s => s.RefreshSignInAsync(It.Is<IdentityUser<Guid>>(x => x == removal.User)), Times.Once);
    }

    [Fact]
    public async Task OnPostLinkLoginAsync_SchemeNameProvider_ReturnsChallengeAndSignsOut()
    {
        // Arrange
        var schemeNameProvider = Generated.NewSchemeName();
        var linking = BuildModelForLinkLogin(schemeNameProvider);

        // Act
        var result = await linking.Model.OnPostLinkLoginAsync(schemeNameProvider);

        // Assert
        AssertChallengedAndSignedOut(result, linking, schemeNameProvider);
    }

    [Fact]
    public async Task OnPostLinkLoginAsync_BlankProvider_ReturnsChallengeAndSignsOut()
    {
        // Arrange
        var blankProvider = Generated.NewBlank();
        var linking = BuildModelForLinkLogin(blankProvider);

        // Act
        var result = await linking.Model.OnPostLinkLoginAsync(blankProvider);

        // Assert
        AssertChallengedAndSignedOut(result, linking, blankProvider);
    }

    [Fact]
    public async Task OnPostLinkLoginAsync_WhitespaceProvider_ReturnsChallengeAndSignsOut()
    {
        // Arrange
        var whitespaceProvider = Generated.NewWhitespaceValue();
        var linking = BuildModelForLinkLogin(whitespaceProvider);

        // Act
        var result = await linking.Model.OnPostLinkLoginAsync(whitespaceProvider);

        // Assert
        AssertChallengedAndSignedOut(result, linking, whitespaceProvider);
    }

    [Fact]
    public async Task OnPostLinkLoginAsync_PunctuatedProvider_ReturnsChallengeAndSignsOut()
    {
        // Arrange
        var punctuatedProvider = Generated.NewPunctuatedPageName();
        var linking = BuildModelForLinkLogin(punctuatedProvider);

        // Act
        var result = await linking.Model.OnPostLinkLoginAsync(punctuatedProvider);

        // Assert
        AssertChallengedAndSignedOut(result, linking, punctuatedProvider);
    }

    private static LinkLoginArrangement BuildModelForLinkLogin(string provider)
    {
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
            NullLogger<UserManager<IdentityUser<Guid>>>.Instance);

        var expectedUserId = Generated.NewUserId().ToString();
        mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(expectedUserId);

        var mockSignInManager = new Mock<SignInManager<IdentityUser<Guid>>>(
            mockUserManager.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(),
            Mock.Of<IOptions<IdentityOptions>>(),
            NullLogger<SignInManager<IdentityUser<Guid>>>.Instance,
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<IdentityUser<Guid>>>());

        var expectedProperties = new AuthenticationProperties(
            new Dictionary<string, string?>(StringComparer.Ordinal) { { Generated.NewPropertyKey(), Generated.NewPropertyValue() } });
        var expectedRedirect = Generated.NewLocalPath();
        var mockUrlHelper = new Mock<IUrlHelper>(MockBehavior.Strict);
        var urlRouteData = new RouteData();
        urlRouteData.Values[AspNetRouteConstants.PageRouteValueName] = ExternalLogins.ExternalLoginsPagePath;
        mockUrlHelper.SetupGet(u => u.ActionContext).Returns(
            new ActionContext(new DefaultHttpContext(), urlRouteData, new ActionDescriptor()));

        mockUrlHelper.Setup(u => u.RouteUrl(It.IsAny<UrlRouteContext>())).Returns(expectedRedirect);
        mockSignInManager
            .Setup(s => s.ConfigureExternalAuthenticationProperties(
                It.Is<string>(p => p == provider),
                It.Is<string>(r => r == expectedRedirect),
                It.Is<string>(id => id == expectedUserId)))
            .Returns(expectedProperties);

        var mockUserStore = new Mock<IUserStore<IdentityUser<Guid>>>();
        var mockAuthService = new Mock<IAuthenticationService>(MockBehavior.Strict);
        mockAuthService
            .Setup(a => a.SignOutAsync(It.IsAny<HttpContext>(), IdentityConstants.ExternalScheme, It.IsAny<AuthenticationProperties>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var services = new Mock<IServiceProvider>(MockBehavior.Loose);
        services
            .Setup(s => s.GetService(typeof(IAuthenticationService)))
            .Returns(mockAuthService.Object);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.Object
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, expectedUserId)
        ]));

        var model = new ExternalLogins(mockUserManager.Object, mockSignInManager.Object, mockUserStore.Object)
        {
            Url = mockUrlHelper.Object,
            PageContext = new PageContext { HttpContext = httpContext }
        };

        return new LinkLoginArrangement
        {
            Model = model,
            UserManagerMock = mockUserManager,
            SignInManagerMock = mockSignInManager,
            AuthServiceMock = mockAuthService,
            HttpContext = httpContext,
            ExpectedProperties = expectedProperties,
            ExpectedRedirect = expectedRedirect,
            ExpectedUserId = expectedUserId,
        };
    }

    private static void AssertChallengedAndSignedOut(IActionResult result, LinkLoginArrangement linking, string provider)
    {
        var challenge = Assert.IsType<ChallengeResult>(result);
        Assert.Contains(provider, challenge.AuthenticationSchemes);
        Assert.Same(linking.ExpectedProperties, challenge.Properties);
        linking.AuthServiceMock.Verify(
            a => a.SignOutAsync(
                linking.HttpContext,
                IdentityConstants.ExternalScheme,
                It.IsAny<AuthenticationProperties>()),
            Times.Once);
        linking.SignInManagerMock.Verify(
            s => s.ConfigureExternalAuthenticationProperties(
                It.Is<string>(p => p == provider),
                It.Is<string>(r => r == linking.ExpectedRedirect),
                It.Is<string>(id => id == linking.ExpectedUserId)),
            Times.Once);
        linking.UserManagerMock.Verify(u => u.GetUserId(linking.HttpContext.User), Times.Once);
    }

    private static IdentityResult RemoveLoginFailure() =>
        IdentityResult.Failed(new IdentityError { Description = Generated.NewFailureReason() });

    private static RemoveLoginArrangement BuildModelForRemoveLogin(string loginProvider, string providerKey, IdentityResult removeLoginResult)
    {
        var user = new IdentityUser<Guid> { Id = Generated.NewUserId() };
        var userStoreMockForCtor = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(
            userStoreMockForCtor,
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<IPasswordHasher<IdentityUser<Guid>>>(),
            Array.Empty<IUserValidator<IdentityUser<Guid>>>(),
            Array.Empty<IPasswordValidator<IdentityUser<Guid>>>(),
            Mock.Of<ILookupNormalizer>(),
            Mock.Of<IdentityErrorDescriber>(),
            Mock.Of<IServiceProvider>(),
            NullLogger<UserManager<IdentityUser<Guid>>>.Instance);

        userManagerMock
            .Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);

        userManagerMock
            .Setup(u => u.RemoveLoginAsync(It.Is<IdentityUser<Guid>>(x => x == user), loginProvider, providerKey))
            .ReturnsAsync(removeLoginResult);

        var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(
            userManagerMock.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(),
            Mock.Of<IOptions<IdentityOptions>>(),
            NullLogger<SignInManager<IdentityUser<Guid>>>.Instance,
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<IdentityUser<Guid>>>());

        signInManagerMock
            .Setup(s => s.RefreshSignInAsync(It.IsAny<IdentityUser<Guid>>()))
            .Returns(Task.CompletedTask);

        var model = new ExternalLogins(userManagerMock.Object, signInManagerMock.Object, Mock.Of<IUserStore<IdentityUser<Guid>>>());
        return new RemoveLoginArrangement
        {
            Model = model,
            UserManagerMock = userManagerMock,
            SignInManagerMock = signInManagerMock,
            User = user,
        };
    }

    private static ExternalLogins BuildModelForLinkLoginCallback(IdentityResult addLoginResult)
    {
        var linkingUser = new IdentityUser<Guid> { Id = Generated.NewUserId() };
        var linkingUserId = linkingUser.Id.ToString();
        var loginProvider = Generated.NewSchemeName();
        var loginProviderKey = Generated.NewProviderKey();

        var userStore = Mock.Of<IUserStore<IdentityUser<Guid>>>();

        var userManagerMock = MockHelpers.MockUserManager();
        userManagerMock
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(linkingUser);
        userManagerMock
            .Setup(um => um.GetUserIdAsync(linkingUser))
            .ReturnsAsync(linkingUserId);

        var signInManagerMock = MockHelpers.MockSignInManager(userManagerMock.Object);

        var externalPrincipal = new ClaimsPrincipal(new ClaimsIdentity());
        var externalLoginInfo = new ExternalLoginInfo(
            externalPrincipal,
            loginProvider,
            loginProviderKey,
            displayName: loginProvider);

        signInManagerMock
            .Setup(sm => sm.GetExternalLoginInfoAsync(linkingUserId))
            .ReturnsAsync(externalLoginInfo);

        userManagerMock
            .Setup(um => um.AddLoginAsync(linkingUser, externalLoginInfo))
            .ReturnsAsync(addLoginResult);

        var mockAuthService = new Mock<IAuthenticationService>(MockBehavior.Strict);
        mockAuthService
            .Setup(a => a.SignOutAsync(It.IsAny<HttpContext>(), IdentityConstants.ExternalScheme, It.IsAny<AuthenticationProperties>()))
            .Returns(Task.CompletedTask);
        var services = new Mock<IServiceProvider>(MockBehavior.Loose);
        services.Setup(s => s.GetService(typeof(IAuthenticationService))).Returns(mockAuthService.Object);

        var model = new ExternalLogins(userManagerMock.Object, signInManagerMock.Object, userStore);
        model.PageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext { RequestServices = services.Object }
        };
        return model;
    }

    private sealed class LinkLoginArrangement
    {
        public required ExternalLogins Model { get; init; }

        public required Mock<UserManager<IdentityUser<Guid>>> UserManagerMock { get; init; }

        public required Mock<SignInManager<IdentityUser<Guid>>> SignInManagerMock { get; init; }

        public required Mock<IAuthenticationService> AuthServiceMock { get; init; }

        public required HttpContext HttpContext { get; init; }

        public required AuthenticationProperties ExpectedProperties { get; init; }

        public required string ExpectedRedirect { get; init; }

        public required string ExpectedUserId { get; init; }
    }

    private sealed class RemoveLoginArrangement
    {
        public required ExternalLogins Model { get; init; }

        public required Mock<UserManager<IdentityUser<Guid>>> UserManagerMock { get; init; }

        public required Mock<SignInManager<IdentityUser<Guid>>> SignInManagerMock { get; init; }

        public required IdentityUser<Guid> User { get; init; }
    }
}
