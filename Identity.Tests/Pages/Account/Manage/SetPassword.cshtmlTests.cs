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
/// Tests for SetPasswordModel constructor behavior.
/// Note: Constructing real UserManager&lt;TUser&gt; or SignInManager&lt;TUser&gt; requires many framework dependencies.
/// Where such construction/mocking is required, a skipped test with guidance is provided.
/// </summary>
public class SetPasswordModelTests
{
    /// <summary>
    /// Verifies that the SetPasswordModel constructor does not throw when both dependencies are null.
    /// Input conditions: userManager = null, signInManager = null.
    /// Expected result: An instance of SetPasswordModel is created successfully (no exception) and is not null.
    /// </summary>
    [Fact]
    public void Constructor_BothDependenciesNull_DoesNotThrowAndCreatesInstance()
    {
        // Arrange
        UserManager<IdentityUser<Guid>>? userManager = null;
        SignInManager<IdentityUser<Guid>>? signInManager = null;

        // Act
        var exception = Record.Exception(() => new SetPasswordModel(userManager, signInManager));

        // Assert
        Assert.Null(exception);
        var model = new SetPasswordModel(userManager, signInManager);
        Assert.NotNull(model);
    }

    /// <summary>
    /// Partial test: constructing SetPasswordModel with non-null UserManager and SignInManager.
    /// Input conditions: requires properly constructed/mocked UserManager&lt;IdentityUser&lt;Guid&gt;&gt; and SignInManager&lt;IdentityUser&lt;Guid&gt;&gt;.
    /// Expected result: an instance is created without exceptions.
    /// 
    /// Rationale and next steps:
    /// Creating concrete UserManager and SignInManager instances requires many dependencies (stores, accessors, loggers, options, etc.).
    /// According to project constraints, do NOT create custom fake types. Instead, use Moq to mock only if the types' constructors can be satisfied.
    /// To complete this test, provide properly configured mocks or factory methods to create UserManager and SignInManager (e.g., mock IUserStore&lt;T&gt; and other ctor args).
    /// This test is marked skipped until such factory/mocks are provided.
    /// </summary>
    [Fact]
    public void Constructor_WithNonNullDependencies_NotImplemented()
    {
        // Arrange
        // Create the minimal dependencies required to construct concrete UserManager and SignInManager instances.
        var userStoreMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var options = Options.Create(new IdentityOptions());
        var passwordHasher = new PasswordHasher<IdentityUser<Guid>>();
        var userValidators = new List<IUserValidator<IdentityUser<Guid>>>();
        var passwordValidators = new List<IPasswordValidator<IdentityUser<Guid>>>();
        var keyNormalizer = new UpperInvariantLookupNormalizer();
        var identityErrorDescriber = new IdentityErrorDescriber();
        var services = new Mock<IServiceProvider>().Object;
        var userManagerLogger = new Mock<ILogger<UserManager<IdentityUser<Guid>>>>().Object;

        var userManager = new UserManager<IdentityUser<Guid>>(
            userStoreMock.Object,
            options,
            passwordHasher,
            userValidators,
            passwordValidators,
            keyNormalizer,
            identityErrorDescriber,
            services,
            userManagerLogger);

        var httpContextAccessor = new Mock<IHttpContextAccessor>().Object;
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>().Object;
        var signInLogger = new Mock<ILogger<SignInManager<IdentityUser<Guid>>>>().Object;
        var schemeProvider = new Mock<IAuthenticationSchemeProvider>().Object;
        var userConfirmation = new Mock<IUserConfirmation<IdentityUser<Guid>>>().Object;

        var signInManager = new SignInManager<IdentityUser<Guid>>(
            userManager,
            httpContextAccessor,
            claimsFactory,
            options,
            signInLogger,
            schemeProvider,
            userConfirmation);

        // Act
        var exception = Record.Exception(() => new SetPasswordModel(userManager, signInManager));

        // Assert
        Assert.Null(exception);
        var model = new SetPasswordModel(userManager, signInManager);
        Assert.NotNull(model);
    }

