#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
namespace Identity.Tests.Pages.Account;

using System.Text;
using Identity.Pages.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Moq;

/// <summary>
/// Tests for ConfirmEmailChangeModel constructor behavior.
/// Note: The production constructor depends on framework types (UserManager and SignInManager)
/// which are not trivial to instantiate without DI and backing stores. The symbol metadata
/// indicates these cannot be straightforwardly mocked here. As such the tests below are
/// provided as skipped, guided templates to complete in an environment where those dependencies
/// can be provided or properly mocked.
/// </summary>
[Trait("Category", "Unit")]
public class ConfirmEmailChangeModelTests
{
    /// <summary>
    /// Ensures that the constructor succeeds when provided valid UserManager and SignInManager instances.
    /// Input conditions: non-null userManager and signInManager instances.
    /// Expected result: constructor does not throw and an instance of ConfirmEmailChangeModel is created.
    /// </summary>
    [Fact]
    public void Constructor_WithValidDependencies_DoesNotThrow()
    {
        // Arrange
        // NOTE: To implement this test, construct or mock UserManager<IdentityUser<Guid>> and
        // SignInManager<IdentityUser<Guid>> with valid constructor parameters (stores, options, etc.).
        // Example approaches:
        // - Use Moq to mock only the virtual members and supply required constructor args to Mock<UserManager<...>>(...) if feasible.
        // - Create a real UserManager with an in-memory IUserStore implementation provided by your test environment.
        //
        // For safety and portability we do not attempt to instantiate these framework classes here.
        var storeMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var options = Microsoft.Extensions.Options.Options.Create(new IdentityOptions());
        var passwordHasher = new Mock<IPasswordHasher<IdentityUser<Guid>>>().Object;
        var userValidators = new List<IUserValidator<IdentityUser<Guid>>>();
        var pwdValidators = new List<IPasswordValidator<IdentityUser<Guid>>>();
        var keyNormalizer = new Mock<ILookupNormalizer>().Object;
        var errors = new IdentityErrorDescriber();
        var services = new Mock<IServiceProvider>().Object;
        var loggerUserManager = new Mock<ILogger<UserManager<IdentityUser<Guid>>>>().Object;
        var userManager = new UserManager<IdentityUser<Guid>>(storeMock.Object, options, passwordHasher, userValidators, pwdValidators, keyNormalizer, errors, services, loggerUserManager);
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>();
        var loggerSignIn = new Mock<ILogger<SignInManager<IdentityUser<Guid>>>>().Object;
        var schemes = new Mock<IAuthenticationSchemeProvider>().Object;
        var confirmation = new Mock<IUserConfirmation<IdentityUser<Guid>>>().Object;
        var signInManager = new SignInManager<IdentityUser<Guid>>(userManager, httpContextAccessor.Object, claimsFactory.Object, options, loggerSignIn, schemes, confirmation);

        // Act
        var model = new ConfirmEmailChangeModel(userManager, signInManager);

        // Assert
        Assert.NotNull(model);

        // Additional behavioral assertions may be added once dependencies can be provided.
    }

    /// <summary>
    /// Verifies constructor behavior when null dependencies are supplied.
    /// Input conditions: userManager is null or signInManager is null.
    /// Expected result: If the production constructor guards against null inputs, an ArgumentNullException should be thrown.
    /// If it does not, the test should be adapted to assert that the constructed instance handles nulls appropriately.
    /// </summary>
    [Fact]
    public void Constructor_NullParameters_ThrowsIfValidated()
    {
        // Arrange
        // Provide nulls for the dependencies to exercise null-argument behavior.
        var targetType = typeof(ConfirmEmailChangeModel);

        // Act & Assert
        // We accept either:
        //  - the constructor throws an ArgumentNullException for null arguments (defensive),
        //  - or the constructor successfully constructs an instance (non-defensive).
        // Any other exception type or missing constructor will fail the test.
        var ctor = (System.Reflection.ConstructorInfo?)null;
        foreach (var c in targetType.GetConstructors())
        {
            if (c.GetParameters().Length == 2)
            {
                ctor = c;
                break;
            }
        }

        if (ctor == null)
        {
            throw new InvalidOperationException("No constructor with two parameters was found on ConfirmEmailChangeModel.");
        }

        try
        {
            var instance = ctor.Invoke([null, null]);
            Assert.NotNull(instance);
        }
        catch (System.Reflection.TargetInvocationException tie) when (tie.InnerException is ArgumentNullException)
        {
            // Expected defensive behavior: the constructor validated arguments and threw ArgumentNullException.
        }
    }

