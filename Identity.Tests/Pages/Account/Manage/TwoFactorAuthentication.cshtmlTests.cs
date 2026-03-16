#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
namespace Identity.Tests.Pages.Account.Manage;

using System.Security.Claims;
using Identity.Pages.Account.Manage;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

/// <summary>
/// Tests for TwoFactorAuthenticationModel constructor and basic property behavior.
/// Focuses on constructor behavior given nullable dependencies and ensures public properties initialize to expected defaults.
/// </summary>
public class TwoFactorAuthenticationModelTests
{
    /// <summary>
    /// Verifies that constructing TwoFactorAuthenticationModel with null userManager and signInManager
    /// and an optional logger does not throw and initializes public properties to their default values.
    /// Input conditions:
    /// - userManager: null
    /// - signInManager: null
    /// - logger: present or null (parameterized)
    /// Expected result:
    /// - No exception thrown.
    /// - Public properties are default: HasAuthenticator == false, RecoveryCodesLeft == 0,
    ///   Is2faEnabled == false, IsMachineRemembered == false, StatusMessage == null.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Constructor_NullUserManagerAndSignInManager_WithOptionalLogger_InitializesDefaults(bool provideLogger)
    {
        // Arrange
        UserManager<IdentityUser<Guid>>? userManager = null;
        SignInManager<IdentityUser<Guid>>? signInManager = null;
        var logger = provideLogger
            ? new Mock<ILogger<TwoFactorAuthenticationModel>>().Object
            : null;

        // Act
        var ex = Record.Exception(() => new TwoFactorAuthenticationModel(userManager, signInManager));

        // Assert
        Assert.Null(ex); // constructor should not throw

        var model = new TwoFactorAuthenticationModel(userManager, signInManager);

        // Default public properties should reflect defaults (auto-properties default values)
        Assert.False(model.HasAuthenticator);
        Assert.Equal(0, model.RecoveryCodesLeft);
        Assert.False(model.Is2faEnabled);
        Assert.False(model.IsMachineRemembered);
        Assert.Null(model.StatusMessage);
    }

    /// <summary>
    /// Verifies that public auto-properties are writable after construction and reflect assigned values.
    /// Input conditions:
    /// - Construct with null dependencies.
    /// - Set each public property to a non-default value.
    /// Expected result:
    /// - Properties return the values they were set to.
    /// </summary>
    [Fact]
    public void Properties_SetAfterConstruction_ReflectAssignedValues()
    {
        // Arrange
        UserManager<IdentityUser<Guid>>? userManager = null;
        SignInManager<IdentityUser<Guid>>? signInManager = null;
        var logger = new Mock<ILogger<TwoFactorAuthenticationModel>>().Object;

        var model = new TwoFactorAuthenticationModel(userManager, signInManager);

        // Act
        model.HasAuthenticator = true;
        model.RecoveryCodesLeft = 5;
        model.Is2faEnabled = true;
        model.IsMachineRemembered = true;
        model.StatusMessage = "status";

        // Assert
        Assert.True(model.HasAuthenticator);
        Assert.Equal(5, model.RecoveryCodesLeft);
        Assert.True(model.Is2faEnabled);
        Assert.True(model.IsMachineRemembered);
        Assert.Equal("status", model.StatusMessage);
    }

    /// <summary>
    /// Tests that when the user manager cannot find the current user, OnPostAsync returns NotFound with the expected message.
    /// Input conditions: UserManager.GetUserAsync returns null and UserManager.GetUserId returns a non-null identifier.
    /// Expected result: NotFoundObjectResult containing the formatted message and SignInManager.ForgetTwoFactorClientAsync is not called.
    /// </summary>
    [Fact]
    public async Task OnPostAsync_UserNotFound_ReturnsNotFoundWithUserIdMessage()
    {
        // Arrange
        var userStoreMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(userStoreMock.Object, null, null, null, null, null, null, null, null);

        var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(
            userManagerMock.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(),
            Options.Create(new IdentityOptions()),
            Mock.Of<ILogger<SignInManager<IdentityUser<Guid>>>>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<IdentityUser<Guid>>>());

        var loggerMock = Mock.Of<ILogger<TwoFactorAuthenticationModel>>();
        var model = new TwoFactorAuthenticationModel(userManagerMock.Object, signInManagerMock.Object);

        // Prepare ClaimsPrincipal for the PageContext (User)
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        model.PageContext = new PageContext { HttpContext = new DefaultHttpContext { User = principal } };

        const string expectedUserId = "missing-user-id";
        userManagerMock
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((IdentityUser<Guid>?)null);
        userManagerMock
            .Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(expectedUserId);

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal($"Unable to load user with ID '{expectedUserId}'.", notFound.Value);
        signInManagerMock.Verify(s => s.ForgetTwoFactorClientAsync(), Times.Never);
    }

    /// <summary>
    /// Tests that when a user is found, OnPostAsync calls ForgetTwoFactorClientAsync, sets the StatusMessage,
    /// and returns a RedirectToPageResult.
    /// Input conditions: UserManager.GetUserAsync returns a valid user; SignInManager.ForgetTwoFactorClientAsync completes successfully.
    /// Expected result: RedirectToPageResult, StatusMessage set to the expected text, and ForgetTwoFactorClientAsync invoked once.
    /// </summary>
    [Fact]
    public async Task OnPostAsync_UserFound_ForgetsClientAndRedirectsAndSetsStatusMessage()
    {
        // Arrange
        var userStoreMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(
            userStoreMock.Object,
            null, null, null, null, null, null, null, null);

        var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(
            userManagerMock.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(),
            Options.Create(new IdentityOptions()),
            Mock.Of<ILogger<SignInManager<IdentityUser<Guid>>>>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<IdentityUser<Guid>>>());

        var loggerMock = Mock.Of<ILogger<TwoFactorAuthenticationModel>>();
        var model = new TwoFactorAuthenticationModel(userManagerMock.Object, signInManagerMock.Object);

        // Prepare ClaimsPrincipal for the PageContext (User)
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        model.PageContext = new PageContext { HttpContext = new DefaultHttpContext { User = principal } };

        var user = new IdentityUser<Guid> { Id = Guid.NewGuid() };
        userManagerMock
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);