    /// <summary>
    /// Verifies that when the model state is invalid, OnPostAsync returns a PageResult without calling user manager or sign-in manager.
    /// Condition: ModelState contains an error.
    /// Expected: PageResult is returned.
    /// </summary>
    [Fact]
    public async Task OnPostAsync_ModelStateInvalid_ReturnsPage()
    {
        // Arrange
        var storeMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(storeMock.Object, null, null, null, null, null, null, null, null);
        var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(
            userManagerMock.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(),
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<ILogger<SignInManager<IdentityUser<Guid>>>>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<IdentityUser<Guid>>>());

        var pageModel = new SetPasswordModel(userManagerMock.Object, signInManagerMock.Object);
        // Make model state invalid
        pageModel.ModelState.AddModelError("Test", "Invalid");

        // Act
        var result = await pageModel.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        // Ensure no user manager/signin calls happened
        userManagerMock.Verify(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>()), Times.Never);
        userManagerMock.Verify(u => u.AddPasswordAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<string>()), Times.Never);
        signInManagerMock.Verify(s => s.RefreshSignInAsync(It.IsAny<IdentityUser<Guid>>()), Times.Never);
    }

    /// <summary>
    /// Verifies that when the current user cannot be found, OnPostAsync returns NotFoundObjectResult containing the user id.
    /// Condition: UserManager.GetUserAsync returns null and GetUserId returns a known id.
    /// Expected: NotFoundObjectResult with the expected message.
    /// </summary>
    [Fact]
    public async Task OnPostAsync_UserNotFound_ReturnsNotFoundWithMessage()
    {
        // Arrange
        var expectedUserId = "user-123";
        var storeMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(
            storeMock.Object, null, null, null, null, null, null, null, null);
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

        var pageModel = new SetPasswordModel(userManagerMock.Object, signInManagerMock.Object);

        // Set Input so the null/whitespace guard is bypassed and we reach the user lookup
        pageModel.Input = new SetPasswordModel.InputModel { NewPassword = "NewP@ss1!" };
        // Act
        var result = await pageModel.OnPostAsync();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var message = Assert.IsType<string>(notFound.Value);
        Assert.Equal($"Unable to load user with ID '{expectedUserId}'.", message);
        userManagerMock.Verify(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>()), Times.Once);
    }

    /// <summary>
    /// Verifies that when UserManager.GetUserAsync returns null the handler returns NotFoundObjectResult
    /// containing the UserId obtained from UserManager.GetUserId(User).
    /// Input: UserManager.GetUserAsync returns null and GetUserId returns a specific id string.
    /// Expected: NotFoundObjectResult whose Value string contains the configured id.
    /// </summary>
    [Fact]
    public async Task OnGetAsync_UserNotFound_ReturnsNotFoundWithUserIdInMessage()
    {
        // Arrange
        var mockUserStore = new Mock<IUserStore<IdentityUser<Guid>>>();
        var mockUserManager = new Mock<UserManager<IdentityUser<Guid>>>(
                mockUserStore.Object,
                null, // IOptions<IdentityOptions>
                null, // IPasswordHasher<IdentityUser<Guid>>
                null, // IEnumerable<IUserValidator<IdentityUser<Guid>>>
                null, // IEnumerable<IPasswordValidator<IdentityUser<Guid>>>
                null, // ILookupNormalizer
                null, // IdentityErrorDescriber
                null, // IServiceProvider
                null  // ILogger<UserManager<IdentityUser<Guid>>>
            )
            { CallBase = false };

        var mockSignInManager = new Mock<SignInManager<IdentityUser<Guid>>>(
                mockUserManager.Object,
                new Mock<IHttpContextAccessor>().Object,
                new Mock<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>().Object,
                null,
                null,
                new Mock<IAuthenticationSchemeProvider>().Object,
                new Mock<IUserConfirmation<IdentityUser<Guid>>>().Object
            )
            { CallBase = false };

        const string expectedId = "expected-user-id";
        mockUserManager
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((IdentityUser<Guid>?)null);
        mockUserManager
            .Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(expectedId);

        var model = new SetPasswordModel(mockUserManager.Object, mockSignInManager.Object);

        // Provide a PageContext with a principal (content of principal is irrelevant due to setups)
        model.PageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity())
            }
        };

        // Act
        var result = await model.OnGetAsync();

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var value = Assert.IsType<string>(notFoundResult.Value);
        Assert.Contains(expectedId, value);
        Assert.Contains("Unable to load user with ID", value);
    }

    /// <summary>
    /// Verifies OnGetAsync behavior for existing users when HasPasswordAsync is true or false.
    /// Inputs: a non-null user returned by GetUserAsync and HasPasswordAsync = true/false.
    /// Expected:
    ///  - when true: RedirectToPageResult to "./ChangePassword".
    ///  - when false: PageResult returned to allow setting a password.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task OnGetAsync_ExistingUser_BehavesBasedOnHasPassword(bool hasPassword)
    {
        // Arrange
        var mockUserStore = new Mock<IUserStore<IdentityUser<Guid>>>();
        var mockUserManager = new Mock<UserManager<IdentityUser<Guid>>>(
                mockUserStore.Object,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null
            )
            { CallBase = false };

        var mockSignInManager = new Mock<SignInManager<IdentityUser<Guid>>>(
                mockUserManager.Object,
                new Mock<IHttpContextAccessor>().Object,
                new Mock<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>().Object,
                null,
                null,
                new Mock<IAuthenticationSchemeProvider>().Object,
                new Mock<IUserConfirmation<IdentityUser<Guid>>>().Object
            )
            { CallBase = false };

        var user = new IdentityUser<Guid>();
        mockUserManager
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);
        mockUserManager
            .Setup(um => um.HasPasswordAsync(user))
            .ReturnsAsync(hasPassword);

        var model = new SetPasswordModel(mockUserManager.Object, mockSignInManager.Object);
        model.PageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity())
            }
        };

        // Act
        var result = await model.OnGetAsync();

        // Assert
        if (hasPassword)
        {
            var redirect = Assert.IsType<RedirectToPageResult>(result);
            Assert.Equal("./ChangePassword", redirect.PageName);
        }
        else
        {
            Assert.IsType<PageResult>(result);
        }
    }
}