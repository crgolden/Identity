namespace Identity.Tests.Pages.Account;

using Identity.Pages.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

public partial class ConfirmEmailModelTests
{
    /// <summary>
    /// Test data for null userId or null code inputs which should redirect to the Index page.
    /// Covers: userId null, code null, both null.
    /// </summary>
    public static IEnumerable<object?[]> RedirectNullCases()
    {
        yield return new object?[] { null, "non-null-code" };
        yield return new object?[] { "non-null-user", null };
        yield return new object?[] { null, null };
    }

    /// <summary>
    /// Verifies that when either userId or code is null the action redirects to the Index page.
    /// Input conditions: combinations where userId or code (or both) are null.
    /// Expected result: RedirectToPageResult with PageName \"/Index\".
    /// </summary>
    [Theory]
    [MemberData(nameof(RedirectNullCases))]
    public async Task OnGetAsync_NullUserIdOrCode_RedirectsToIndex(string? userId, string? code)
    {
        // Arrange
        var storeMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(storeMock.Object, null, null, null, null, null, null, null, null);
        var model = new ConfirmEmailModel(userManagerMock.Object);

        // Act
        IActionResult result = await model.OnGetAsync(userId, code);

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Index", redirect.PageName);
    }

    /// <summary>
    /// Verifies that when the user is not found the action returns NotFoundObjectResult with a descriptive message.
    /// Input conditions: valid (non-null) userId and code, but UserManager.FindByIdAsync returns null.
    /// Expected result: NotFoundObjectResult with message \"Unable to load user with ID '{userId}'.\".
    /// </summary>
    [Fact]
    public async Task OnGetAsync_UserNotFound_ReturnsNotFoundObjectResult()
    {
        // Arrange
        const string userId = "missing-user-id";
        const string code = "unused-code";
        var storeMock = new Mock<IUserStore<IdentityUser<Guid>>>();
        var userManagerMock = new Mock<UserManager<IdentityUser<Guid>>>(storeMock.Object, null, null, null, null, null, null, null, null);
        userManagerMock
            .Setup(u => u.FindByIdAsync(userId))
            .ReturnsAsync((IdentityUser<Guid>?)null);

        var model = new ConfirmEmailModel(userManagerMock.Object);

        // Act
        IActionResult result = await model.OnGetAsync(userId, code);

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal($"Unable to load user with ID '{userId}'.", notFound.Value);
    }

    /// <summary>
    /// Verifies that the constructor does not throw when a null UserManager is provided
    /// and that the public StatusMessage property is null by default.
    /// Input conditions: userManager parameter = null.
    /// Expected result: instance is created successfully and StatusMessage remains null.
    /// </summary>
    [Fact]
    public void ConfirmEmailModel_Constructor_UserManagerNull_DoesNotThrowAndStatusMessageIsNull()
    {
        // Arrange
        UserManager<IdentityUser<Guid>>? userManager = null;

        // Act
        var model = new ConfirmEmailModel(userManager);

        // Assert
        Assert.NotNull(model);
        Assert.Null(model.StatusMessage);
    }

    /// <summary>
    /// Verifies that the constructor accepts a valid UserManager instance and
    /// that the public StatusMessage property is null by default.
    /// Input conditions: userManager parameter = a constructed UserManager with minimal dependencies.
    /// Expected result: instance is created successfully and StatusMessage remains null.
    /// </summary>
    [Fact]
    public void ConfirmEmailModel_Constructor_ValidUserManager_InstanceCreatedAndStatusMessageIsNull()
    {
        // Arrange
        var store = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var options = Mock.Of<IOptions<IdentityOptions>>();
        var passwordHasher = Mock.Of<IPasswordHasher<IdentityUser<Guid>>>();
        var userValidators = new List<IUserValidator<IdentityUser<Guid>>>();
        var passwordValidators = new List<IPasswordValidator<IdentityUser<Guid>>>();
        var keyNormalizer = Mock.Of<ILookupNormalizer>();
        var errors = new IdentityErrorDescriber();
        var services = Mock.Of<IServiceProvider>();
        var logger = Mock.Of<ILogger<UserManager<IdentityUser<Guid>>>>();

        var userManager = new UserManager<IdentityUser<Guid>>(
            store,
            options,
            passwordHasher,
            userValidators,
            passwordValidators,
            keyNormalizer,
            errors,
            services,
            logger);

        // Act
        var model = new ConfirmEmailModel(userManager);

        // Assert
        Assert.NotNull(model);
        Assert.Null(model.StatusMessage);
    }
}
