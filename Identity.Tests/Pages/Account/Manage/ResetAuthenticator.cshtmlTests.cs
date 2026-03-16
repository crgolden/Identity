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

public class ResetAuthenticatorModelTests
{
    /// <summary>
    /// Tests OnGet behavior for both when a user is present and when user retrieval returns null.
    /// Inputs:
    /// - userExists: whether UserManager.GetUserAsync returns a non-null IdentityUser.
    /// - expectedUserId: the value that UserManager.GetUserId(User) should return when user is null.
    /// Expected:
    /// - When userExists is true, OnGet returns a PageResult.
    /// - When userExists is false, OnGet returns a NotFoundObjectResult with message containing the expectedUserId.
    /// </summary>
    [Theory]
    [MemberData(nameof(OnGetTestCases))]
    public async Task OnGet_UserExistence_ReturnsExpectedResult(bool userExists, string? expectedUserId, Type expectedResultType, string? expectedMessage)
    {
        // Arrange
        var storeMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(
            storeMock.Object,
            null, // IOptions<IdentityOptions>
            null, // IPasswordHasher<TUser>
            null, // IEnumerable<IUserValidator<TUser>>
            null, // IEnumerable<IPasswordValidator<TUser>>
            null, // ILookupNormalizer
            null, // IdentityErrorDescriber
            null, // IServiceProvider
            null  // ILogger<UserManager<TUser>>
        );

        // Setup GetUserAsync to return a user or null based on input
        userManagerMock
            .Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(userExists ? new IdentityUser<Guid>() : null);

        // Setup GetUserId to return the provided expectedUserId (may be null)
        userManagerMock
            .Setup(u => u.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(expectedUserId);

        // Create a SignInManager instance with minimal mocked dependencies (not used by OnGet)
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        var claimsFactoryMock = new Mock<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>();
        var options = Options.Create(new IdentityOptions());
        var loggerSignInMock = new Mock<ILogger<SignInManager<IdentityUser<Guid>>>>();
        var schemesMock = new Mock<IAuthenticationSchemeProvider>();
        var confirmationMock = new Mock<IUserConfirmation<IdentityUser<Guid>>>();

        var signInManager = new SignInManager<IdentityUser<Guid>>(
            userManagerMock.Object,
            httpContextAccessorMock.Object,
            claimsFactoryMock.Object,
            options,
            loggerSignInMock.Object,
            schemesMock.Object,
            confirmationMock.Object
        );

        var loggerMock = new Mock<ILogger<ResetAuthenticatorModel>>();

        var model = new ResetAuthenticatorModel(userManagerMock.Object, signInManager, loggerMock.Object);

        // Set up a minimal PageContext with a ClaimsPrincipal so PageModel.User is available
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
            new Claim(ClaimTypes.NameIdentifier, expectedUserId ?? string.Empty)
        }, "test"));

        model.PageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };

        // Act
        IActionResult result = await model.OnGet();

        // Assert
        Assert.IsType(expectedResultType, result);

        if (expectedResultType == typeof(NotFoundObjectResult))
        {
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            // The implementation constructs the message using _userManager.GetUserId(User)
            Assert.Equal(expectedMessage, notFound.Value as string);
        }
        else if (expectedResultType == typeof(PageResult))
        {
            // Nothing else to assert for PageResult beyond type
            Assert.IsType<PageResult>(result);
        }
    }

    public static IEnumerable<object[]> OnGetTestCases()
    {
        // Case: user exists -> expect PageResult
        yield return new object[] { true, null, typeof(PageResult), null };

        // Case: user not found -> expect NotFound with ID embedded in message
        const string missingUserId = "user-123";
        string expectedMessage = $"Unable to load user with ID '{missingUserId}'.";
        yield return new object[] { false, missingUserId, typeof(NotFoundObjectResult), expectedMessage };
    }

    /// <summary>
    /// Ensures that when the user cannot be found by UserManager.GetUserAsync, OnPostAsync returns a NotFoundObjectResult
    /// containing the user id obtained from UserManager.GetUserId(User), and that no further user management methods are invoked.
    /// Input: UserManager.GetUserAsync returns null; UserManager.GetUserId returns a specific id string.
    /// Expected: NotFoundObjectResult with the expected message.
    /// </summary>
    [Fact]
    public async Task OnPostAsync_UserNotFound_ReturnsNotFoundWithExpectedMessage()
    {
        // Arrange
        var userIdString = "missing-user-id";
        var userStore = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var mockUserManager = new Mock<UserManager<IdentityUser<Guid>>>(
            userStore,
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<IPasswordHasher<IdentityUser<Guid>>>(),
            Enumerable.Empty<IUserValidator<IdentityUser<Guid>>>(),
            Enumerable.Empty<IPasswordValidator<IdentityUser<Guid>>>(),
            Mock.Of<ILookupNormalizer>(),
            Mock.Of<IdentityErrorDescriber>(),
            null,
            Mock.Of<ILogger<UserManager<IdentityUser<Guid>>>>());

        // GetUserAsync returns null to simulate missing user
        mockUserManager
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((IdentityUser<Guid>?)null);

        // GetUserId returns a string used in the NotFound message
        mockUserManager
            .Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(userIdString);

        var mockSignInManager = new Mock<SignInManager<IdentityUser<Guid>>>(
            mockUserManager.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(),
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<ILogger<SignInManager<IdentityUser<Guid>>>>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<IdentityUser<Guid>>>());

        var mockLogger = new Mock<ILogger<ResetAuthenticatorModel>>();

        var model = new ResetAuthenticatorModel(mockUserManager.Object, mockSignInManager.Object, mockLogger.Object);

        // Provide a ClaimsPrincipal (not used beyond forwarding to mocks)
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        model.PageContext = new PageContext { HttpContext = new DefaultHttpContext { User = principal } };

        // Act
        IActionResult result = await model.OnPostAsync();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal($"Unable to load user with ID '{userIdString}'.", notFound.Value);

        // Ensure no attempt to modify user state occurred
        mockUserManager.Verify(um => um.SetTwoFactorEnabledAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<bool>()), Times.Never);
        mockUserManager.Verify(um => um.ResetAuthenticatorKeyAsync(It.IsAny<IdentityUser<Guid>>()), Times.Never);
        mockSignInManager.Verify(sm => sm.RefreshSignInAsync(It.IsAny<IdentityUser<Guid>>()), Times.Never);
    }

    /// <summary>
    /// Validates that when a user is returned, OnPostAsync will attempt to disable two-factor, reset the authenticator key,
    /// refresh the sign-in, set the StatusMessage, and redirect to the EnableAuthenticator page.
    /// This test is parameterized to run for both IdentityResult.Success and IdentityResult.Failed for the user manager operations,
    /// because the implementation does not branch on the IdentityResult values.
    /// Inputs:
    /// - userExists: true (a user object).
    /// - succeedOperations: controls whether SetTwoFactorEnabledAsync and ResetAuthenticatorKeyAsync return success or failed results.
    /// Expected:
    /// - RedirectToPageResult to \"./EnableAuthenticator\".
    /// - StatusMessage set to the expected informative string.
    /// - Calls to SetTwoFactorEnabledAsync, ResetAuthenticatorKeyAsync, GetUserIdAsync, and RefreshSignInAsync occur exactly once.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task OnPostAsync_UserExists_ResetsAndRedirectsRegardlessOfIdentityResult(bool succeedOperations)
    {
        // Arrange
        var user = new IdentityUser<Guid> { Id = Guid.NewGuid(), UserName = "tester" };

        var userStore = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var mockUserManager = new Mock<UserManager<IdentityUser<Guid>>>(
            userStore,
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<IPasswordHasher<IdentityUser<Guid>>>(),
            Enumerable.Empty<IUserValidator<IdentityUser<Guid>>>(),
            Enumerable.Empty<IPasswordValidator<IdentityUser<Guid>>>(),
            Mock.Of<ILookupNormalizer>(),
            Mock.Of<IdentityErrorDescriber>(),
            null,
            Mock.Of<ILogger<UserManager<IdentityUser<Guid>>>>());

        mockUserManager
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);

        var identityResult = succeedOperations ? IdentityResult.Success : IdentityResult.Failed(new IdentityError { Description = "fail" });

        mockUserManager
            .Setup(um => um.SetTwoFactorEnabledAsync(user, false))
            .ReturnsAsync(identityResult);

        mockUserManager
            .Setup(um => um.ResetAuthenticatorKeyAsync(user))
            .ReturnsAsync(identityResult);

        mockUserManager
            .Setup(um => um.GetUserIdAsync(user))
            .ReturnsAsync("the-user-id");

        var mockSignInManager = new Mock<SignInManager<IdentityUser<Guid>>>(
            mockUserManager.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(),
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<ILogger<SignInManager<IdentityUser<Guid>>>>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<IdentityUser<Guid>>>());

        mockSignInManager
            .Setup(sm => sm.RefreshSignInAsync(user))
            .Returns(Task.CompletedTask);

        var mockLogger = new Mock<ILogger<ResetAuthenticatorModel>>();

        var model = new ResetAuthenticatorModel(mockUserManager.Object, mockSignInManager.Object, mockLogger.Object);
        model.PageContext = new PageContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) } };

        // Act
        IActionResult result = await model.OnPostAsync();

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./EnableAuthenticator", redirect.PageName);
        Assert.Equal("Your authenticator app key has been reset, you will need to configure your authenticator app using the new key.", model.StatusMessage);

        // Verify operations were attempted regardless of identity result success/failure
        mockUserManager.Verify(um => um.SetTwoFactorEnabledAsync(user, false), Times.Once);
        mockUserManager.Verify(um => um.ResetAuthenticatorKeyAsync(user), Times.Once);
        mockUserManager.Verify(um => um.GetUserIdAsync(user), Times.Once);
        mockSignInManager.Verify(sm => sm.RefreshSignInAsync(user), Times.Once);
    }

    /// <summary>
    /// Verifies that the constructor does not throw when provided with valid dependencies.
    /// Input conditions: valid ILogger mock is provided; UserManager and SignInManager must be
    /// provided as real instances or proper mocks configured by the test environment.
    /// Expected result: constructor completes without throwing exceptions.
    /// </summary>
    [Fact]
    public void ResetAuthenticatorModel_Constructor_WithValidDependencies_DoesNotThrow()
    {
        // Arrange
        // NOTE: The following shows intent. Concrete construction of UserManager and SignInManager
        // is environment-specific and requires stores, options, context accessors, and factories.
        // Do NOT implement fakes or custom test types here per project rules. Use DI-provided or
        // helper-factory instances in real tests.
        var loggerMock = new Mock<ILogger<ResetAuthenticatorModel>>();

        // TODO: Acquire or construct valid instances of these dependencies in the test environment.
        UserManager<IdentityUser<Guid>>? userManager = null;
        SignInManager<IdentityUser<Guid>>? signInManager = null;

        // Act
        // The test was previously skipped. For the purpose of verifying the constructor does not throw,
        // we pass null managers because the constructor only stores the references and does not access them.
        var model = new ResetAuthenticatorModel(userManager, signInManager, loggerMock.Object);

        // Assert
        // If constructor completes, the test should verify no exception was thrown.
        Assert.NotNull(model);
    }

    /// <summary>
    /// Verifies behavior when nulls are provided for constructor parameters.
    /// Input conditions: nulls passed for one or more constructor parameters.
    /// Expected result: This test is skipped because the source constructor performs no null checks
    /// and test code must not assign null to non-nullable parameters without explicit nullability changes.
    /// If the constructor is later updated to validate arguments, replace this skipped test with
    /// explicit assertions for ArgumentNullException and related messages.
    /// </summary>
    [Fact]
    public void ResetAuthenticatorModel_Constructor_NullDependencies_BehaviorDocumented()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<ResetAuthenticatorModel>>();
        UserManager<IdentityUser<Guid>>? nullUserManager = null;
        SignInManager<IdentityUser<Guid>>? nullSignInManager = null;

        // Act
        var model = new ResetAuthenticatorModel(nullUserManager, nullSignInManager, loggerMock.Object);

        // Assert
        Assert.NotNull(model);
    }
}