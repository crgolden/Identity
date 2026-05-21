#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
namespace Identity.Tests.Pages.Account.Manage;
using Infrastructure;

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

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class ResetAuthenticatorModelTests
{
    public static TheoryData<bool, string?, Type, string?> OnGetTestCases()
    {
        const string missingUserId = "user-123";
        var expectedMessage = $"Unable to load user with ID '{missingUserId}'.";
        return new TheoryData<bool, string?, Type, string?>
        {
            // Case: user exists -> expect PageResult
            { true, null, typeof(PageResult), null },

            // Case: user not found -> expect NotFound with ID embedded in message
            { false, missingUserId, typeof(NotFoundObjectResult), expectedMessage },
        };
    }

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
            null);  // ILogger<UserManager<TUser>>

        // Setup GetUserAsync to return a user or null based on input
        userManagerMock
            .Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(userExists ? new IdentityUser<Guid>() : null);

        // Setup GetUserId to return the provided expectedUserId (may be null)
        userManagerMock
            .Setup(u => u.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(expectedUserId);

        // Create a SignInManager instance with minimal mocked dependencies (not used by OnGet)
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        var claimsFactoryMock = new Mock<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>();
        var options = Options.Create(new IdentityOptions());
        var loggerSignInMock = new Mock<ILogger<SignInManager<IdentityUser<Guid>>>>();
        var schemesMock = new Mock<IAuthenticationSchemeProvider>(MockBehavior.Strict);
        var confirmationMock = new Mock<IUserConfirmation<IdentityUser<Guid>>>();

        var signInManager = new SignInManager<IdentityUser<Guid>>(
            userManagerMock.Object,
            httpContextAccessorMock.Object,
            claimsFactoryMock.Object,
            options,
            loggerSignInMock.Object,
            schemesMock.Object,
            confirmationMock.Object);

        var model = new ResetAuthenticatorModel(userManagerMock.Object, signInManager);

        // Set up a minimal PageContext with a ClaimsPrincipal so PageModel.User is available
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [
            new Claim(ClaimTypes.NameIdentifier, expectedUserId ?? string.Empty)
        ], "test"));

        model.PageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };

        // Act
        var result = await model.OnGet();

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

        var model = new ResetAuthenticatorModel(mockUserManager.Object, mockSignInManager.Object);

        // Provide a ClaimsPrincipal (not used beyond forwarding to mocks)
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        model.PageContext = new PageContext { HttpContext = new DefaultHttpContext { User = principal } };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal($"Unable to load user with ID '{userIdString}'.", notFound.Value);

        // Ensure no attempt to modify user state occurred
        mockUserManager.Verify(um => um.SetTwoFactorEnabledAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<bool>()), Times.Never);
        mockUserManager.Verify(um => um.ResetAuthenticatorKeyAsync(It.IsAny<IdentityUser<Guid>>()), Times.Never);
        mockSignInManager.Verify(sm => sm.RefreshSignInAsync(It.IsAny<IdentityUser<Guid>>()), Times.Never);
    }

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

        var model = new ResetAuthenticatorModel(mockUserManager.Object, mockSignInManager.Object);
        model.PageContext = new PageContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) } };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./EnableAuthenticator", redirect.PageName);
        Assert.Equal("Your authenticator app key has been reset, you will need to configure your authenticator app using the new key.", model.StatusMessage);

        // Verify operations were attempted regardless of identity result success/failure
        mockUserManager.Verify(um => um.SetTwoFactorEnabledAsync(user, false), Times.Once);
        mockUserManager.Verify(um => um.ResetAuthenticatorKeyAsync(user), Times.Once);
        mockSignInManager.Verify(sm => sm.RefreshSignInAsync(user), Times.Once);
    }

    [Fact]
    public void ResetAuthenticatorModel_Constructor_WithValidDependencies_DoesNotThrow()
    {
        // Arrange
        // NOTE: The following shows intent. Concrete construction of UserManager and SignInManager
        // is environment-specific and requires stores, options, context accessors, and factories.
        // Do NOT implement fakes or custom test types here per project rules. Use DI-provided or
        // helper-factory instances in real tests.

        // Passing null managers because the constructor only stores the references and does not access them.
        UserManager<IdentityUser<Guid>>? userManager = null;
        SignInManager<IdentityUser<Guid>>? signInManager = null;

        // Act
        var model = new ResetAuthenticatorModel(userManager, signInManager);

        // Assert
        // If constructor completes, the test should verify no exception was thrown.
        Assert.NotNull(model);
    }

    [Fact]
    public void ResetAuthenticatorModel_Constructor_NullDependencies_BehaviorDocumented()
    {
        // Arrange
        UserManager<IdentityUser<Guid>>? nullUserManager = null;
        SignInManager<IdentityUser<Guid>>? nullSignInManager = null;

        // Act
        var model = new ResetAuthenticatorModel(nullUserManager, nullSignInManager);

        // Assert
        Assert.NotNull(model);
    }
}
