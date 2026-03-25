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
/// Tests for ChangePasswordModel.OnGetAsync behavior.
/// Covers branches where user is null, user exists with/without password, and propagation of exceptions.
/// </summary>
[Trait("Category", "Unit")]
public class ChangePasswordModelTests
{
    public static IEnumerable<object?[]> GetUserIdValues()
    {
        // Provide a concrete string and a null value to exercise both message forms
        yield return
        [
            "test-user-123"
        ];
        yield return
        [
            null
        ];
    }

    /// <summary>
    /// Arrange: ModelState contains an error (invalid model).
    /// Act: Call OnPostAsync.
    /// Assert: A PageResult is returned and no call to GetUserAsync is made.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task OnPostAsync_ModelStateInvalid_ReturnsPage()
    {
        // Arrange
        var userStore = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(userStore, null, null, null, null, null, null, null, null);
        var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(userManagerMock.Object, Mock.Of<IHttpContextAccessor>(), Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(), null, null, null, null);
        var loggerMock = new Mock<ILogger<ChangePasswordModel>>();
        var model = new ChangePasswordModel(userManagerMock.Object, signInManagerMock.Object, loggerMock.Object);

        // Put a dummy principal on the PageContext (not strictly used because we expect early exit)
        model.PageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal()
            }
        };

        // Make model invalid
        model.ModelState.AddModelError("SomeKey", "Some error");

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);

        // Ensure GetUserAsync was never called due to early return
        userManagerMock.Verify(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()), Times.Never);
    }

    /// <summary>
    /// Arrange: UserManager.GetUserAsync returns null and GetUserId returns a specific id.
    /// Act: Call OnPostAsync.
    /// Assert: NotFoundObjectResult is returned with the expected message containing the user id.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [Fact]
    public async Task OnPostAsync_UserNotFound_ReturnsNotFoundWithUserId()
    {
        // Arrange
        var userStore = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(userStore, null, null, null, null, null, null, null, null);
        var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(userManagerMock.Object, Mock.Of<IHttpContextAccessor>(), Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(), null, null, null, null);
        var loggerMock = new Mock<ILogger<ChangePasswordModel>>();
        var model = new ChangePasswordModel(userManagerMock.Object, signInManagerMock.Object, loggerMock.Object);

        // Prepare a principal and page context
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        model.PageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };

        // Setup UserManager to return null user and a known id
        const string expectedId = "expected-user-id";
        userManagerMock.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((IdentityUser<Guid>?)null);
        userManagerMock.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(expectedId);

        // Set Input so the null/whitespace guard is bypassed and we reach the user lookup
        model.Input = new ChangePasswordModel.InputModel { OldPassword = "OldP@ss1!", NewPassword = "NewP@ss1!" };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal($"Unable to load user with ID '{expectedId}'.", notFound.Value);
    }

    /// <summary>
    /// Verifies that constructing ChangePasswordModel with valid (non-null) dependencies does not throw.
    /// Input conditions: valid instances for userManager, signInManager and logger must be provided.
    /// Expected result: constructor completes without throwing and the resulting object is not null.
    ///
    /// NOTE (Skipped): Creating UserManager<IdentityUser<Guid>> and SignInManager<IdentityUser<Guid>>
    /// requires providing concrete implementations of many ASP.NET Core Identity services (IUserStore,
    /// IOptions, IPasswordHasher, validators, ILookupNormalizer, IdentityErrorDescriber, IServiceProvider, etc.)
    /// and/or using advanced Moq constructions with explicit constructor arguments. Because such setup is
    /// environment-specific and beyond the scope of this generated test, this test is marked as skipped and
    /// documents how to implement it:
    /// - Create a concrete UserStore/IUserStore<IdentityUser<Guid>> (or mock one) and provide all required
    ///   constructor arguments to the UserManager constructor, then pass the instance into ChangePasswordModel.
    /// - Alternatively, construct mocks for UserManager and SignInManager by supplying the required constructor
    ///   arguments to Mock<T>'s constructor (see Moq documentation) and then call mock.Object.
    /// </summary>
    [Fact]
    public void Constructor_WithValidDependencies_DoesNotThrow()
    {
        // Arrange
        var userStoreMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var identityOptions = Options.Create(new IdentityOptions());
        var passwordHasher = new PasswordHasher<IdentityUser<Guid>>();
        var userValidators = Array.Empty<IUserValidator<IdentityUser<Guid>>>();
        var passwordValidators = Array.Empty<IPasswordValidator<IdentityUser<Guid>>>();
        var lookupNormalizer = new UpperInvariantLookupNormalizer();
        var errorDescriber = new IdentityErrorDescriber();
        var serviceProvider = new Mock<IServiceProvider>().Object;
        var userManagerLogger = new Mock<ILogger<UserManager<IdentityUser<Guid>>>>().Object;
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(userStoreMock.Object, identityOptions, passwordHasher, userValidators, passwordValidators, lookupNormalizer, errorDescriber, serviceProvider, userManagerLogger);
        var httpContextAccessor = new Mock<IHttpContextAccessor>().Object;
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>().Object;
        var signInManagerLogger = new Mock<ILogger<SignInManager<IdentityUser<Guid>>>>().Object;
        var schemes = new Mock<IAuthenticationSchemeProvider>().Object;
        var confirmation = new Mock<IUserConfirmation<IdentityUser<Guid>>>().Object;
        var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(userManagerMock.Object, httpContextAccessor, claimsFactory, identityOptions, signInManagerLogger, schemes, confirmation);
        var loggerMock = new Mock<ILogger<ChangePasswordModel>>();

        // Act
        var model = new ChangePasswordModel(userManagerMock.Object, signInManagerMock.Object, loggerMock.Object);

        // Assert
        Assert.NotNull(model);
    }

    /// <summary>
    /// Partial test template for null-parameter behavior.
    /// Input conditions: one or more constructor parameters are null.
    /// Expected result: If the implementation adds null checks, an ArgumentNullException should be thrown.
    ///
    /// NOTE (Skipped): The current ChangePasswordModel constructor in the provided source file does simple assignments
    /// and does not perform null checks. This test is provided as a template and skipped. If you add null validation
    /// to the constructor, remove Skip and implement the arrange/act/assert commented code below.
    /// </summary>
    [Fact]
    public void Constructor_NullParameters_DoesNotThrow()
    {
        // Arrange
        // The current implementation of ChangePasswordModel does not validate constructor arguments.
        // Verify that constructing with null managers does not throw (reflects current behavior).
        var loggerMock = new Mock<ILogger<ChangePasswordModel>>();

        // Act
        var exception = Record.Exception(() => new ChangePasswordModel(null!, null!, loggerMock.Object));

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    /// Verifies that OnGetAsync returns NotFoundObjectResult containing the user ID when GetUserAsync returns null.
    /// Input: GetUserAsync returns null; GetUserId returns "some-user-id".
    /// Expected: NotFoundObjectResult with value containing the user ID string.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task OnGetAsync_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var (userManagerMock, signInManagerMock, loggerMock) = CreateMocks();
        userManagerMock.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((IdentityUser<Guid>?)null);
        userManagerMock.Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("some-user-id");

        var model = CreateModel(userManagerMock.Object, signInManagerMock.Object, loggerMock.Object);

        // Act
        var result = await model.OnGetAsync();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("some-user-id", notFound.Value?.ToString() ?? string.Empty, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that OnGetAsync redirects to the SetPassword page when the user has no password set.
    /// Input: GetUserAsync returns a user; HasPasswordAsync returns false.
    /// Expected: RedirectToPageResult with page name "./SetPassword".
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task OnGetAsync_UserHasNoPassword_RedirectsToSetPassword()
    {
        // Arrange
        var (userManagerMock, signInManagerMock, loggerMock) = CreateMocks();
        var user = new IdentityUser<Guid>();
        userManagerMock.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManagerMock.Setup(um => um.HasPasswordAsync(user)).ReturnsAsync(false);

        var model = CreateModel(userManagerMock.Object, signInManagerMock.Object, loggerMock.Object);

        // Act
        var result = await model.OnGetAsync();

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./SetPassword", redirect.PageName);
    }

    /// <summary>
    /// Verifies that OnGetAsync returns PageResult when the user exists and has a password set.
    /// Input: GetUserAsync returns a user; HasPasswordAsync returns true.
    /// Expected: PageResult.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task OnGetAsync_UserHasPassword_ReturnsPage()
    {
        // Arrange
        var (userManagerMock, signInManagerMock, loggerMock) = CreateMocks();
        var user = new IdentityUser<Guid>();
        userManagerMock.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManagerMock.Setup(um => um.HasPasswordAsync(user)).ReturnsAsync(true);

        var model = CreateModel(userManagerMock.Object, signInManagerMock.Object, loggerMock.Object);

        // Act
        var result = await model.OnGetAsync();

        // Assert
        Assert.IsType<PageResult>(result);
    }

    /// <summary>
    /// Verifies that OnPostAsync returns PageResult without calling GetUserAsync when OldPassword is null.
    /// Input: Input.OldPassword = null, Input.NewPassword = "NewP@ss1!".
    /// Expected: PageResult returned; GetUserAsync is never called.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task OnPostAsync_NullOldPassword_ReturnsPageWithoutCallingGetUser()
    {
        // Arrange
        var (userManagerMock, signInManagerMock, loggerMock) = CreateMocks();
        var model = CreateModel(userManagerMock.Object, signInManagerMock.Object, loggerMock.Object);
        model.Input = new ChangePasswordModel.InputModel { OldPassword = null, NewPassword = "NewP@ss1!" };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        userManagerMock.Verify(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()), Times.Never);
    }

    /// <summary>
    /// Verifies that OnPostAsync returns PageResult and populates ModelState errors when ChangePasswordAsync fails.
    /// Input: valid user returned, ChangePasswordAsync returns a failed IdentityResult with error descriptions.
    /// Expected: PageResult; ModelState contains the error descriptions from IdentityResult.Errors.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task OnPostAsync_ChangePasswordFails_ReturnsPageWithModelErrors()
    {
        // Arrange
        var (userManagerMock, signInManagerMock, loggerMock) = CreateMocks();
        var user = new IdentityUser<Guid>();
        var failedResult = IdentityResult.Failed(new IdentityError { Description = "Password too weak." });
        userManagerMock.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManagerMock
            .Setup(um => um.ChangePasswordAsync(user, "OldP@ss1!", "NewP@ss1!"))
            .ReturnsAsync(failedResult);

        var model = CreateModel(userManagerMock.Object, signInManagerMock.Object, loggerMock.Object);
        model.Input = new ChangePasswordModel.InputModel { OldPassword = "OldP@ss1!", NewPassword = "NewP@ss1!" };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        Assert.Contains(model.ModelState.Values.SelectMany(v => v.Errors), e => e.ErrorMessage == "Password too weak.");
    }

    /// <summary>
    /// Verifies that OnPostAsync refreshes the sign-in, sets StatusMessage, and redirects on successful password change.
    /// Input: valid user, ChangePasswordAsync returns IdentityResult.Success.
    /// Expected: RefreshSignInAsync is called once; StatusMessage is set; RedirectToPageResult is returned.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task OnPostAsync_ChangePasswordSucceeds_SetsStatusMessageAndRedirects()
    {
        // Arrange
        var (userManagerMock, signInManagerMock, loggerMock) = CreateMocks();
        var user = new IdentityUser<Guid>();
        userManagerMock.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManagerMock
            .Setup(um => um.ChangePasswordAsync(user, "OldP@ss1!", "NewP@ss1!"))
            .ReturnsAsync(IdentityResult.Success);
        signInManagerMock
            .Setup(sm => sm.RefreshSignInAsync(user))
            .Returns(Task.CompletedTask);

        var model = CreateModel(userManagerMock.Object, signInManagerMock.Object, loggerMock.Object);
        model.Input = new ChangePasswordModel.InputModel { OldPassword = "OldP@ss1!", NewPassword = "NewP@ss1!" };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        signInManagerMock.Verify(sm => sm.RefreshSignInAsync(user), Times.Once);
        Assert.Equal("Your password has been changed.", model.StatusMessage);
        Assert.IsType<RedirectToPageResult>(result);
    }

    private static (Mock<UserManager<IdentityUser<Guid>>> userManager,
                    Mock<SignInManager<IdentityUser<Guid>>> signInManager,
                    Mock<ILogger<ChangePasswordModel>> logger) CreateMocks()
    {
        var userStore = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(
            userStore, null, null, null, null, null, null, null, null);
        var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(
            userManagerMock.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(),
            null,
            null,
            null,
            null);
        var loggerMock = new Mock<ILogger<ChangePasswordModel>>();
        return (userManagerMock, signInManagerMock, loggerMock);
    }

    private static ChangePasswordModel CreateModel(
        UserManager<IdentityUser<Guid>> userManager,
        SignInManager<IdentityUser<Guid>> signInManager,
        ILogger<ChangePasswordModel> logger)
    {
        var model = new ChangePasswordModel(userManager, signInManager, logger);
        model.PageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() },
        };
        return model;
    }
}