        signInManagerMock
            .Setup(sm => sm.ForgetTwoFactorClientAsync())
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("The current browser has been forgotten. When you login again from this browser you will be prompted for your 2fa code.", model.StatusMessage);
        signInManagerMock.Verify(sm => sm.ForgetTwoFactorClientAsync(), Times.Once);
    }

    /// <summary>
    /// Tests that when GetUserAsync returns null the handler returns NotFoundObjectResult
    /// with a message containing the value returned by GetUserId(User).
    /// Input conditions: UserManager.GetUserAsync returns null; UserManager.GetUserId returns a known id.
    /// Expected result: NotFoundObjectResult with the expected message.
    /// </summary>
    [Fact]
    public async Task OnGetAsync_UserNotFound_ReturnsNotFoundObjectResult()
    {
        // Arrange
        var userStoreMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var mockUserManager = new Mock<UserManager<IdentityUser<Guid>>>(
            userStoreMock.Object, null, null, null, null, null, null, null, null);

        var mockSignInManager = new Mock<SignInManager<IdentityUser<Guid>>>(
            mockUserManager.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(),
            null,
            Mock.Of<ILogger<SignInManager<IdentityUser<Guid>>>>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<IdentityUser<Guid>>>());

        var mockLogger = new Mock<ILogger<TwoFactorAuthenticationModel>>();

        // Setup: GetUserAsync returns null and GetUserId returns a specific id string
        const string expectedId = "expected-id-123";
        mockUserManager
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((IdentityUser<Guid>?)null);
        mockUserManager
            .Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(expectedId);

        var pageModel = new TwoFactorAuthenticationModel(mockUserManager.Object, mockSignInManager.Object);

        // Act
        var result = await pageModel.OnGetAsync();

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal($"Unable to load user with ID '{expectedId}'.", notFoundResult.Value);
    }

    /// <summary>
    /// Parameterized test for the successful path of OnGetAsync.
    /// Purpose: Verify that when GetUserAsync returns a user, the model properties are set
    /// according to the values returned by the user manager and sign-in manager and that PageResult is returned.
    /// Inputs: combinations of authenticator key (null or non-null), two-factor enabled flag,
    /// client-remembered flag, and recovery codes count (including boundary ints).
    /// Expected: Properties HasAuthenticator, Is2faEnabled, IsMachineRemembered, RecoveryCodesLeft match inputs;
    /// result is PageResult.
    /// </summary>
    [Theory]
    [MemberData(nameof(GetOnGetAsyncCases))]
    public async Task OnGetAsync_UserFound_SetsPropertiesAndReturnsPageResult(string? authenticatorKey, bool is2faEnabled, bool isMachineRemembered, int recoveryCodes)
    {
        // Arrange
        var userStoreMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var mockUserManager = new Mock<UserManager<IdentityUser<Guid>>>(
            userStoreMock.Object, null, null, null, null, null, null, null, null);

        var mockSignInManager = new Mock<SignInManager<IdentityUser<Guid>>>(
            mockUserManager.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(),
            null,
            Mock.Of<ILogger<SignInManager<IdentityUser<Guid>>>>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<IdentityUser<Guid>>>());

        var mockLogger = new Mock<ILogger<TwoFactorAuthenticationModel>>();

        var user = new IdentityUser<Guid>();

        // Setup manager behaviors according to parameters
        mockUserManager
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);
        mockUserManager
            .Setup(um => um.GetAuthenticatorKeyAsync(user))
            .ReturnsAsync(authenticatorKey);
        mockUserManager
            .Setup(um => um.GetTwoFactorEnabledAsync(user))
            .ReturnsAsync(is2faEnabled);
        mockUserManager
            .Setup(um => um.CountRecoveryCodesAsync(user))
            .ReturnsAsync(recoveryCodes);

        mockSignInManager
            .Setup(sm => sm.IsTwoFactorClientRememberedAsync(user))
            .ReturnsAsync(isMachineRemembered);

        var pageModel = new TwoFactorAuthenticationModel(mockUserManager.Object, mockSignInManager.Object);

        // Act
        var result = await pageModel.OnGetAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal(authenticatorKey != null, pageModel.HasAuthenticator);
        Assert.Equal(is2faEnabled, pageModel.Is2faEnabled);
        Assert.Equal(isMachineRemembered, pageModel.IsMachineRemembered);
        Assert.Equal(recoveryCodes, pageModel.RecoveryCodesLeft);
    }

    public static IEnumerable<object?[]> GetOnGetAsyncCases()
    {
        // Cover typical, boundary and unusual numeric values as RecoveryCodesLeft, and both null/non-null authenticator key.
        yield return [null, false, false, 0];
        yield return ["auth-key-abc", true, true, 5];
        yield return ["k", false, true, int.MaxValue];
        yield return [null, true, false, int.MinValue];
    }
}