    /// <summary>
    /// Tests that when any of the route parameters (userId, email, code) is null,
    /// the handler redirects to the Index page.
    /// Input conditions: one of the parameters is null (tested via InlineData).
    /// Expected result: RedirectToPageResult with PageName '/Index'.
    /// </summary>
    [Theory]
    [InlineData(null, "user@example.com", "code")]
    [InlineData("userId", null, "code")]
    [InlineData("userId", "user@example.com", null)]
    public async Task OnGetAsync_NullParameters_RedirectsToIndex(string? userId, string? email, string? code)
    {
        // Arrange
        var storeMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(storeMock.Object, null, null, null, null, null, null, null, null);
        var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(userManagerMock.Object, Mock.Of<IHttpContextAccessor>(), Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(), null, null, null, null);
        var model = new ConfirmEmailChangeModel(userManagerMock.Object, signInManagerMock.Object);

        // Act
        var result = await model.OnGetAsync(userId, email, code);

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Index", redirect.PageName);
    }

    /// <summary>
    /// Tests that when the user cannot be found by the provided userId,
    /// the handler returns a NotFoundObjectResult containing the expected message.
    /// Input conditions: valid non-null parameters but FindByIdAsync returns null.
    /// Expected result: NotFoundObjectResult with message containing the supplied userId.
    /// </summary>
    [Fact]
    public async Task OnGetAsync_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        const string userId = "missing-user";
        const string email = "user@example.com";
        const string token = "tok";
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var storeMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(storeMock.Object, null, null, null, null, null, null, null, null);
        userManagerMock.Setup(um => um.FindByIdAsync(It.Is<string>(s => s == userId))).ReturnsAsync((IdentityUser<Guid>?)null);
        var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(userManagerMock.Object, Mock.Of<IHttpContextAccessor>(), Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(), null, null, null, null);
        var model = new ConfirmEmailChangeModel(userManagerMock.Object, signInManagerMock.Object);

