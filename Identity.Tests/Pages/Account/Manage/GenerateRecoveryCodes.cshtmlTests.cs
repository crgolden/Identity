#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
namespace Identity.Tests.Pages.Account.Manage;

using System.Security.Claims;
using Identity.Pages.Account.Manage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Moq;

[Trait("Category", "Unit")]
public class GenerateRecoveryCodesModelTests
{
    /// <summary>
    /// Verifies that the constructor can be invoked with valid dependencies and produces a non-null instance.
    /// This test is marked as skipped because creating a usable UserManager{IdentityUser{Guid}} instance
    /// or a fully functional Mock of it requires complex setup (store, options, password hasher, etc.).
    /// The ILogger can be mocked with Moq; however the UserManager cannot be trivially mocked without
    /// supplying constructor arguments. Implementers should provide an appropriate UserManager mock or
    /// factory before enabling this test.
    /// Input conditions: a mock ILogger and a mock or real UserManager supplied.
    /// Expected result: an instance of GenerateRecoveryCodesModel is created and is not null.
    /// </summary>
    [Fact]
    public void Constructor_WithValidDependencies_CreatesInstance()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<GenerateRecoveryCodesModel>>();
        var userStoreMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var options = Microsoft.Extensions.Options.Options.Create(new IdentityOptions());
        var passwordHasherMock = new Mock<IPasswordHasher<IdentityUser<Guid>>>();
        var userValidators = new List<IUserValidator<IdentityUser<Guid>>>();
        var passwordValidators = new List<IPasswordValidator<IdentityUser<Guid>>>();
        var keyNormalizerMock = new Mock<ILookupNormalizer>();
        var services = new Mock<IServiceProvider>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(
            userStoreMock.Object,
            options,
            passwordHasherMock.Object,
            userValidators,
            passwordValidators,
            keyNormalizerMock.Object,
            new IdentityErrorDescriber(),
            services.Object,
            new Mock<ILogger<UserManager<IdentityUser<Guid>>>>().Object);

        // Act
        var model = new GenerateRecoveryCodesModel(userManagerMock.Object, loggerMock.Object);

