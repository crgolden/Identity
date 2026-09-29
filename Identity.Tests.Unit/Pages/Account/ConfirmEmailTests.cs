namespace Identity.Tests.Unit.Pages.Account;

using System.Text;
using Identity.Pages.Account;
using Identity.Tests.Unit.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

[Collection(UnitCollection.Name)]
[Trait("Category", "Unit")]
public class ConfirmEmailTests
{
    [Fact]
    public async Task OnGetAsync_NullUserId_RedirectsToIndex()
    {
        // Arrange
        var code = Generated.NewEmailConfirmationToken();
        var model = BuildModel();

        // Act
        var result = await model.OnGetAsync(null, code);

        // Assert
        AssertRedirectsToIndex(result);
    }

    [Fact]
    public async Task OnGetAsync_NullCode_RedirectsToIndex()
    {
        // Arrange
        var userId = Generated.NewUserId().ToString();
        var model = BuildModel();

        // Act
        var result = await model.OnGetAsync(userId, null);

        // Assert
        AssertRedirectsToIndex(result);
    }

    [Fact]
    public async Task OnGetAsync_NullUserIdAndCode_RedirectsToIndex()
    {
        // Arrange
        var model = BuildModel();

        // Act
        var result = await model.OnGetAsync(null, null);

        // Assert
        AssertRedirectsToIndex(result);
    }

    [Fact]
    public async Task OnGetAsync_EmptyUserId_RedirectsToIndex()
    {
        // Arrange
        var code = Generated.NewEmailConfirmationToken();
        var model = BuildModel();

        // Act
        var result = await model.OnGetAsync(Generated.NewBlank(), code);

        // Assert
        AssertRedirectsToIndex(result);
    }

    [Fact]
    public async Task OnGetAsync_WhitespaceUserId_RedirectsToIndex()
    {
        // Arrange
        var whitespaceUserId = Generated.NewWhitespaceValue();
        var code = Generated.NewEmailConfirmationToken();
        var model = BuildModel();

        // Act
        var result = await model.OnGetAsync(whitespaceUserId, code);

        // Assert
        AssertRedirectsToIndex(result);
    }

    [Fact]
    public async Task OnGetAsync_EmptyCode_RedirectsToIndex()
    {
        // Arrange
        var userId = Generated.NewUserId().ToString();
        var model = BuildModel();

        // Act
        var result = await model.OnGetAsync(userId, Generated.NewBlank());

        // Assert
        AssertRedirectsToIndex(result);
    }

    [Fact]
    public async Task OnGetAsync_WhitespaceCode_RedirectsToIndex()
    {
        // Arrange
        var userId = Generated.NewUserId().ToString();
        var whitespaceCode = Generated.NewWhitespaceValue();
        var model = BuildModel();

        // Act
        var result = await model.OnGetAsync(userId, whitespaceCode);

        // Assert
        AssertRedirectsToIndex(result);
    }

    [Fact]
    public async Task OnGetAsync_UserNotFound_ReturnsNotFoundObjectResult()
    {
        // Arrange
        var userId = Generated.NewUserId().ToString();
        var code = Generated.NewEmailConfirmationToken();
        var userManagerMock = MockHelpers.MockUserManager();
        userManagerMock
            .Setup(u => u.FindByIdAsync(userId))
            .ReturnsAsync((IdentityUser<Guid>?)null);

        var model = new ConfirmEmail(userManagerMock.Object);

        // Act
        var result = await model.OnGetAsync(userId, code);

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(UserMessages.UnableToLoadUser(userId), notFound.Value);
    }

    [Fact]
    public async Task OnGetAsync_ConfirmEmailSucceeds_ReturnsPageWithSuccessMessage()
    {
        // Arrange
        var userId = Generated.NewUserId().ToString();
        var token = Generated.NewEmailConfirmationToken();
        var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var user = new IdentityUser<Guid> { Id = Generated.NewUserId() };
        var userManagerMock = MockHelpers.MockUserManager();
        userManagerMock.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(user);
        userManagerMock.Setup(m => m.ConfirmEmailAsync(user, token)).ReturnsAsync(IdentityResult.Success);
        var model = new ConfirmEmail(userManagerMock.Object);

        // Act
        var result = await model.OnGetAsync(userId, code);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal(ConfirmEmail.EmailConfirmedMessage, model.StatusMessage);
    }

    [Fact]
    public async Task OnGetAsync_ConfirmEmailFails_ReturnsPageWithErrorMessage()
    {
        // Arrange
        var userId = Generated.NewUserId().ToString();
        var token = Generated.NewEmailConfirmationToken();
        var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var user = new IdentityUser<Guid> { Id = Generated.NewUserId() };
        var userManagerMock = MockHelpers.MockUserManager();
        userManagerMock.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(user);
        userManagerMock.Setup(m => m.ConfirmEmailAsync(user, token)).ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = Generated.NewFailureReason() }));
        var model = new ConfirmEmail(userManagerMock.Object);

        // Act
        var result = await model.OnGetAsync(userId, code);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal(ConfirmEmail.EmailConfirmationFailedMessage, model.StatusMessage);
    }

    [Fact]
    public void ConfirmEmailModel_Constructor_ValidUserManager_InstanceCreatedAndStatusMessageIsNull()
    {
        // Arrange
        var store = Mock.Of<IUserStore<IdentityUser<Guid>>>();
        var options = Mock.Of<IOptions<IdentityOptions>>();
        var passwordHasher = Mock.Of<IPasswordHasher<IdentityUser<Guid>>>();
        var userValidators = Array.Empty<IUserValidator<IdentityUser<Guid>>>();
        var passwordValidators = Array.Empty<IPasswordValidator<IdentityUser<Guid>>>();
        var keyNormalizer = Mock.Of<ILookupNormalizer>();
        var errors = new IdentityErrorDescriber();
        var services = Mock.Of<IServiceProvider>();
        var logger = NullLogger<UserManager<IdentityUser<Guid>>>.Instance;

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
        var model = new ConfirmEmail(userManager);

        // Assert
        Assert.NotNull(model);
        Assert.Null(model.StatusMessage);
    }

    private static ConfirmEmail BuildModel()
    {
        var userManagerMock = MockHelpers.MockUserManager();
        return new ConfirmEmail(userManagerMock.Object);
    }

    private static void AssertRedirectsToIndex(IActionResult result)
    {
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(PageRoutes.Home, redirect.PageName);
    }
}