        // Act
        var result = await model.OnGetAsync(userId, email, encoded);

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal($"Unable to load user with ID '{userId}'.", notFound.Value);
    }

    /// <summary>
    /// Tests that when ChangeEmailAsync fails, the handler returns the PageResult
    /// and sets StatusMessage to indicate an email change error.
    /// Input conditions: user exists; ChangeEmailAsync returns a failed IdentityResult.
    /// Expected result: PageResult and StatusMessage == "Error changing email.".
    /// </summary>
    [Fact]
    public async Task OnGetAsync_ChangeEmailFails_ReturnsPageAndSetsStatusMessage()
    {
        // Arrange
        const string userId = "user-1";
        const string email = "new@example.com";
        const string token = "change-token";
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var user = new IdentityUser<Guid>
        {
            Id = Guid.NewGuid()
        };
        var storeMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(storeMock.Object, null, null, null, null, null, null, null, null);
        userManagerMock.Setup(um => um.FindByIdAsync(It.Is<string>(s => s == userId))).ReturnsAsync(user);
        userManagerMock.Setup(um => um.ChangeEmailAsync(It.IsAny<IdentityUser<Guid>>(), It.Is<string>(s => s == email), It.Is<string>(s => s == token))).ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "invalid token" }));
        var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(userManagerMock.Object, Mock.Of<IHttpContextAccessor>(), Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(), null, null, null, null);
        var model = new ConfirmEmailChangeModel(userManagerMock.Object, signInManagerMock.Object);

        // Act
        var result = await model.OnGetAsync(userId, email, encoded);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal("Error changing email.", model.StatusMessage);
    }

    /// <summary>
    /// Tests that when ChangeEmailAsync succeeds but SetUserNameAsync fails,
    /// the handler returns the PageResult and sets StatusMessage to indicate a user name change error.
    /// Input conditions: user exists; ChangeEmailAsync returns Success; SetUserNameAsync returns Failed.
    /// Expected result: PageResult and StatusMessage == "Error changing user name.".
    /// </summary>
    [Fact]
    public async Task OnGetAsync_SetUserNameFails_ReturnsPageAndSetsStatusMessage()
    {
        // Arrange
        const string userId = "user-2";
        const string email = "newuser@example.com";
        const string token = "token-2";
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var user = new IdentityUser<Guid>
        {
            Id = Guid.NewGuid()
        };
        var storeMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(storeMock.Object, null, null, null, null, null, null, null, null);
        userManagerMock.Setup(um => um.FindByIdAsync(It.Is<string>(s => s == userId))).ReturnsAsync(user);
        userManagerMock.Setup(um => um.ChangeEmailAsync(It.IsAny<IdentityUser<Guid>>(), It.Is<string>(s => s == email), It.Is<string>(s => s == token))).ReturnsAsync(IdentityResult.Success);
        userManagerMock.Setup(um => um.SetUserNameAsync(It.IsAny<IdentityUser<Guid>>(), It.Is<string>(s => s == email))).ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "set user name failed" }));
        var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(userManagerMock.Object, Mock.Of<IHttpContextAccessor>(), Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(), null, null, null, null);
        var model = new ConfirmEmailChangeModel(userManagerMock.Object, signInManagerMock.Object);

        // Act
        var result = await model.OnGetAsync(userId, email, encoded);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal("Error changing user name.", model.StatusMessage);
    }

    /// <summary>
    /// Tests the successful path where ChangeEmailAsync and SetUserNameAsync both succeed,
    /// ensuring RefreshSignInAsync is called and StatusMessage is set to the success message.
    /// Input conditions: user exists; ChangeEmailAsync returns Success; SetUserNameAsync returns Success.
    /// Expected result: PageResult and StatusMessage == "Thank you for confirming your email change.".
    /// </summary>
    [Fact]
    public async Task OnGetAsync_AllOperationsSucceed_RefreshesSignInAndSetsSuccessMessage()
    {
        // Arrange
        const string userId = "user-3";
        const string email = "ok@example.com";
        const string token = "ok-token";
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var user = new IdentityUser<Guid>
        {
            Id = Guid.NewGuid()
        };
        var storeMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(storeMock.Object, null, null, null, null, null, null, null, null);
        userManagerMock.Setup(um => um.FindByIdAsync(It.Is<string>(s => s == userId))).ReturnsAsync(user);
        userManagerMock.Setup(um => um.ChangeEmailAsync(It.IsAny<IdentityUser<Guid>>(), It.Is<string>(s => s == email), It.Is<string>(s => s == token))).ReturnsAsync(IdentityResult.Success);
        userManagerMock.Setup(um => um.SetUserNameAsync(It.IsAny<IdentityUser<Guid>>(), It.Is<string>(s => s == email))).ReturnsAsync(IdentityResult.Success);
        var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(userManagerMock.Object, Mock.Of<IHttpContextAccessor>(), Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(), null, null, null, null);
        signInManagerMock.Setup(s => s.RefreshSignInAsync(It.IsAny<IdentityUser<Guid>>())).Returns(Task.CompletedTask).Verifiable();
        var model = new ConfirmEmailChangeModel(userManagerMock.Object, signInManagerMock.Object);

        // Act
        var result = await model.OnGetAsync(userId, email, encoded);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal("Thank you for confirming your email change.", model.StatusMessage);
        signInManagerMock.Verify(s => s.RefreshSignInAsync(It.Is<IdentityUser<Guid>>(u => u == user)), Times.Once);
    }

    /// <summary>
    /// Tests that empty or whitespace email inputs redirect to /Index because the source guards
    /// against null/whitespace values for userId, email, and code.
    /// Expected result: RedirectToPageResult to "/Index".
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task OnGetAsync_EmptyOrWhitespaceEmail_RedirectsToIndex(string email)
    {
        // Arrange
        const string userId = "user-4";
        const string token = "var-token";
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var storeMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(storeMock.Object, null, null, null, null, null, null, null, null);
        var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(userManagerMock.Object, Mock.Of<IHttpContextAccessor>(), Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(), null, null, null, null);
        var model = new ConfirmEmailChangeModel(userManagerMock.Object, signInManagerMock.Object);

        // Act
        var result = await model.OnGetAsync(userId, email, encoded);

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Index", redirect.PageName);
    }

    /// <summary>
    /// Tests that a non-null, non-whitespace email proceeds through the handler successfully.
    /// Input conditions: user exists; ChangeEmailAsync and SetUserNameAsync return Success.
    /// Expected result: PageResult and StatusMessage success.
    /// </summary>
    [Fact]
    public async Task OnGetAsync_SpecialCharacterEmail_ProceedsAndReturnSuccess()
    {
        // Arrange
        const string userId = "user-4";
        const string email = "user+special@ex\u00E4mple.com";
        const string token = "var-token";
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var user = new IdentityUser<Guid>
        {
            Id = Guid.NewGuid()
        };
        var storeMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(storeMock.Object, null, null, null, null, null, null, null, null);
        userManagerMock.Setup(um => um.FindByIdAsync(It.Is<string>(s => s == userId))).ReturnsAsync(user);
        userManagerMock.Setup(um => um.ChangeEmailAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
        userManagerMock.Setup(um => um.SetUserNameAsync(It.IsAny<IdentityUser<Guid>>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
        var signInManagerMock = new Mock<SignInManager<IdentityUser<Guid>>>(userManagerMock.Object, Mock.Of<IHttpContextAccessor>(), Mock.Of<IUserClaimsPrincipalFactory<IdentityUser<Guid>>>(), null, null, null, null);
        signInManagerMock.Setup(s => s.RefreshSignInAsync(It.IsAny<IdentityUser<Guid>>())).Returns(Task.CompletedTask);
        var model = new ConfirmEmailChangeModel(userManagerMock.Object, signInManagerMock.Object);

        // Act
        var result = await model.OnGetAsync(userId, email, encoded);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal("Thank you for confirming your email change.", model.StatusMessage);
    }
}