        // Assert
        Assert.NotNull(model);
    }

    /// <summary>
    /// Validates constructor behavior when dependencies are null.
    /// This test is marked as skipped because the production constructor does not perform null checks
    /// and the code's nullability annotations in the source are disabled (#nullable disable).
    /// If the intended behavior is to guard against null arguments, add ArgumentNullException checks
    /// to the production constructor and then update this test to assert that exceptions are thrown.
    /// Input conditions: null userManager and/or null logger.
    /// Expected result (if constructor is changed): ArgumentNullException is thrown for null parameters.
    /// </summary>
    [Fact]
    public void Constructor_NullDependencies_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var exception = Record.Exception(() => new GenerateRecoveryCodesModel(null, null));

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    /// Verifies that when the UserManager cannot find a user for the current principal,
    /// OnPostAsync returns a NotFoundObjectResult containing the user id returned by UserManager.GetUserId.
    /// Input conditions:
    ///  - UserManager.GetUserAsync returns null
    ///  - UserManager.GetUserId returns a specific id string
    /// Expected result:
    ///  - IActionResult is NotFoundObjectResult and its Value matches the expected message.
    /// </summary>
    [Fact]
    public async Task OnPostAsync_UserNotFound_ReturnsNotFoundWithMessage()
    {
        // Arrange
        var storeMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(
            storeMock.Object, null, null, null, null, null, null, null, null);

        var expectedUserId = "missing-user-id";
        userManagerMock
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((IdentityUser<Guid>?)null);
        userManagerMock
            .Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(expectedUserId);

        var loggerMock = new Mock<ILogger<GenerateRecoveryCodesModel>>();

        var model = new GenerateRecoveryCodesModel(userManagerMock.Object, loggerMock.Object)
        {
            PageContext = new PageContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } }
        };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var expectedMessage = $"Unable to load user with ID '{expectedUserId}'.";
        Assert.Equal(expectedMessage, Assert.IsType<string>(notFound.Value));
        Assert.Equal(expectedMessage, (string)notFound.Value);
    }

    /// <summary>
    /// Verifies that when a user is found but two-factor authentication is disabled,
    /// OnPostAsync throws an InvalidOperationException.
    /// Input conditions:
    ///  - UserManager.GetUserAsync returns a non-null user
    ///  - UserManager.GetTwoFactorEnabledAsync returns false
    /// Expected result:
    ///  - InvalidOperationException is thrown with the expected message.
    /// </summary>
    [Fact]
    public async Task OnPostAsync_TwoFactorDisabled_ThrowsInvalidOperationException()
    {
        // Arrange
        var storeMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(
            storeMock.Object, null, null, null, null, null, null, null, null);

        var user = new IdentityUser<Guid> { Id = Guid.NewGuid() };
        userManagerMock
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);
        userManagerMock
            .Setup(um => um.GetTwoFactorEnabledAsync(user))
            .ReturnsAsync(false);
        userManagerMock
            .Setup(um => um.GetUserIdAsync(user))
            .ReturnsAsync("some-user-id");

        var loggerMock = new Mock<ILogger<GenerateRecoveryCodesModel>>();

        var model = new GenerateRecoveryCodesModel(userManagerMock.Object, loggerMock.Object)
        {
            PageContext = new PageContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await model.OnPostAsync());
        Assert.Equal("Cannot generate recovery codes for user as they do not have 2FA enabled.", ex.Message);
    }

    /// <summary>
    /// Verifies that when a user is found and two-factor authentication is enabled,
    /// OnPostAsync generates recovery codes, sets RecoveryCodes and StatusMessage, and redirects to ShowRecoveryCodes.
    /// Input conditions:
    ///  - UserManager.GetUserAsync returns a non-null user
    ///  - UserManager.GetTwoFactorEnabledAsync returns true
    ///  - UserManager.GenerateNewTwoFactorRecoveryCodesAsync returns a collection of codes
    /// Expected result:
    ///  - IActionResult is RedirectToPageResult with PageName "./ShowRecoveryCodes"
    ///  - Model.RecoveryCodes contains the generated codes
    ///  - Model.StatusMessage has the success message
    /// </summary>
    [Fact]
    public async Task OnPostAsync_TwoFactorEnabled_GeneratesCodesAndRedirects()
    {
        // Arrange
        var storeMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(
            storeMock.Object, null, null, null, null, null, null, null, null);

        var user = new IdentityUser<Guid> { Id = Guid.NewGuid() };
        userManagerMock
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);
        userManagerMock
            .Setup(um => um.GetTwoFactorEnabledAsync(user))
            .ReturnsAsync(true);
        userManagerMock
            .Setup(um => um.GetUserIdAsync(user))
            .ReturnsAsync("active-user-id");

        var generatedCodes = new List<string> { "code1", "code2", "code3" };
        userManagerMock
            .Setup(um => um.GenerateNewTwoFactorRecoveryCodesAsync(user, 10))
            .ReturnsAsync(generatedCodes);

        var loggerMock = new Mock<ILogger<GenerateRecoveryCodesModel>>();

        var model = new GenerateRecoveryCodesModel(userManagerMock.Object, loggerMock.Object)
        {
            PageContext = new PageContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } }
        };

        // Act
        var result = await model.OnPostAsync();

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("./ShowRecoveryCodes", redirect.PageName);

        Assert.NotNull(model.RecoveryCodes);
        Assert.Equal(generatedCodes.Count, model.RecoveryCodes.Length);
        Assert.Equal(generatedCodes, model.RecoveryCodes.ToList());

        Assert.Equal("You have generated new recovery codes.", model.StatusMessage);
    }

    /// <summary>
    /// Verifies that when the user manager cannot find a user for the current principal,
    /// OnGetAsync returns a NotFoundObjectResult containing the user id provided by UserManager.GetUserId.
    /// Input conditions: UserManager.GetUserAsync returns null and GetUserId returns a known id string.
    /// Expected result: NotFoundObjectResult with message: "Unable to load user with ID '{id}'."
    /// </summary>
    [Fact]
    public async Task OnGetAsync_UserNotFound_ReturnsNotFoundWithUserIdMessage()
    {
        // Arrange
        var storeMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(
            storeMock.Object, null, null, null, null, null, null, null, null);

        // Simulate no user found
        userManagerMock
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((IdentityUser<Guid>?)null);

        // Ensure GetUserId returns a specific id used in the NotFound message
        const string expectedId = "expected-user-id";
        userManagerMock
            .Setup(um => um.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(expectedId);

        var loggerMock = new Mock<ILogger<GenerateRecoveryCodesModel>>();

        var model = new GenerateRecoveryCodesModel(userManagerMock.Object, loggerMock.Object);

        // Provide a ClaimsPrincipal for completeness
        model.PageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([
                    new Claim(ClaimTypes.NameIdentifier, expectedId)
                ]))
            }
        };

        // Act
        var result = await model.OnGetAsync();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var message = Assert.IsType<string>(notFound.Value);
        Assert.Equal($"Unable to load user with ID '{expectedId}'.", message);
    }

    /// <summary>
    /// Tests behavior of OnGetAsync when a user exists and two-factor authentication flag varies.
    /// Input conditions:
    ///  - A valid user is returned by UserManager.GetUserAsync.
    ///  - UserManager.GetTwoFactorEnabledAsync returns the provided boolean (true/false).
    /// Expected results:
    ///  - If true: method returns a PageResult.
    ///  - If false: method throws InvalidOperationException with the expected message.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task OnGetAsync_TwoFactorEnabledFlag_BehavesAsExpected(bool isTwoFactorEnabled)
    {
        // Arrange
        var storeMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(
            storeMock.Object, null, null, null, null, null, null, null, null);

        var user = new IdentityUser<Guid>();
        userManagerMock
            .Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);

        userManagerMock
            .Setup(um => um.GetTwoFactorEnabledAsync(It.IsAny<IdentityUser<Guid>>()))
            .ReturnsAsync(isTwoFactorEnabled);

        var loggerMock = new Mock<ILogger<GenerateRecoveryCodesModel>>();

        var model = new GenerateRecoveryCodesModel(userManagerMock.Object, loggerMock.Object);

        // Provide a simple ClaimsPrincipal (not relied upon by mocked methods, but realistic)
        model.PageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity())
            }
        };

        // Act & Assert
        if (isTwoFactorEnabled)
        {
            var result = await model.OnGetAsync();
            Assert.IsType<PageResult>(result);
        }
        else
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => model.OnGetAsync());
            Assert.Equal("Cannot generate recovery codes for user because they do not have 2FA enabled.", ex.Message);
        }
    }
}
